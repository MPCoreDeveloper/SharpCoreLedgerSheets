using System.Globalization;
using System.Text;
using SharpCoreLedgerSheets.Domain.Reporting;
using SharpCoreLedgerSheets.Reports.Abstractions;
using SharpCoreLedgerSheets.Reports.Models;

namespace SharpCoreLedgerSheets.Reports.Services;

/// <summary>
/// Exports reports to RFC 4180 comma-separated values.
/// </summary>
/// <remarks>
/// The separator is a comma and all numbers are written with the invariant culture, so the files
/// import predictably into any spreadsheet regardless of the machine's regional settings.
/// </remarks>
public sealed class CsvReportExporter(IReportLabels labels) : IReportExporter
{
    private const char Separator = ',';

    /// <inheritdoc />
    public string ToCsv(FinancialOverviewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = new StringBuilder();
        Header(builder, model);
        BalanceSheetLines(builder, model.Overview.BalanceSheet);
        return builder.ToString();
    }

    /// <inheritdoc />
    public string ToCompleteCsv(FinancialOverviewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = new StringBuilder();
        Header(builder, model);

        BalanceSheetLines(builder, model.Overview.BalanceSheet);
        ProfitAndLossLines(builder, model.Overview.ProfitAndLoss);
        CashFlowLines(builder, model.Overview.CashFlow);
        RatioLines(builder, model.Overview.Ratio);

        return builder.ToString();
    }

    private static void Header(StringBuilder builder, FinancialOverviewModel model)
    {
        var metadata = model.Metadata;

        builder.AppendLine(Line(metadata.Title));

        if (!string.IsNullOrWhiteSpace(metadata.Administration))
        {
            builder.AppendLine(Line(metadata.Administration));
        }

        if (!string.IsNullOrWhiteSpace(metadata.Period))
        {
            builder.AppendLine(Line(metadata.Period));
        }

        builder.AppendLine(Line(metadata.GeneratedOn.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)));
        builder.AppendLine();
    }

    private void BalanceSheetLines(StringBuilder builder, BalanceSheet balanceSheet)
    {
        builder.AppendLine(Line(labels.BalanceSheet));
        builder.AppendLine(Line(labels.Account, labels.Description, labels.Amount));

        Section(builder, labels.Assets, balanceSheet.Assets);
        builder.AppendLine(Line(string.Empty, $"{labels.Total} {labels.Assets}", Amount(balanceSheet.TotalAssets)));
        builder.AppendLine();

        Section(builder, labels.Liabilities, balanceSheet.Liabilities);
        builder.AppendLine(Line(string.Empty, $"{labels.Total} {labels.Liabilities}", Amount(balanceSheet.TotalLiabilities)));
        builder.AppendLine();

        Section(builder, labels.Equity, balanceSheet.Equity);
        builder.AppendLine(Line(string.Empty, labels.Result, Amount(balanceSheet.Result)));
        builder.AppendLine(Line(string.Empty, $"{labels.Total} {labels.Equity}", Amount(balanceSheet.EquityIncludingResult)));
        builder.AppendLine();
    }

    private void ProfitAndLossLines(StringBuilder builder, ProfitAndLoss profitAndLoss)
    {
        builder.AppendLine(Line(labels.ProfitAndLoss));
        builder.AppendLine(Line(labels.Account, labels.Description, labels.Amount));

        Section(builder, labels.Revenue, profitAndLoss.Revenue);
        builder.AppendLine(Line(string.Empty, $"{labels.Total} {labels.Revenue}", Amount(profitAndLoss.TotalRevenue)));

        Section(builder, labels.Costs, profitAndLoss.Costs);
        builder.AppendLine(Line(string.Empty, $"{labels.Total} {labels.Costs}", Amount(profitAndLoss.TotalCosts)));
        builder.AppendLine(Line(string.Empty, labels.Result, Amount(profitAndLoss.Result)));
        builder.AppendLine();
    }

    private void CashFlowLines(StringBuilder builder, CashFlowOverview cashFlow)
    {
        builder.AppendLine(Line(labels.CashFlowStatement));
        builder.AppendLine(Line(labels.Description, labels.Amount));

        CashFlowSection(builder, labels.Operating, cashFlow.Operating, cashFlow.NetOperating);
        CashFlowSection(builder, labels.Investing, cashFlow.Investing, cashFlow.NetInvesting);
        CashFlowSection(builder, labels.Financing, cashFlow.Financing, cashFlow.NetFinancing);

        builder.AppendLine(Line(labels.OpeningBalance, Amount(cashFlow.OpeningBalance)));
        builder.AppendLine(Line(labels.NetMovement, Amount(cashFlow.NetMovement)));
        builder.AppendLine(Line(labels.ClosingBalance, Amount(cashFlow.ClosingBalance)));
        builder.AppendLine();
    }

    private void RatioLines(StringBuilder builder, RatioOverview ratio)
    {
        builder.AppendLine(Line(labels.Ratio));
        builder.AppendLine(Line(labels.Description, labels.Amount));

        builder.AppendLine(Line(labels.CurrentRatio, RatioValue(ratio.CurrentRatio)));
        builder.AppendLine(Line(labels.QuickRatio, RatioValue(ratio.QuickRatio)));
        builder.AppendLine(Line(labels.Solvency, Percentage(ratio.Solvency)));
        builder.AppendLine(Line(labels.NetProfitMargin, Percentage(ratio.NetProfitMargin)));
        builder.AppendLine(Line(labels.ReturnOnEquity, Percentage(ratio.ReturnOnEquity)));
    }

    private static void Section(StringBuilder builder, string title, IReadOnlyList<BalanceLine> lines)
    {
        builder.AppendLine(Line(title));

        foreach (var line in lines)
        {
            builder.AppendLine(Line(line.Number ?? string.Empty, line.Description, Amount(line.Amount)));
        }
    }

    private void CashFlowSection(StringBuilder builder, string title, IReadOnlyList<BalanceLine> lines, decimal net)
    {
        builder.AppendLine(Line(title));

        foreach (var line in lines)
        {
            builder.AppendLine(Line(string.Empty, line.Description, Amount(line.Amount)));
        }

        builder.AppendLine(Line(string.Empty, $"{labels.Total} {title}", Amount(net)));
    }

    private static string Amount(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string RatioValue(decimal? value) =>
        value.HasValue ? value.Value.ToString("0.00", CultureInfo.InvariantCulture) : string.Empty;

    private static string Percentage(decimal? value) =>
        value.HasValue ? (value.Value * 100m).ToString("0.0", CultureInfo.InvariantCulture) : string.Empty;

    private static string Line(params string[] fields) =>
        string.Join(Separator, fields.Select(Escape));

    private static string Escape(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        if (field.Contains(Separator, StringComparison.Ordinal) ||
            field.Contains('"', StringComparison.Ordinal) ||
            field.Contains('\n', StringComparison.Ordinal))
        {
            return $"\"{field.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return field;
    }
}
