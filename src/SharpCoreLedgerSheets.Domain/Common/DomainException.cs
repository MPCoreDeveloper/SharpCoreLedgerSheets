using System.Globalization;

namespace SharpCoreLedgerSheets.Domain.Common;

/// <summary>
/// Base exception for violations of domain (business) rules.
/// </summary>
/// <remarks>
/// Domain code never throws generic framework exceptions for rule violations; it throws
/// <see cref="DomainException"/> so the application layer can translate it consistently. A rule that a
/// user can trigger carries a <see cref="MessageCode"/> plus the values the message needs, so the host can
/// show the message in the user's language; <see cref="Exception.Message"/> stays the Dutch fallback.
/// </remarks>
public sealed class DomainException : Exception
{
    /// <summary>Creates a new domain exception with the given message.</summary>
    public DomainException(string message)
        : base(message) => Arguments = [];

    /// <summary>Creates a new domain exception with the given message and inner exception.</summary>
    public DomainException(string message, Exception innerException)
        : base(message, innerException) => Arguments = [];

    /// <summary>Creates a new domain exception with a translatable message code.</summary>
    /// <param name="code">One of the <see cref="MessageCode"/> constants.</param>
    /// <param name="messageFormat">The Dutch fallback message, with <c>{0}</c>-style placeholders.</param>
    /// <param name="arguments">The values inserted into the message.</param>
    public DomainException(string code, string messageFormat, params object?[] arguments)
        : base(Format(messageFormat, arguments))
    {
        Code = code;
        Arguments = arguments;
    }

    /// <summary>The <see cref="MessageCode"/> that identifies this message, when it has one.</summary>
    public string? Code { get; }

    /// <summary>The values inserted into the message, in placeholder order.</summary>
    public IReadOnlyList<object?> Arguments { get; }

    private static string Format(string format, object?[] arguments) =>
        arguments.Length == 0 ? format : string.Format(CultureInfo.InvariantCulture, format, arguments);
}

