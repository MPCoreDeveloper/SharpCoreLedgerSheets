namespace FinOverzicht.Reports.Abstractions;

/// <summary>
/// Raised when a report cannot be rendered.
/// </summary>
public sealed class ReportRenderingException : Exception
{
    /// <summary>Creates a new exception.</summary>
    public ReportRenderingException(string message, string? rapportNaam = null, Exception? innerException = null)
        : base(message, innerException) => RapportNaam = rapportNaam;

    /// <summary>The template or report that failed, when known.</summary>
    public string? RapportNaam { get; }
}
