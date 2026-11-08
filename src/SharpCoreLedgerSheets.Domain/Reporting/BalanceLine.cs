namespace SharpCoreLedgerSheets.Domain.Reporting;

/// <summary>
/// A single line in a financial statement: an optional account number, a description and an amount.
/// </summary>
/// <remarks>
/// The optional classification flags are copied from the underlying <c>Account</c> so that
/// derived calculations (for example the liquidity ratios) can run on report data alone.
/// </remarks>
public sealed record BalanceLine(string? Number, string Description, decimal Amount)
{
    /// <summary>Whether the underlying account is a current (short-term) asset or liability.</summary>
    public bool Current { get; init; }

    /// <summary>Whether the underlying account represents inventory.</summary>
    public bool Inventory { get; init; }

    /// <summary>Whether the underlying account represents cash or a cash equivalent.</summary>
    public bool CashEquivalent { get; init; }
}
