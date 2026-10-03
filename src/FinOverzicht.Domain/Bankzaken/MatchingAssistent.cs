using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Domain.Bankzaken;

/// <summary>
/// Default <see cref="IMatchingAssistent"/>.
/// </summary>
/// <remarks>
/// It uses three signals, in order of decreasing specificity:
/// <list type="number">
///   <item>an earlier confirmed booking for the same counterparty IBAN;</item>
///   <item>an earlier confirmed booking for a keyword in the counterparty or the description;</item>
///   <item>the name, or the standalone ledger number, of an account that occurs in the description.</item>
/// </list>
/// The assistant never guesses: when none of these signals yields an active account it returns
/// <see langword="null"/> and the user chooses the account themselves.
/// </remarks>
public sealed class MatchingAssistent : IMatchingAssistent
{
    /// <summary>Certainty of a rule learned on the counterparty's IBAN.</summary>
    private const int ZekerheidIban = 100;

    /// <summary>Certainty of a rule learned on a keyword.</summary>
    private const int ZekerheidTrefwoord = 90;

    /// <summary>Certainty of an account name or number that occurs in the description.</summary>
    private const int ZekerheidRekening = 70;

    /// <inheritdoc />
    public Boekingsvoorstel? StelVoor(
        BankTransactie transactie,
        IReadOnlyList<Rekening> rekeningen,
        IReadOnlyList<TegenrekeningRegel> regels,
        string? bankRekeningNummer = null)
    {
        ArgumentNullException.ThrowIfNull(transactie);
        ArgumentNullException.ThrowIfNull(rekeningen);
        ArgumentNullException.ThrowIfNull(regels);

        var eigenRekening = bankRekeningNummer ?? transactie.BankRekeningNummer;

        var kandidaten = rekeningen
            .Where(rekening => rekening.IsActief && !IsZelfdeRekening(rekening, eigenRekening))
            .ToList();

        if (kandidaten.Count == 0)
        {
            return null;
        }

        var iban = TekstNormalisatie.Iban(transactie.TegenpartijIban);

        if (iban.Length > 0)
        {
            var opIban = BesteRegel(
                regels.Where(regel => string.Equals(
                    TekstNormalisatie.Iban(regel.Trefwoord), iban, StringComparison.Ordinal)),
                kandidaten);

            if (opIban is not null)
            {
                return Voorstel(
                    transactie,
                    opIban.Value.Rekening,
                    ZekerheidIban,
                    $"Eerder geboekt voor tegenpartij {iban}.");
            }
        }

        var opTrefwoord = BesteRegel(
            regels.Where(regel => TekstNormalisatie.Bevat(transactie.Zoektekst, regel.Trefwoord)),
            kandidaten);

        if (opTrefwoord is not null)
        {
            return Voorstel(
                transactie,
                opTrefwoord.Value.Rekening,
                ZekerheidTrefwoord,
                $"Trefwoord '{opTrefwoord.Value.Regel.Trefwoord}' is eerder op deze rekening geboekt.");
        }

        var opNaam = OpNaam(transactie.Zoektekst, kandidaten);

        return opNaam is null
            ? null
            : Voorstel(
                transactie,
                opNaam.Value.Rekening,
                ZekerheidRekening,
                $"De omschrijving noemt '{opNaam.Value.Zoekterm}'.");
    }

    /// <summary>
    /// Picks the rule that should win and resolves it to an account. Rules pointing at an account
    /// that no longer exists or is inactive are skipped, so a stale rule cannot block a proposal.
    /// </summary>
    private static (TegenrekeningRegel Regel, Rekening Rekening)? BesteRegel(
        IEnumerable<TegenrekeningRegel> regels,
        IReadOnlyList<Rekening> kandidaten)
    {
        foreach (var regel in regels
            .OrderByDescending(regel => regel.Bevestigingen)
            .ThenByDescending(regel => regel.Trefwoord.Length))
        {
            var rekening = kandidaten.FirstOrDefault(
                kandidaat => IsZelfdeRekening(kandidaat, regel.TegenrekeningNummer));

            if (rekening is not null)
            {
                return (regel, rekening);
            }
        }

        return null;
    }

    /// <summary>Finds an account whose name, or whose ledger number as a separate word, is mentioned.</summary>
    private static (Rekening Rekening, string Zoekterm)? OpNaam(
        string zoektekst,
        IReadOnlyList<Rekening> kandidaten)
    {
        var woorden = TekstNormalisatie.Woorden(zoektekst);

        foreach (var rekening in kandidaten
            .OrderByDescending(rekening => rekening.Naam.Length)
            .ThenBy(rekening => rekening.Nummer, StringComparer.Ordinal))
        {
            if (TekstNormalisatie.Bevat(zoektekst, TekstNormalisatie.Normaliseer(rekening.Naam)))
            {
                return (rekening, rekening.Naam);
            }

            if (woorden.Contains(TekstNormalisatie.Normaliseer(rekening.Nummer), StringComparer.Ordinal))
            {
                return (rekening, rekening.Nummer);
            }
        }

        return null;
    }

    private static bool IsZelfdeRekening(Rekening rekening, string nummer) =>
        string.Equals(rekening.Nummer, nummer, StringComparison.OrdinalIgnoreCase);

    private static Boekingsvoorstel Voorstel(
        BankTransactie transactie,
        Rekening rekening,
        int zekerheid,
        string reden) =>
        new(transactie.Id, rekening.Nummer, rekening.Naam, zekerheid, reden);
}
