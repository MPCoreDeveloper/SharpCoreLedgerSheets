using SharpCoreLedgerSheets.Reports.Models;

namespace SharpCoreLedgerSheets.Reports.Abstractions;

/// <summary>
/// Exports a report model to a comma-separated file for use in spreadsheets.
/// </summary>
public interface IReportExporter
{
    /// <summary>Exports the balance sheet to CSV.</summary>
    string ToCsv(FinancialOverviewModel model);

    /// <summary>Exports the complete overview (all statements plus ratios) to CSV.</summary>
    string ToCompleteCsv(FinancialOverviewModel model);
}
