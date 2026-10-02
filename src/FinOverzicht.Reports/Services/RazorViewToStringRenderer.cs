using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Reports.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace FinOverzicht.Reports.Services;

/// <summary>
/// Renders a Razor view to an HTML string outside of an HTTP request, using a synthetic
/// <see cref="ActionContext"/> so report templates can be rendered from background services
/// or any host.
/// </summary>
public sealed class RazorViewToStringRenderer(
    IRazorViewEngine viewEngine,
    ITempDataProvider tempDataProvider,
    IServiceProvider serviceProvider)
{
    /// <summary>Renders the view at <paramref name="viewPad"/> with the given model.</summary>
    /// <typeparam name="TModel">The report model type.</typeparam>
    /// <param name="viewPad">View path without extension, for example <c>/Templates/BalansReport</c>.</param>
    /// <param name="model">The view model.</param>
    /// <param name="extraViewData">Extra ViewData entries (CSS, labels, ...).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ReportRenderingException">Thrown when the view cannot be found.</exception>
    public async Task<string> RenderAsync<TModel>(
        string viewPad,
        TModel model,
        IDictionary<string, object?> extraViewData,
        CancellationToken ct = default)
        where TModel : ReportBaseViewModel
    {
        ArgumentNullException.ThrowIfNull(extraViewData);
        ct.ThrowIfCancellationRequested();

        var actionContext = MaakActionContext();
        var view = ZoekView(actionContext, viewPad);

        var viewData = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model,
        };

        foreach (var (sleutel, waarde) in extraViewData)
        {
            viewData[sleutel] = waarde;
        }

        await using var output = new StringWriter();

        var viewContext = new ViewContext(
            actionContext,
            view,
            viewData,
            new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
            output,
            new HtmlHelperOptions());

        await view.RenderAsync(viewContext);
        return output.ToString();
    }

    private IView ZoekView(ActionContext actionContext, string viewPad)
    {
        var metExtensie = viewPad.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
            ? viewPad
            : viewPad + ".cshtml";

        // Compiled views are keyed by their full path including the extension.
        var resultaat = viewEngine.GetView(executingFilePath: null, viewPath: metExtensie, isMainPage: true);
        if (resultaat.Success)
        {
            return resultaat.View;
        }

        var zonderExtensie = viewEngine.GetView(executingFilePath: null, viewPath: viewPad, isMainPage: true);
        if (zonderExtensie.Success)
        {
            return zonderExtensie.View;
        }

        // Fall back to FindView with just the view name; ReportViewLocationExpander expands it to
        // /Templates/{viewName}.cshtml so compiled views are still located.
        var viewNaam = Path.GetFileNameWithoutExtension(viewPad.Replace('\\', '/'));
        var gevonden = viewEngine.FindView(actionContext, viewNaam, isMainPage: true);
        if (gevonden.Success)
        {
            return gevonden.View;
        }

        var gezocht = resultaat.SearchedLocations
            .Concat(zonderExtensie.SearchedLocations)
            .Concat(gevonden.SearchedLocations)
            .Distinct(StringComparer.Ordinal);

        throw new ReportRenderingException(
            $"Razor view '{viewPad}' is niet gevonden. Gezochte locaties: {string.Join(", ", gezocht)}",
            viewPad);
    }

    private ActionContext MaakActionContext()
    {
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
    }
}
