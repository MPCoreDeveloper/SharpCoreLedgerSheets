namespace LedgerSheets.Domain.Reporting;

/// <summary>
/// The balance sheet ("balanceSheet") at a specific date: assets versus liabilities plus equity.
/// </summary>
/// <remarks>
/// The current-period result is kept separate from the equity accounts so the statement shows
/// both the capital accounts and the profit or loss for the period under review.
/// The accounting equation is: <c>TotalAssets == TotalLiabilities + TotalEquity + Result</c>.
/// </remarks>
public sealed record BalanceSheet(
    IReadOnlyList<BalanceLine> Assets,
    IReadOnlyList<BalanceLine> Liabilities,
    IReadOnlyList<BalanceLine> Equity,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal Result,
    DateOnly PerDate)
{
    /// <summary>Equity including the accumulated result.</summary>
    public decimal EquityIncludingResult => TotalEquity + Result;

    /// <summary>The total of the credit side (liabilities + equity + result).</summary>
    public decimal TotalCreditSide => TotalLiabilities + TotalEquity + Result;

    /// <summary>True when the balance sheet balances.</summary>
    public bool IsBalanced => TotalAssets == TotalCreditSide;

    /// <summary>The difference between both sides (0 for a balanced sheet).</summary>
    public decimal Difference => TotalAssets - TotalCreditSide;
}
