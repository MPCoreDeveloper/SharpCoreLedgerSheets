namespace SharpCoreLedgerSheets.Domain.Reporting;

/// <summary>
/// The key financial ratios derived from the balance sheet and the profit and loss statement.
/// </summary>
/// <remarks>
/// Ratio proportions (solvency, net margin, return on equity) are expressed as fractions:
/// <c>0.25</c> means 25%. A value is <see langword="null"/> when it cannot be computed
/// (for example because the denominator is zero).
/// </remarks>
public sealed record RatioOverview(
    decimal? CurrentRatio,
    decimal? QuickRatio,
    decimal? Solvency,
    decimal? NetProfitMargin,
    decimal? ReturnOnEquity)
{
    /// <summary>A ratio set with no computed values (empty ledger).</summary>
    public static RatioOverview Empty => new(null, null, null, null, null);
}
