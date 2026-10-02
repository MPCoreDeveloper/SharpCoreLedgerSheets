using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Reports.Models;
using FinOverzicht.Reports.Reporting;
using FinOverzicht.Reports.Services;

namespace FinOverzicht.Reports.Tests;

public sealed class CsvReportExporterTests
{
    private readonly CsvReportExporter _exporter = new(new NederlandseReportLabels());
    private readonly FinancieelOverzichtModel _model = TestOverzicht.MaakModel();

    [Fact]
    public void NaarCsv_ContainsHeaderAndBalanceTotals()
    {
        var csv = _exporter.NaarCsv(_model);

        Assert.Contains("Financieel overzicht", csv);
        Assert.Contains("Balans", csv);
        Assert.Contains("Activa", csv);
        Assert.Contains("9800.00", csv);
        Assert.Contains("3600.00", csv);
    }

    [Fact]
    public void NaarCsv_UsesInvariantNumbersAndCommaSeparator()
    {
        var csv = _exporter.NaarCsv(_model);

        Assert.Contains("9800.00", csv);
        Assert.DoesNotContain("9800,00", csv);
        Assert.Contains(csv.Split('\n'), line => line.TrimEnd('\r') == ",Totaal Activa,9800.00");
    }

    [Fact]
    public void NaarVolledigeCsv_ContainsAllStatementsAndRatios()
    {
        var csv = _exporter.NaarVolledigeCsv(_model);

        Assert.Contains("Balans", csv);
        Assert.Contains("Winst- en verliesrekening", csv);
        Assert.Contains("Kasstroomoverzicht", csv);
        Assert.Contains("Kengetallen", csv);
        Assert.Contains("Current ratio", csv);
        Assert.Contains("Solvabiliteit", csv);
    }

    [Fact]
    public void NaarVolledigeCsv_ContainsCashFlowReconciliation()
    {
        var csv = _exporter.NaarVolledigeCsv(_model);

        Assert.Contains("Beginsaldo", csv);
        Assert.Contains("Netto kasstroom", csv);
        Assert.Contains("Eindsaldo", csv);
    }

    [Fact]
    public void NaarVolledigeCsv_QuotesValuesContainingSeparator()
    {
        var model = new FinancieelOverzichtModel(TestOverzicht.Maak())
        {
            Metadata = new ReportMetadata
            {
                Titel = "Overzicht, compleet",
                GegenereerdOp = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            },
        };

        var csv = _exporter.NaarVolledigeCsv(model);

        Assert.Contains("\"Overzicht, compleet\"", csv);
    }

    [Fact]
    public void NaarCsv_ThrowsForNullModel()
    {
        Assert.Throws<ArgumentNullException>(() => _exporter.NaarCsv(null!));
    }
}
