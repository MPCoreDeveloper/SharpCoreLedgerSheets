using LedgerSheets.Application.Models;
using LedgerSheets.Application.Services;
using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Web.Pages;

/// <summary>Dashboard: the three statements plus the key ratios for one financial year.</summary>
public sealed class IndexModel(IReportingService reporting) : AppPageModel
{
    public FinancialOverview? Overview { get; private set; }

    public int Year { get; private set; }

    public async Task OnGetAsync(int? year, CancellationToken ct)
    {
        Year = year ?? DateTime.Today.Year;
        await TryAsync(async () => Overview = await reporting.BuildOverviewAsync(Period.ForYear(Year), ct));
    }
}
