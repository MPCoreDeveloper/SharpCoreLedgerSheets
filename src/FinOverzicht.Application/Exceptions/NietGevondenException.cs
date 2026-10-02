namespace FinOverzicht.Application.Exceptions;

/// <summary>
/// Raised when a requested aggregate cannot be found.
/// </summary>
public sealed class NietGevondenException : Exception
{
    /// <summary>Creates a new exception with the given message.</summary>
    public NietGevondenException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a new exception with the given message and inner exception.</summary>
    public NietGevondenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
