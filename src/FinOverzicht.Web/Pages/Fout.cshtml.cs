using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinOverzicht.Web.Pages;

/// <summary>Generic error page used by the production exception handler.</summary>
public sealed class FoutModel : PageModel
{
    public void OnGet()
    {
        // Nothing to load; the page only renders a friendly message.
    }
}
