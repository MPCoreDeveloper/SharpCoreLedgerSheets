using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Reporting;

namespace SharpCoreLedgerSheets.Domain.Services;

/// <summary>
/// Builds a <see cref="ProfitAndLoss"/> for a given period from a <see cref="Ledger"/>.
/// </summary>
public interface IProfitAndLossBuilder
{
    /// <summary>
    /// Builds the profit and loss statement for <paramref name="period"/>.
    /// </summary>
    ProfitAndLoss BuildOn(Ledger ledger, Period period);
}
