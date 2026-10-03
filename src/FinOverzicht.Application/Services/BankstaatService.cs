using FinOverzicht.Application.Abstractions;
using FinOverzicht.Application.Exceptions;
using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Application.Services;

/// <summary>
/// Default <see cref="IBankstaatService"/>.
/// </summary>
/// <remarks>
/// The service orchestrates the bank import: it picks a reader for the file, filters out movements
/// that are already known, asks the <see cref="IMatchingAssistent"/> for a suggestion, and posts the
/// journal entry through <see cref="IJournaalpostService"/> so that every booking rule stays in one
/// place. A confirmed booking is remembered as a <see cref="TegenrekeningRegel"/>, which is how the
/// assistant improves while the administration grows.
/// </remarks>
public sealed class BankstaatService(
    IBankstaatRepository bankstaatRepository,
    ITegenrekeningRegelRepository regelRepository,
    IRekeningRepository rekeningRepository,
    IJournaalpostService journaalpostService,
    IMatchingAssistent matchingAssistent,
    IEnumerable<IBankstaatImporteur> importeurs) : IBankstaatService
{
    /// <inheritdoc />
    public async Task<BankstaatImportResultaat> ImporteerAsync(
        BankstaatImportCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Bestandsnaam);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Inhoud);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.BankRekeningNummer);

        var bankrekening = await ZoekActieveRekeningAsync(command.BankRekeningNummer, ct).ConfigureAwait(false);
        var importeur = KiesImporteur(command);
        var gelezen = importeur.Lees(command.Inhoud, bankrekening.Nummer);

        // The keys that are already stored make the import idempotent; adding to the set also filters
        // out a movement that appears twice inside one file.
        var bekend = (await bankstaatRepository.BestaandeSleutelsAsync(ct).ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);

        var nieuw = gelezen.Where(transactie => bekend.Add(transactie.Sleutel)).ToList();

        if (nieuw.Count > 0)
        {
            await bankstaatRepository.ToevoegenAsync(nieuw, ct).ConfigureAwait(false);
        }

        return new BankstaatImportResultaat(
            importeur.Formaat,
            gelezen.Count,
            nieuw.Count,
            gelezen.Count - nieuw.Count,
            nieuw);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<BankTransactie>> AlleAsync(
        BankTransactieStatus? status = null,
        CancellationToken ct = default) =>
        bankstaatRepository.AlleAsync(status, ct);

    /// <inheritdoc />
    public async Task<Boekingsvoorstel?> VoorstelAsync(Guid transactieId, CancellationToken ct = default)
    {
        var transactie = await ZoekTransactieAsync(transactieId, ct).ConfigureAwait(false);
        var voorstellen = await VoorstellenAsync([transactie], ct).ConfigureAwait(false);

        return voorstellen.Count > 0 ? voorstellen[0] : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Boekingsvoorstel>> VoorstellenAsync(
        IReadOnlyList<BankTransactie> transacties,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transacties);

        if (transacties.Count == 0)
        {
            return [];
        }

        // The ledger and the learned rules are read once for the whole list: a statement of a few
        // hundred movements must not turn into a few hundred round trips for the same two tables.
        var rekeningen = await rekeningRepository.AlleAsync(ct).ConfigureAwait(false);
        var regels = await regelRepository.AlleAsync(ct).ConfigureAwait(false);

        var voorstellen = new List<Boekingsvoorstel>(transacties.Count);

        foreach (var transactie in transacties)
        {
            ArgumentNullException.ThrowIfNull(transactie);

            if (matchingAssistent.StelVoor(transactie, rekeningen, regels) is { } voorstel)
            {
                voorstellen.Add(voorstel);
            }
        }

        return voorstellen;
    }

    /// <inheritdoc />
    public async Task<Journaalpost> BoekAsync(
        Guid transactieId,
        string tegenrekeningNummer,
        string? omschrijving = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tegenrekeningNummer);

        var transactie = await ZoekTransactieAsync(transactieId, ct).ConfigureAwait(false);

        if (transactie.Status != BankTransactieStatus.Open)
        {
            throw new DomeinException($"De bankmutatie van {transactie.Datum:dd-MM-yyyy} is al verwerkt.");
        }

        var bankrekening = await ZoekActieveRekeningAsync(transactie.BankRekeningNummer, ct).ConfigureAwait(false);
        var tegenrekening = await ZoekActieveRekeningAsync(tegenrekeningNummer, ct).ConfigureAwait(false);

        if (string.Equals(bankrekening.Nummer, tegenrekening.Nummer, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomeinException("De tegenrekening van een bankmutatie mag niet de bankrekening zelf zijn.");
        }

        var bedrag = transactie.BedragZonderTeken.Waarde;
        var tekst = omschrijving ?? BepaalOmschrijving(transactie);

        // Received money debits the bank account, paid money credits it; the counterparty takes the
        // other side. That is exactly the entry a bookkeeper would make by hand.
        IReadOnlyList<BoekingsregelCommand> regels = transactie.IsBijschrijving
            ?
            [
                new BoekingsregelCommand(bankrekening.Nummer, bedrag, 0m, tekst),
                new BoekingsregelCommand(tegenrekening.Nummer, 0m, bedrag, tekst),
            ]
            :
            [
                new BoekingsregelCommand(tegenrekening.Nummer, bedrag, 0m, tekst),
                new BoekingsregelCommand(bankrekening.Nummer, 0m, bedrag, tekst),
            ];

        var post = await journaalpostService.BoekAsync(
            new JournaalpostBoekenCommand(transactie.Datum, tekst, regels, transactie.Referentie),
            ct).ConfigureAwait(false);

        await bankstaatRepository.BijwerkenAsync(transactie.MarkeerGeboekt(post.Id), ct).ConfigureAwait(false);
        await OnthoudAsync(transactie, tegenrekening.Nummer, ct).ConfigureAwait(false);

        return post;
    }

    /// <inheritdoc />
    public async Task NegeerAsync(Guid transactieId, CancellationToken ct = default)
    {
        var transactie = await ZoekTransactieAsync(transactieId, ct).ConfigureAwait(false);

        if (transactie.Status == BankTransactieStatus.Geboekt)
        {
            throw new DomeinException("Een geboekte bankmutatie kan niet worden genegeerd.");
        }

        await bankstaatRepository.BijwerkenAsync(transactie.Negeer(), ct).ConfigureAwait(false);
    }

    /// <summary>Remembers a confirmed booking as a matching rule for the next import.</summary>
    private async Task OnthoudAsync(BankTransactie transactie, string tegenrekeningNummer, CancellationToken ct)
    {
        // The IBAN is the most specific keyword; without one the counterparty's name is the best we
        // have. A very short keyword is skipped: it would match almost every movement.
        var trefwoord = TegenrekeningRegel.TrefwoordVan(transactie.TegenpartijIban ?? transactie.TegenpartijNaam);

        if (trefwoord.Length < TegenrekeningRegel.MinLengteTrefwoord)
        {
            return;
        }

        var bestaand = (await regelRepository.AlleAsync(ct).ConfigureAwait(false))
            .FirstOrDefault(regel => string.Equals(regel.Trefwoord, trefwoord, StringComparison.Ordinal));

        // Booking the same keyword on another account is a deliberate correction: the rule moves with
        // it and counts as one more confirmation.
        var regel = bestaand is null
            ? new TegenrekeningRegel(trefwoord, tegenrekeningNummer)
            : bestaand.VoorRekening(tegenrekeningNummer).Bevestigd();

        await regelRepository.BewaarAsync(regel, ct).ConfigureAwait(false);
    }

    /// <summary>The description of the journal entry: who it was and what it was for.</summary>
    private static string BepaalOmschrijving(BankTransactie transactie)
    {
        var delen = new[] { transactie.TegenpartijNaam, transactie.Omschrijving }
            .Where(deel => !string.IsNullOrWhiteSpace(deel));

        var omschrijving = string.Join(" - ", delen);

        return omschrijving.Length > 0 ? omschrijving : $"Bankmutatie {transactie.Referentie}";
    }

    /// <summary>Picks the reader: the forced format, or the first reader that recognises the file.</summary>
    private IBankstaatImporteur KiesImporteur(BankstaatImportCommand command)
    {
        if (command.Formaat is { } formaat)
        {
            return importeurs.FirstOrDefault(importeur => importeur.Formaat == formaat)
                ?? throw new DomeinException($"Er is geen lezer voor het formaat '{formaat}'.");
        }

        return importeurs.FirstOrDefault(
                importeur => importeur.KanVerwerken(command.Bestandsnaam, command.Inhoud))
            ?? throw new DomeinException(
                $"Het bestand '{command.Bestandsnaam}' is geen ondersteund bankafschrift (CAMT.053 of CSV).");
    }

    private async Task<BankTransactie> ZoekTransactieAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Geef een geldig id op.", nameof(id));
        }

        return await bankstaatRepository.ZoekAsync(id, ct).ConfigureAwait(false)
            ?? throw new NietGevondenException($"Bankmutatie '{id}' is niet gevonden.");
    }

    private async Task<Rekening> ZoekActieveRekeningAsync(string nummer, CancellationToken ct)
    {
        var rekening = await rekeningRepository.ZoekOpNummerAsync(nummer, ct).ConfigureAwait(false)
            ?? throw new NietGevondenException($"Rekening '{nummer}' is niet gevonden.");

        if (!rekening.IsActief)
        {
            throw new DomeinException($"Rekening '{rekening.Nummer}' ({rekening.Naam}) is niet actief.");
        }

        return rekening;
    }
}
