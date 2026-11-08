using Microsoft.AspNetCore.Mvc.Razor;

namespace SharpCoreLedgerSheets.Reports.Services;

/// <summary>
/// Extends the Razor view engine search paths so it can locate report templates stored under
/// <c>/Templates/</c> in the SharpCoreLedgerSheets.Reports class library.
/// </summary>
/// <remarks>
/// Without this expander <c>IRazorViewEngine</c> only searches the default MVC locations
/// (<c>/Views/{controller}/</c>, <c>/Views/Shared/</c>, ...) and never finds compiled
/// <c>/Templates/</c> views.
/// </remarks>
internal sealed class ReportViewLocationExpander : IViewLocationExpander
{
    private static readonly string[] ExtraLocations =
    [
        "/Templates/{0}.cshtml",
        "/Templates/Shared/{0}.cshtml",
    ];

    /// <inheritdoc />
    public void PopulateValues(ViewLocationExpanderContext context)
    {
        // No context values are needed; the paths are static.
    }

    /// <inheritdoc />
    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        ArgumentNullException.ThrowIfNull(viewLocations);

        // Prepend our locations so they are tried before the default MVC paths.
        return ExtraLocations.Concat(viewLocations);
    }
}
