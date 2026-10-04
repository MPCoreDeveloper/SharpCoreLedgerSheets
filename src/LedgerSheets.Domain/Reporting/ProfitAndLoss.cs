using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Domain.Reporting;

/// <summary>
/// The profit and loss statement ("profit- and verliesrekening") for a period.
/// </summary>
public sealed record ProfitAndLoss(
    IReadOnlyList<BalanceLine> Revenue,
    IReadOnlyList<BalanceLine> Costs,
    decimal TotalRevenue,
    decimal TotalCosts,
    Period Period)
{
    /// <summary>The result for the period (revenue minus expenses).</summary>
    public decimal Result => TotalRevenue - TotalCosts;

    /// <summary>True when the period ended with a profit.</summary>
    public bool IsProfit => Result > 0m;

    /// <summary>True when the period ended with a loss.</summary>
    public bool IsLoss => Result < 0m;

    /// <summary>The result as a fraction of revenue (0.25 = 25%), or <see langword="null"/> when there is no revenue.</summary>
    public decimal? NetMargin => TotalRevenue == 0m ? null : Result / TotalRevenue;
}
