namespace SharpCoreLedgerSheets.Reports.Models;

/// <summary>
/// Output formats supported by the SharpCoreLedgerSheets report engine.
/// </summary>
/// <remarks>
/// PDF is deliberately not generated in-process: an HTML report is self-contained and
/// print-ready, so the browser's "Save as PDF" produces the PDF without shipping a
/// multi-hundred-megabyte headless browser inside this package.
/// </remarks>
public enum ReportFormat
{
    /// <summary>A self-contained, print-ready HTML document.</summary>
    Html,

    /// <summary>A comma-separated file suitable for spreadsheets.</summary>
    Csv,
}
