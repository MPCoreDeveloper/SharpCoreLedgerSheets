namespace LedgerSheets.Reports.Abstractions;

/// <summary>
/// Raised when a report cannot be rendered.
/// </summary>
public sealed class ReportRenderingException : Exception
{
    /// <summary>Creates a new exception.</summary>
    public ReportRenderingException(string message, string? reportName = null, Exception? innerException = null)
        : base(message, innerException) => ReportName = reportName;

    /// <summary>The template or report that failed, when known.</summary>
    public string? ReportName { get; }
}
