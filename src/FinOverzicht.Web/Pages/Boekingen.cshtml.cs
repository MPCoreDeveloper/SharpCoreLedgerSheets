using FinOverzicht.Application.Models;
using FinOverzicht.Application.Services;
using FinOverzicht.Domain.Boekhouden;
using Microsoft.AspNetCore.Mvc;

namespace FinOverzicht.Web.Pages;

/// <summary>Journal entries: list the entries of a year and post a simple two-line entry.</summary>
public sealed class BoekingenModel(IJournaalpostService journaalposten, IRekeningService rekeningen) : FinPageModel
{
    public IReadOnlyList<Journaalpost> Posten { get; private set; } = [];

    public IReadOnlyList<Rekening> AlleRekeningen { get; private set; } = [];

    public int Jaar { get; private set; }

    [BindProperty]
    public DateOnly Datum { get; set; }

    [BindProperty]
    public string Omschrijving { get; set; } = string.Empty;

    [BindProperty]
    public string? Referentie { get; set; }

    [BindProperty]
    public string DebetRekening { get; set; } = string.Empty;

    [BindProperty]
    public string CreditRekening { get; set; } = string.Empty;

    [BindProperty]
    public decimal Bedrag { get; set; }

    public async Task OnGetAsync(int? jaar, CancellationToken ct)
    {
        Jaar = jaar ?? DateTime.Today.Year;
        Datum = DateOnly.FromDateTime(DateTime.Today);
        await LaadAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(int? jaar, CancellationToken ct)
    {
        Jaar = jaar ?? Datum.Year;

        await ProbeerAsync(async () =>
        {
            var regels = new List<BoekingsregelCommand>
            {
                new(DebetRekening, Bedrag, 0m),
                new(CreditRekening, 0m, Bedrag),
            };

            await journaalposten.BoekAsync(
                new JournaalpostBoekenCommand(Datum, Omschrijving, regels, Referentie), ct);

            Omschrijving = string.Empty;
            Referentie = null;
            Bedrag = 0m;
        });

        await LaadAsync(ct);
        return Page();
    }

    private async Task LaadAsync(CancellationToken ct)
    {
        await ProbeerAsync(async () => Posten = await journaalposten.AlleAsync(Periode.VoorJaar(Jaar), ct));
        await ProbeerAsync(async () => AlleRekeningen = await rekeningen.AlleAsync(ct));
    }
}
