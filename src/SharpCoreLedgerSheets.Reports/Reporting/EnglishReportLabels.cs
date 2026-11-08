using System.Globalization;
using SharpCoreLedgerSheets.Reports.Abstractions;

namespace SharpCoreLedgerSheets.Reports.Reporting;

/// <summary>
/// Dutch labels, used as the built-in default so the report engine works without a host.
/// </summary>
/// <remarks>
/// The web application replaces this registration with an <c>IStringLocalizer</c>-backed
/// implementation so reports follow the request culture.
/// </remarks>
public sealed class EnglishReportLabels : IReportLabels
{
    /// <inheritdoc />
    public string BalanceSheet => "Balance sheet";

    /// <inheritdoc />
    public string ProfitAndLoss => "Profit and loss statement";

    /// <inheritdoc />
    public string CashFlowStatement => "Cash flow statement";

    /// <inheritdoc />
    public string Ratio => "Key ratios";

    /// <inheritdoc />
    public string Assets => "Assets";

    /// <inheritdoc />
    public string Liabilities => "Liabilities";

    /// <inheritdoc />
    public string Equity => "Equity";

    /// <inheritdoc />
    public string Result => "Result";

    /// <inheritdoc />
    public string Revenue => "Revenue";

    /// <inheritdoc />
    public string Costs => "Expenses";

    /// <inheritdoc />
    public string Total => "Total";

    /// <inheritdoc />
    public string Operating => "Operating activities";

    /// <inheritdoc />
    public string Investing => "Investing activities";

    /// <inheritdoc />
    public string Financing => "Financing activities";

    /// <inheritdoc />
    public string OpeningBalance => "Opening balance";

    /// <inheritdoc />
    public string ClosingBalance => "Closing balance";

    /// <inheritdoc />
    public string NetMovement => "Net cash flow";

    /// <inheritdoc />
    public string CurrentRatio => "Current ratio";

    /// <inheritdoc />
    public string QuickRatio => "Quick ratio";

    /// <inheritdoc />
    public string Solvency => "Solvency";

    /// <inheritdoc />
    public string NetProfitMargin => "Net profit margin";

    /// <inheritdoc />
    public string ReturnOnEquity => "Return on equity";

    /// <inheritdoc />
    public string Account => "Account";

    /// <inheritdoc />
    public string Description => "Description";

    /// <inheritdoc />
    public string Amount => "Amount";

    /// <inheritdoc />
    public string NoData => "No data available for this period.";

    /// <inheritdoc />
    public string GeneratedOn => "Generated on";

    /// <inheritdoc />
    public string Period => "Period";

    /// <inheritdoc />
    public string Balanced => "Balanced";

    /// <inheritdoc />
    public string NotBalanced => "Out of balance";

    /// <inheritdoc />
    public string OverviewTitle => "Financial overview";

    /// <inheritdoc />
    public string OverviewDescription => "Balance sheet, result, cash flow and key ratios in one document.";

    /// <inheritdoc />
    public string this[string key] => key switch
    {
        nameof(BalanceSheet) => BalanceSheet,
        nameof(ProfitAndLoss) => ProfitAndLoss,
        nameof(CashFlowStatement) => CashFlowStatement,
        nameof(Ratio) => Ratio,
        nameof(Assets) => Assets,
        nameof(Liabilities) => Liabilities,
        nameof(Equity) => Equity,
        nameof(Result) => Result,
        nameof(Revenue) => Revenue,
        nameof(Costs) => Costs,
        nameof(Total) => Total,
        nameof(Operating) => Operating,
        nameof(Investing) => Investing,
        nameof(Financing) => Financing,
        nameof(OpeningBalance) => OpeningBalance,
        nameof(ClosingBalance) => ClosingBalance,
        nameof(NetMovement) => NetMovement,
        nameof(CurrentRatio) => CurrentRatio,
        nameof(QuickRatio) => QuickRatio,
        nameof(Solvency) => Solvency,
        nameof(NetProfitMargin) => NetProfitMargin,
        nameof(ReturnOnEquity) => ReturnOnEquity,
        nameof(Account) => Account,
        nameof(Description) => Description,
        nameof(Amount) => Amount,
        nameof(NoData) => NoData,
        nameof(GeneratedOn) => GeneratedOn,
        nameof(Period) => Period,
        nameof(Balanced) => Balanced,
        nameof(NotBalanced) => NotBalanced,
        nameof(OverviewTitle) => OverviewTitle,
        nameof(OverviewDescription) => OverviewDescription,
        _ => throw new ArgumentOutOfRangeException(
            nameof(key),
            key,
            string.Create(CultureInfo.InvariantCulture, $"Onbekende label-sleutel: '{key}'.")),
    };
}
