using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Reporting;

namespace SharpCoreLedgerSheets.Domain.Services;

/// <summary>
/// Builds a <see cref="BalanceSheet"/> at a given date from a <see cref="Ledger"/>.
/// </summary>
public interface IBalanceSheetBuilder
{
    /// <summary>
    /// Builds the balance sheet as of <paramref name="perDate"/> (inclusive).
    /// </summary>
    BalanceSheet BuildOn(Ledger ledger, DateOnly perDate);
}
