using SharpCoreLedgerSheets.Domain.Reporting;

namespace SharpCoreLedgerSheets.Domain.Services;

/// <summary>
/// Default implementation of <see cref="IRatioCalculator"/>.
/// </summary>
/// <remarks>
/// All proportions are returned as fractions (0.25 = 25%). A ratio is <see langword="null"/>
/// when its denominator is zero. Ratios are rounded to four decimals for stable comparisons.
/// </remarks>
public sealed class RatioCalculator : IRatioCalculator
{
    /// <inheritdoc />
    public RatioOverview Calculate(BalanceSheet balanceSheet, ProfitAndLoss profitAndLoss)
    {
        ArgumentNullException.ThrowIfNull(balanceSheet);
        ArgumentNullException.ThrowIfNull(profitAndLoss);

        var currentAssets = balanceSheet.Assets.Where(r => r.Current).Sum(r => r.Amount);
        var inventory = balanceSheet.Assets.Where(r => r.Inventory).Sum(r => r.Amount);
        var kortlopendeLiabilities = balanceSheet.Liabilities.Where(r => r.Current).Sum(r => r.Amount);

        var equity = balanceSheet.EquityIncludingResult;
        var totalAssets = balanceSheet.TotalAssets;
        var revenue = profitAndLoss.TotalRevenue;
        var result = profitAndLoss.Result;

        return new RatioOverview(
            CurrentRatio: Divide(currentAssets, kortlopendeLiabilities),
            QuickRatio: Divide(currentAssets - inventory, kortlopendeLiabilities),
            Solvency: Divide(equity, totalAssets),
            NetProfitMargin: Divide(result, revenue),
            ReturnOnEquity: Divide(result, equity));
    }

    private static decimal? Divide(decimal numerator, decimal denominator) =>
        denominator == 0m ? null : decimal.Round(numerator / denominator, 4, MidpointRounding.AwayFromZero);
}
