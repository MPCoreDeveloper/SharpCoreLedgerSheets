using FinOverzicht.Reports.Models;

namespace FinOverzicht.Reports.Abstractions;

/// <summary>
/// Exports a report model to a comma-separated file for use in spreadsheets.
/// </summary>
public interface IReportExporter
{
    /// <summary>Exports the balance sheet to CSV.</summary>
    string NaarCsv(FinancieelOverzichtModel model);

    /// <summary>Exports the complete overview (all statements plus ratios) to CSV.</summary>
    string NaarVolledigeCsv(FinancieelOverzichtModel model);
}
