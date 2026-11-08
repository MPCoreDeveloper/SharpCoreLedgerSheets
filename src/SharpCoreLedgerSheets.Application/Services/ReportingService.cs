using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Services;

namespace SharpCoreLedgerSheets.Application.Services;

/// <summary>
/// Default <see cref="IReportingService"/>: composes the domain statement builders.
/// </summary>
/// <remarks>
/// The balance sheet is taken at the end of the period while the profit and loss statement and
/// the cash flow statement cover the period itself, which is the conventional presentation.
/// </remarks>
public sealed class ReportingService(
    ILedgerLoader ledgerLoader,
    IBalanceSheetBuilder balanceSheetBuilder,
    IProfitAndLossBuilder profitAndLossBuilder,
    ICashFlowBuilder cashFlowBuilder,
    IRatioCalculator ratioCalculator) : IReportingService
{
    /// <inheritdoc />
    public async Task<FinancialOverview> BuildOverviewAsync(Period period, CancellationToken ct = default)
    {
        var ledger = await ledgerLoader.LoadAsync(ct).ConfigureAwait(false);

        var balanceSheet = balanceSheetBuilder.BuildOn(ledger, period.Closing);
        var profitAndLoss = profitAndLossBuilder.BuildOn(ledger, period);
        var cashFlow = cashFlowBuilder.BuildOn(ledger, period);
        var ratio = ratioCalculator.Calculate(balanceSheet, profitAndLoss);

        return new FinancialOverview(balanceSheet, profitAndLoss, cashFlow, ratio, period);
    }
}
