using LedgerSheets.Application.Models;
using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Application.Services;

/// <summary>
/// Produces the complete financial overview (three statements plus ratios) for a period.
/// </summary>
public interface IReportingService
{
    /// <summary>Builds the overview for <paramref name="period"/>.</summary>
    Task<FinancialOverview> BuildOverviewAsync(Period period, CancellationToken ct = default);
}
