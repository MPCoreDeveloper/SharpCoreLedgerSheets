using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Reporting;

namespace LedgerSheets.Application.Models;

/// <summary>
/// The complete financial picture for a period: the three core statements plus the key ratios.
/// </summary>
/// <remarks>
/// This is the primary read model of the application layer; both the web UI and the report
/// engine consume it.
/// </remarks>
public sealed record FinancialOverview(
    BalanceSheet BalanceSheet,
    ProfitAndLoss ProfitAndLoss,
    CashFlowOverview CashFlow,
    RatioOverview Ratio,
    Period Period)
{
    /// <summary>True when all three statements are internally consistent.</summary>
    public bool IsConsistent => BalanceSheet.IsBalanced && CashFlow.IsBalanced;
}
