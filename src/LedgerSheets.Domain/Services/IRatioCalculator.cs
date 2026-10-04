using LedgerSheets.Domain.Reporting;

namespace LedgerSheets.Domain.Services;

/// <summary>
/// Derives the key <see cref="RatioOverview"/> from a balance sheet and a profit and loss statement.
/// </summary>
public interface IRatioCalculator
{
    /// <summary>
    /// Calculates the ratios. <paramref name="balanceSheet"/> should be taken at the end of the period
    /// of <paramref name="profitAndLoss"/>.
    /// </summary>
    RatioOverview Calculate(BalanceSheet balanceSheet, ProfitAndLoss profitAndLoss);
}
