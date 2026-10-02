using FinOverzicht.Application.Models;
using FinOverzicht.Application.Services;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Web.Pages;

/// <summary>Dashboard: the three statements plus the key ratios for one financial year.</summary>
public sealed class IndexModel(IRapportageService rapportage) : FinPageModel
{
    public FinancieelOverzicht? Overzicht { get; private set; }

    public int Jaar { get; private set; }

    public async Task OnGetAsync(int? jaar, CancellationToken ct)
    {
        Jaar = jaar ?? DateTime.Today.Year;
        await ProbeerAsync(async () => Overzicht = await rapportage.StelOverzichtOpAsync(Periode.VoorJaar(Jaar), ct));
    }
}
