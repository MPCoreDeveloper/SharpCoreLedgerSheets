using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;

namespace SharpCoreLedgerSheets.Web.Pages;

/// <summary>Dashboard: the three statements plus the key ratios for one book year.</summary>
public sealed class IndexModel(
    IReportingService reporting,
    IAdministrationService administration) : AppPageModel
{
    public FinancialOverview? Overview { get; private set; }

    public int Year { get; private set; }

    /// <summary>The settings of the administration (company name, book year).</summary>
    public AdministrationSettings Settings { get; private set; } = AdministrationSettings.Default;

    /// <summary>The book year that is shown, honouring a broken book year.</summary>
    public Period BookYear { get; private set; } = Period.ForYear(DateTime.Today.Year);

    public async Task OnGetAsync(int? year, CancellationToken ct)
    {
        Year = year ?? DateTime.Today.Year;

        await TryAsync(async () =>
        {
            Settings = await administration.GetSettingsAsync(ct);
            BookYear = Settings.PeriodFor(Year);
            Overview = await reporting.BuildOverviewAsync(BookYear, ct);
        });
    }
}

