using LedgerSheets.Application.Exceptions;
using LedgerSheets.Domain.Common;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LedgerSheets.Web.Pages;

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
            ErrorMessage = ex.Message;
        }
        catch (NotFoundException ex)
        {
            ErrorMessage = ex.Message;
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
}
