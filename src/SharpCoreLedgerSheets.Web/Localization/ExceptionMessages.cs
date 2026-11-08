using System.Globalization;
using Microsoft.Extensions.Localization;
using SharpCoreLedgerSheets.Application.Exceptions;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Web.Localization;

/// <summary>
/// Turns an expected failure into a message in the language of the current request.
/// </summary>
/// <remarks>
/// The engine raises <see cref="DomainException"/> and <see cref="NotFoundException"/> with a
/// <see cref="MessageCode"/> plus the values the message needs. This class resolves
/// <c>Msg_&lt;code&gt;</c> from the shared resources, so one rule speaks Dutch, English or German. When a
/// culture has no translation the message the exception carries is shown (Dutch, the primary market), and
/// when the exception carries no code at all the message is passed through unchanged.
/// </remarks>
public static class ExceptionMessages
{
    /// <summary>The resource-key prefix used for translatable engine messages.</summary>
    public const string KeyPrefix = "Msg_";

    /// <summary>Returns the message to show for <paramref name="exception"/>.</summary>
    /// <param name="exception">The expected failure that was caught.</param>
    /// <param name="localizer">The localizer of the current request culture.</param>
    public static string Describe(Exception exception, IStringLocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(localizer);

        var fallback = exception.Message;
        string? code = null;
        IReadOnlyList<object?> arguments = [];

        switch (exception)
        {
            case DomainException domain:
                code = domain.Code;
                arguments = domain.Arguments;
                break;

            case NotFoundException notFound:
                code = notFound.Code;
                arguments = notFound.Arguments;
                break;
        }

        if (code is null)
        {
            return fallback;
        }

        var template = localizer[KeyPrefix + code];

        if (template.ResourceNotFound || string.IsNullOrWhiteSpace(template.Value))
        {
            return fallback;
        }

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template.Value, [.. arguments]);
        }
        catch (FormatException)
        {
            // A translation with a broken placeholder must never hide the failure itself.
            return fallback;
        }
    }
}
