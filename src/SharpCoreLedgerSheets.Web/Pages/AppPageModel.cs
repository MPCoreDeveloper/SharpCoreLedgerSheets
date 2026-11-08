using SharpCoreLedgerSheets.Application.Exceptions;
using SharpCoreLedgerSheets.Domain.Common;
using SharpCoreLedgerSheets.Web.Localization;
using SharpCoreLedgerSheets.Web.Resources;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace SharpCoreLedgerSheets.Web.Pages;

/// <summary>
/// Base page model that turns expected failures (domain rules, missing data, storage not
/// configured) into a readable message instead of an unhandled exception.
/// </summary>
public abstract class AppPageModel : PageModel
{
    /// <summary>A user-facing error message, or <see langword="null"/> when nothing went wrong.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Runs an operation, capturing expected failures into <see cref="ErrorMessage"/>.</summary>
    protected async Task TryAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            ErrorMessage = Describe(ex);
        }
        catch (NotFoundException ex)
        {
            ErrorMessage = Describe(ex);
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // The message is translated on the way out: the engine raises a code, the page shows the language of
    // the request. A page model without an HttpContext (unit tests) falls back to the engine's message.
    private string Describe(Exception exception)
    {
        var localizer = HttpContext?.RequestServices?.GetService<IStringLocalizer<SharedResource>>();

        return localizer is null
            ? exception.Message
            : ExceptionMessages.Describe(exception, localizer);
    }
}
