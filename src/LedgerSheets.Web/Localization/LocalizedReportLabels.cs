using LedgerSheets.Reports.Abstractions;
using LedgerSheets.Web.Resources;
using Microsoft.Extensions.Localization;

namespace LedgerSheets.Web.Localization;

/// <summary>
/// Supplies the report engine with labels from the shared resource files, so every report follows
/// the culture of the current request.
/// </summary>
public sealed class LocalizedReportLabels(IStringLocalizer<SharedResource> localizer) : IReportLabels
{
    /// <inheritdoc />
    public string BalanceSheet => localizer[nameof(BalanceSheet)];

    /// <inheritdoc />
    public string ProfitAndLoss => localizer[nameof(ProfitAndLoss)];

    /// <inheritdoc />
    public string CashFlowStatement => localizer[nameof(CashFlowStatement)];

    /// <inheritdoc />
    public string Ratio => localizer[nameof(Ratio)];

    /// <inheritdoc />
    public string Assets => localizer[nameof(Assets)];

    /// <inheritdoc />
    public string Liabilities => localizer[nameof(Liabilities)];

    /// <inheritdoc />
    public string Equity => localizer[nameof(Equity)];

    /// <inheritdoc />
    public string Result => localizer[nameof(Result)];

    /// <inheritdoc />
    public string Revenue => localizer[nameof(Revenue)];

    /// <inheritdoc />
    public string Costs => localizer[nameof(Costs)];

    /// <inheritdoc />
    public string Total => localizer[nameof(Total)];

    /// <inheritdoc />
    public string Operating => localizer[nameof(Operating)];

    /// <inheritdoc />
    public string Investing => localizer[nameof(Investing)];

    /// <inheritdoc />
    public string Financing => localizer[nameof(Financing)];

    /// <inheritdoc />
    public string OpeningBalance => localizer[nameof(OpeningBalance)];

    /// <inheritdoc />
    public string ClosingBalance => localizer[nameof(ClosingBalance)];

    /// <inheritdoc />
    public string NetMovement => localizer[nameof(NetMovement)];

    /// <inheritdoc />
    public string CurrentRatio => localizer[nameof(CurrentRatio)];

    /// <inheritdoc />
    public string QuickRatio => localizer[nameof(QuickRatio)];

    /// <inheritdoc />
    public string Solvency => localizer[nameof(Solvency)];

    /// <inheritdoc />
    public string NetProfitMargin => localizer[nameof(NetProfitMargin)];

    /// <inheritdoc />
    public string ReturnOnEquity => localizer[nameof(ReturnOnEquity)];

    /// <inheritdoc />
    public string Account => localizer[nameof(Account)];

    /// <inheritdoc />
    public string Description => localizer[nameof(Description)];

    /// <inheritdoc />
    public string Amount => localizer[nameof(Amount)];

    /// <inheritdoc />
    public string NoData => localizer[nameof(NoData)];

    /// <inheritdoc />
    public string GeneratedOn => localizer[nameof(GeneratedOn)];

    /// <inheritdoc />
    public string Period => localizer[nameof(Period)];

    /// <inheritdoc />
    public string Balanced => localizer[nameof(Balanced)];

    /// <inheritdoc />
    public string NotBalanced => localizer[nameof(NotBalanced)];

    /// <inheritdoc />
    public string OverviewTitle => localizer[nameof(OverviewTitle)];

    /// <inheritdoc />
    public string OverviewDescription => localizer[nameof(OverviewDescription)];

    /// <inheritdoc />
    public string this[string key] => localizer[key];
}
