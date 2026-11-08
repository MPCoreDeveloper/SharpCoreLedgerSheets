using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Reporting;

namespace SharpCoreLedgerSheets.Domain.Services;

/// <summary>
/// Builds a <see cref="CashFlowOverview"/> for a given period from a <see cref="Ledger"/>.
/// </summary>
public interface ICashFlowBuilder
{
    /// <summary>
    /// Builds the cash flow statement for <paramref name="period"/>.
    /// </summary>
    CashFlowOverview BuildOn(Ledger ledger, Period period);
}
