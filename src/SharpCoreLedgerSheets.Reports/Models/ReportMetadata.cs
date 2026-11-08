namespace SharpCoreLedgerSheets.Reports.Models;

/// <summary>
/// Metadata that applies to every report.
/// </summary>
public sealed record ReportMetadata
{
    /// <summary>The report title.</summary>
    public required string Title { get; init; }

    /// <summary>When the report was generated (UTC).</summary>
    public DateTime GeneratedOn { get; init; } = DateTime.UtcNow;

    /// <summary>Optional subtitle or description.</summary>
    public string? Description { get; init; }

    /// <summary>The reporting period, already formatted for display.</summary>
    public string? Period { get; init; }

    /// <summary>The name of the administration (company or person).</summary>
    public string? Administration { get; init; }

    /// <summary>The formats this report supports.</summary>
    public ReportFormat[] SupportedFormaten { get; init; } = [ReportFormat.Html, ReportFormat.Csv];
}
