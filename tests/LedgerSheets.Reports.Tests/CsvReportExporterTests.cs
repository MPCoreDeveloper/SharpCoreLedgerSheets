using LedgerSheets.Reports.Abstractions;
using LedgerSheets.Reports.Models;
using LedgerSheets.Reports.Reporting;
using LedgerSheets.Reports.Services;

namespace LedgerSheets.Reports.Tests;

public sealed class CsvReportExporterTests
{
    private readonly CsvReportExporter _exporter = new(new EnglishReportLabels());
    private readonly FinancialOverviewModel _model = TestOverview.CreateModel();

    [Fact]
    public void ToCsv_ContainsHeaderAndBalanceTotals()
    {
        var csv = _exporter.ToCsv(_model);

        Assert.Contains("Financial overview", csv);
        Assert.Contains("Balance sheet", csv);
        Assert.Contains("Assets", csv);
        Assert.Contains("9800.00", csv);
        Assert.Contains("3600.00", csv);
    }

    [Fact]
    public void ToCsv_UsesInvariantNumbersAndCommaSeparator()
    {
        var csv = _exporter.ToCsv(_model);

        Assert.Contains("9800.00", csv);
        Assert.DoesNotContain("9800,00", csv);
        Assert.Contains(csv.Split('\n'), line => line.TrimEnd('\r') == ",Total Assets,9800.00");
    }

    [Fact]
    public void ToCompleteCsv_ContainsAllStatementsAndRatios()
    {
        var csv = _exporter.ToCompleteCsv(_model);

        Assert.Contains("Balance sheet", csv);
        Assert.Contains("Profit and loss statement", csv);
        Assert.Contains("Cash flow statement", csv);
        Assert.Contains("Key ratios", csv);
        Assert.Contains("Current ratio", csv);
        Assert.Contains("Solvency", csv);
    }

    [Fact]
    public void ToCompleteCsv_ContainsCashFlowReconciliation()
    {
        var csv = _exporter.ToCompleteCsv(_model);

        Assert.Contains("Opening balance", csv);
        Assert.Contains("Net cash flow", csv);
        Assert.Contains("Closing balance", csv);
    }

    [Fact]
    public void ToCompleteCsv_QuotesValuesContainingSeparator()
    {
        var model = new FinancialOverviewModel(TestOverview.Create())
        {
            Metadata = new ReportMetadata
            {
                Title = "Overview, compleet",
                GeneratedOn = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            },
        };

        var csv = _exporter.ToCompleteCsv(model);

        Assert.Contains("\"Overview, compleet\"", csv);
    }

    [Fact]
    public void ToCsv_ThrowsForNullModel()
    {
        Assert.Throws<ArgumentNullException>(() => _exporter.ToCsv(null!));
    }
}
