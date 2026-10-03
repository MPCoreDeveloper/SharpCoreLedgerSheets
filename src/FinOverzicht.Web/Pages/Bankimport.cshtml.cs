using FinOverzicht.Application.Models;
using FinOverzicht.Application.Services;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FinOverzicht.Web.Pages;

/// <summary>
/// Bank import: upload a statement, review the movements the matching assistant proposes an account
/// for, and book or ignore them one by one.
/// </summary>
public sealed class BankimportModel(IBankstaatService bankstaat, IRekeningService rekeningen) : FinPageModel
{
    /// <summary>The movements that still have to be booked or ignored.</summary>
    public IReadOnlyList<BankimportRegel> Regels { get; private set; } = [];

    /// <summary>The ledger accounts a movement can be booked against.</summary>
    public IReadOnlyList<Rekening> AlleRekeningen { get; private set; } = [];

    /// <summary>The outcome of the last import, when the page was reached through one.</summary>
    public BankstaatImportResultaat? Resultaat { get; private set; }

    /// <summary>The formats a statement can be forced into; 'automatic' is not one of them.</summary>
    public BankstaatFormaat[] Formaten { get; } = Enum.GetValues<BankstaatFormaat>();

    [BindProperty]
    public IFormFile? Bestand { get; set; }

    [BindProperty]
    public string BankRekeningNummer { get; set; } = string.Empty;

    [BindProperty]
    public BankstaatFormaat? Formaat { get; set; }

    [BindProperty]
    public Guid TransactieId { get; set; }

    [BindProperty]
    public string TegenrekeningNummer { get; set; } = string.Empty;

    [BindProperty]
    public string? Omschrijving { get; set; }

    public Task OnGetAsync(CancellationToken ct) => LaadAsync(ct);

    public async Task<IActionResult> OnPostImporteerAsync(CancellationToken ct)
    {
        await ProbeerAsync(async () =>
        {
            if (Bestand is null)
            {
                throw new DomeinException("Kies eerst een bankafschrift (CAMT.053 of CSV) om te importeren.");
            }

            using var lezer = new StreamReader(Bestand.OpenReadStream());
            var inhoud = await lezer.ReadToEndAsync(ct);

            Resultaat = await bankstaat.ImporteerAsync(
                new BankstaatImportCommand(Bestand.FileName, inhoud, BankRekeningNummer, Formaat),
                ct);
        });

        await LaadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostBoekAsync(CancellationToken ct)
    {
        await ProbeerAsync(async () =>
            await bankstaat.BoekAsync(TransactieId, TegenrekeningNummer, Omschrijving, ct));

        await LaadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostNegeerAsync(CancellationToken ct)
    {
        await ProbeerAsync(async () => await bankstaat.NegeerAsync(TransactieId, ct));

        await LaadAsync(ct);
        return Page();
    }

    private async Task LaadAsync(CancellationToken ct)
    {
        await ProbeerAsync(async () => AlleRekeningen = await rekeningen.AlleAsync(ct));
        await ProbeerAsync(async () => Regels = await OpenstaandeRegelsAsync(ct));
    }

    /// <summary>Reads the open movements and asks the assistant for an account per movement.</summary>
    private async Task<IReadOnlyList<BankimportRegel>> OpenstaandeRegelsAsync(CancellationToken ct)
    {
        var open = await bankstaat.AlleAsync(BankTransactieStatus.Open, ct);

        if (open.Count == 0)
        {
            return [];
        }

        // All proposals in one pass: the assistant reads the ledger and the learned rules once for the
        // whole list, so a statement with many movements does not cost a lookup per movement.
        var voorstellen = new Dictionary<Guid, Boekingsvoorstel>();

        foreach (var voorstel in await bankstaat.VoorstellenAsync(open, ct))
        {
            // Indexer rather than ToDictionary: a duplicate id from storage must not take the page down.
            voorstellen[voorstel.TransactieId] = voorstel;
        }

        return [.. open.Select(transactie => new BankimportRegel(
            transactie,
            voorstellen.GetValueOrDefault(transactie.Id)))];
    }
}
