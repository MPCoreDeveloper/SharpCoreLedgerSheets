namespace LedgerSheets.Application.Exceptions;

/// <summary>
/// Raised when a requested aggregate cannot be found.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Creates a new exception with the given message.</summary>
    public NotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a new exception with the given message and inner exception.</summary>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
