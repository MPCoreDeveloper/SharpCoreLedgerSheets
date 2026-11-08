using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreLedgerSheets.Domain.Reporting;
using SharpCoreLedgerSheets.Domain.Services;

namespace SharpCoreLedgerSheets.Web.Pages;

/// <summary>
/// The VAT return of a book year, per quarter or for the whole year.
/// </summary>
public sealed class VatModel(
    IAdministrationService administration,
    ILedgerLoader ledgerLoader,
    IVatReturnBuilder builder) : AppPageModel
{
    /// <summary>The settings of the administration (its VAT rates drive the return).</summary>
    public AdministrationSettings Settings { get; private set; } = AdministrationSettings.Default;

    /// <summary>The calculated return, or <see langword="null"/> when it could not be built.</summary>
    public VatReturn? Return { get; private set; }

    public int Year { get; private set; }

    /// <summary>The quarter (1..4) or 0 for the whole book year.</summary>
    public int Quarter { get; private set; }

    public async Task OnGetAsync(int? year, int? quarter, CancellationToken ct)
    {
        Year = year ?? DateTime.Today.Year;
        Quarter = quarter is >= 1 and <= 4 ? quarter.Value : 0;

        await TryAsync(async () =>
        {
            Settings = await administration.GetSettingsAsync(ct);

            var bookYear = Settings.PeriodFor(Year);
            var period = Quarter == 0 ? bookYear : QuarterOf(bookYear, Quarter);
            var ledger = await ledgerLoader.LoadAsync(ct);

            Return = builder.BuildOn(ledger, Settings.Vat, period);
        });
    }

    private static Period QuarterOf(Period bookYear, int quarter)
    {
        var start = bookYear.Start.AddMonths(3 * (quarter - 1));

        return new Period(start, start.AddMonths(3).AddDays(-1));
    }
}
