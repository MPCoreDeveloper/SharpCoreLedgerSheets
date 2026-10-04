using LedgerSheets.Reports.Abstractions;
using LedgerSheets.Reports.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace LedgerSheets.Reports.Services;

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
    /// <summary>Renders the view at <paramref name="viewPath"/> with the given model.</summary>
    /// <typeparam name="TModel">The report model type.</typeparam>
    /// <param name="viewPath">View path without extension, for example <c>/Templates/BalanceSheetReport</c>.</param>
    /// <param name="model">The view model.</param>
    /// <param name="extraViewData">Extra ViewData entries (CSS, labels, ...).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ReportRenderingException">Thrown when the view cannot be found.</exception>
    public async Task<string> RenderAsync<TModel>(
        string viewPath,
        TModel model,
        IDictionary<string, object?> extraViewData,
        CancellationToken ct = default)
        where TModel : ReportBaseViewModel
    {
        ArgumentNullException.ThrowIfNull(extraViewData);
        ct.ThrowIfCancellationRequested();

        var actionContext = CreateActionContext();
        var view = FindView(actionContext, viewPath);

        var viewData = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model,
        };

        foreach (var (key, value) in extraViewData)
        {
            viewData[key] = value;
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

    private IView FindView(ActionContext actionContext, string viewPath)
    {
        var withExtension = viewPath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
            ? viewPath
            : viewPath + ".cshtml";

        // Compiled views are keyed by their full path including the extension.
        var result = viewEngine.GetView(executingFilePath: null, viewPath: withExtension, isMainPage: true);
        if (result.Success)
        {
            return result.View;
        }

        var withoutExtension = viewEngine.GetView(executingFilePath: null, viewPath: viewPath, isMainPage: true);
        if (withoutExtension.Success)
        {
            return withoutExtension.View;
        }

        // Fall back to FindView with just the view name; ReportViewLocationExpander expands it to
        // /Templates/{viewName}.cshtml so compiled views are still located.
        var viewName = Path.GetFileNameWithoutExtension(viewPath.Replace('\\', '/'));
        var found = viewEngine.FindView(actionContext, viewName, isMainPage: true);
        if (found.Success)
        {
            return found.View;
        }

        var searched = result.SearchedLocations
            .Concat(withoutExtension.SearchedLocations)
            .Concat(found.SearchedLocations)
            .Distinct(StringComparer.Ordinal);

        throw new ReportRenderingException(
            $"Razor view '{viewPath}' is niet gevonden. Gezochte locaties: {string.Join(", ", searched)}",
            viewPath);
    }

    private ActionContext CreateActionContext()
    {
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
    }
}
