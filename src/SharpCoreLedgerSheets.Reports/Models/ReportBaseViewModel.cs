namespace SharpCoreLedgerSheets.Reports.Models;

/// <summary>
/// Base type for every report view model.
/// </summary>
public abstract record ReportBaseViewModel
{
    /// <summary>Metadata for this report (title, generation date, period, ...).</summary>
    public required ReportMetadata Metadata { get; init; }
}
