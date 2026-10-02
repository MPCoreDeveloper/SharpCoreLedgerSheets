using FinOverzicht.Application.Exceptions;
using FinOverzicht.Domain.Common;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinOverzicht.Web.Pages;

/// <summary>
/// Base page model that turns expected failures (domain rules, missing data, storage not
/// configured) into a readable message instead of an unhandled exception.
/// </summary>
public abstract class FinPageModel : PageModel
{
    /// <summary>A user-facing error message, or <see langword="null"/> when nothing went wrong.</summary>
    public string? Foutmelding { get; private set; }

    /// <summary>Runs an operation, capturing expected failures into <see cref="Foutmelding"/>.</summary>
    protected async Task ProbeerAsync(Func<Task> actie)
    {
        try
        {
            await actie().ConfigureAwait(false);
        }
        catch (DomeinException ex)
        {
            Foutmelding = ex.Message;
        }
        catch (NietGevondenException ex)
        {
            Foutmelding = ex.Message;
        }
        catch (ArgumentException ex)
        {
            Foutmelding = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            Foutmelding = ex.Message;
        }
    }
}
