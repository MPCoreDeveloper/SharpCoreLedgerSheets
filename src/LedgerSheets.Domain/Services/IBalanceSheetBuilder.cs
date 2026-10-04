using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Reporting;

namespace LedgerSheets.Domain.Services;

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
