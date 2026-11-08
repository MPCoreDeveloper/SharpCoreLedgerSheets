using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Domain.Reporting;

/// <summary>
/// The cash flow statement ("cashFlowStatement") for a period, split into operating,
/// investing and financing activities.
/// </summary>
/// <remarks>
/// All amounts are expressed as cash movements: a positive amount increases the cash position.
/// <see cref="IsBalanced"/> verifies that the opening balance plus the net movement equals the
/// closing balance.
/// </remarks>
public sealed record CashFlowOverview(
    IReadOnlyList<BalanceLine> Operating,
    IReadOnlyList<BalanceLine> Investing,
    IReadOnlyList<BalanceLine> Financing,
    decimal OpeningBalance,
    decimal ClosingBalance,
    Period Period)
{
    /// <summary>Net cash from operating activities.</summary>
    public decimal NetOperating => Som(Operating);

    /// <summary>Net cash from investing activities.</summary>
    public decimal NetInvesting => Som(Investing);

    /// <summary>Net cash from financing activities.</summary>
    public decimal NetFinancing => Som(Financing);

    /// <summary>Net change in cash for the period.</summary>
    public decimal NetMovement => NetOperating + NetInvesting + NetFinancing;

    /// <summary>The closing balance implied by the opening balance and the net movement.</summary>
    public decimal ExpectedClosingBalance => OpeningBalance + NetMovement;

    /// <summary>True when the statement reconciles (opening + movement == closing).</summary>
    public bool IsBalanced => decimal.Abs(ClosingBalance - ExpectedClosingBalance) < 0.005m;

    private static decimal Som(IReadOnlyList<BalanceLine> lines) => lines.Sum(r => r.Amount);
}
