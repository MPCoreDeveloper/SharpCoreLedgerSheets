namespace FinOverzicht.Domain.Common;

/// <summary>
/// Base exception for violations of domain (business) rules.
/// </summary>
/// <remarks>
/// Domain code never throws generic framework exceptions for rule violations; it throws
/// <see cref="DomeinException"/> so the application layer can translate it consistently.
/// </remarks>
public sealed class DomeinException : Exception
{
    /// <summary>Creates a new domain exception with the given message.</summary>
    public DomeinException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a new domain exception with the given message and inner exception.</summary>
    public DomeinException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
