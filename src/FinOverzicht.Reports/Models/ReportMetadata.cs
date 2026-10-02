namespace FinOverzicht.Reports.Models;

/// <summary>
/// Metadata that applies to every report.
/// </summary>
public sealed record ReportMetadata
{
    /// <summary>The report title.</summary>
    public required string Titel { get; init; }

    /// <summary>When the report was generated (UTC).</summary>
    public DateTime GegenereerdOp { get; init; } = DateTime.UtcNow;

    /// <summary>Optional subtitle or description.</summary>
    public string? Omschrijving { get; init; }

    /// <summary>The reporting period, already formatted for display.</summary>
    public string? Periode { get; init; }

    /// <summary>The name of the administration (company or person).</summary>
    public string? Administratie { get; init; }

    /// <summary>The formats this report supports.</summary>
    public ReportFormat[] OndersteundeFormaten { get; init; } = [ReportFormat.Html, ReportFormat.Csv];
}
