using System.Globalization;

namespace SharpCoreLedgerSheets.Application.Exceptions;

/// <summary>
/// Raised when a requested aggregate cannot be found.
/// </summary>
/// <remarks>
/// Like <see cref="Domain.Common.DomainException"/>, a not-found raised by a use case carries a
/// <see cref="MessageCode"/> plus its values, so the host can show the message in the user's language.
/// </remarks>
public sealed class NotFoundException : Exception
{
    /// <summary>Creates a new exception with the given message.</summary>
    public NotFoundException(string message)
        : base(message) => Arguments = [];

    /// <summary>Creates a new exception with the given message and inner exception.</summary>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException) => Arguments = [];

    /// <summary>Creates a new exception with a translatable message code.</summary>
    /// <param name="code">One of the <see cref="Domain.Common.MessageCode"/> constants.</param>
    /// <param name="messageFormat">The Dutch fallback message, with <c>{0}</c>-style placeholders.</param>
    /// <param name="arguments">The values inserted into the message.</param>
    public NotFoundException(string code, string messageFormat, params object?[] arguments)
        : base(Format(messageFormat, arguments))
    {
        Code = code;
        Arguments = arguments;
    }

    /// <summary>The <see cref="Domain.Common.MessageCode"/> that identifies this message, when it has one.</summary>
    public string? Code { get; }

    /// <summary>The values inserted into the message, in placeholder order.</summary>
    public IReadOnlyList<object?> Arguments { get; }

    private static string Format(string format, object?[] arguments) =>
        arguments.Length == 0 ? format : string.Format(CultureInfo.InvariantCulture, format, arguments);
}

