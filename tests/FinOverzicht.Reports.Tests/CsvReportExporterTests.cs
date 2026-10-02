using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Reports.Models;
using FinOverzicht.Reports.Reporting;
using FinOverzicht.Reports.Services;
using FluentAssertions;

namespace FinOverzicht.Reports.Tests;

public sealed class CsvReportExporterTests
{
    private readonly CsvReportExporter _exporter = new(new NederlandseReportLabels());
    private readonly FinancieelOverzichtModel _model = TestOverzicht.MaakModel();

    [Fact]
    public void NaarCsv_ContainsHeaderAndBalanceTotals()
    {
        var csv = _exporter.NaarCsv(_model);

        csv.Should().Contain("Financieel overzicht");
        csv.Should().Contain("Balans");
        csv.Should().Contain("Activa");
        csv.Should().Contain("9800.00");
        csv.Should().Contain("3600.00");
    }

    [Fact]
    public void NaarCsv_UsesInvariantNumbersAndCommaSeparator()
    {
        var csv = _exporter.NaarCsv(_model);

        csv.Should().Contain("9800.00");
        csv.Should().NotContain("9800,00");
        csv.Split('\n').Should().Contain(line => line.TrimEnd('\r') == ",Totaal Activa,9800.00");
    }

    [Fact]
    public void NaarVolledigeCsv_ContainsAllStatementsAndRatios()
    {
        var csv = _exporter.NaarVolledigeCsv(_model);

        csv.Should().Contain("Balans");
        csv.Should().Contain("Winst- en verliesrekening");
        csv.Should().Contain("Kasstroomoverzicht");
        csv.Should().Contain("Kengetallen");
        csv.Should().Contain("Current ratio");
        csv.Should().Contain("Solvabiliteit");
    }

    [Fact]
    public void NaarVolledigeCsv_ContainsCashFlowReconciliation()
    {
        var csv = _exporter.NaarVolledigeCsv(_model);

        csv.Should().Contain("Beginsaldo");
        csv.Should().Contain("Netto kasstroom");
        csv.Should().Contain("Eindsaldo");
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

        csv.Should().Contain("\"Overzicht, compleet\"");
    }

    [Fact]
    public void NaarCsv_ThrowsForNullModel()
    {
        var act = () => _exporter.NaarCsv(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
