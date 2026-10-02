using FinOverzicht.Application.Models;
using FinOverzicht.Application.Services;
using FinOverzicht.Domain.Boekhouden;
using Microsoft.AspNetCore.Mvc;

namespace FinOverzicht.Web.Pages;

/// <summary>Chart of accounts: list the ledger accounts and add new ones.</summary>
public sealed class RekeningenModel(IRekeningService rekeningen) : FinPageModel
{
    public IReadOnlyList<Rekening> AlleRekeningen { get; private set; } = [];

    public RekeningSoort[] Soorten { get; } = Enum.GetValues<RekeningSoort>();

    [BindProperty]
    public string Nummer { get; set; } = string.Empty;

    [BindProperty]
    public string Naam { get; set; } = string.Empty;

    [BindProperty]
    public RekeningSoort Soort { get; set; } = RekeningSoort.Activa;

    [BindProperty]
    public bool Vlottend { get; set; }

    [BindProperty]
    public bool LiquideMiddel { get; set; }

    [BindProperty]
    public bool Voorraad { get; set; }

    public Task OnGetAsync(CancellationToken ct) => LaadAsync(ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await ProbeerAsync(async () =>
        {
            await rekeningen.VoegToeAsync(
                new RekeningToevoegenCommand(Nummer, Naam, Soort, Vlottend, LiquideMiddel, Voorraad), ct);

            Nummer = string.Empty;
            Naam = string.Empty;
            Vlottend = false;
            LiquideMiddel = false;
            Voorraad = false;
        });

        await LaadAsync(ct);
        return Page();
    }

    private Task LaadAsync(CancellationToken ct) =>
        ProbeerAsync(async () => AlleRekeningen = await rekeningen.AlleAsync(ct));
}
