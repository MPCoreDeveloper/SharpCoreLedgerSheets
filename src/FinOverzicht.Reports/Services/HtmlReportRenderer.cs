using System.Globalization;
using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Reports.Models;
using FinOverzicht.Reports.Reporting;

namespace FinOverzicht.Reports.Services;

/// <summary>
/// Renders a report to a self-contained HTML document through the Razor template engine.
/// </summary>
/// <remarks>
/// Everything the templates need (styling, labels, header data) is injected via ViewData, so the
/// layout stays free of model typing and the produced document needs no network access at all.
/// </remarks>
public sealed class HtmlReportRenderer(
    RazorViewToStringRenderer razorRenderer,
    IReportLabels labels) : IReportRenderer
{
    /// <inheritdoc />
    public Task<string> RenderHtmlAsync<TModel>(string templateNaam, TModel model, CancellationToken ct = default)
        where TModel : ReportBaseViewModel
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateNaam);
        ArgumentNullException.ThrowIfNull(model);

        var metadata = model.Metadata;

        var viewData = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ReportCss"] = ReportAssets.Css,
            ["Labels"] = labels,
            ["Title"] = metadata.Titel,
            ["Description"] = metadata.Omschrijving,
            ["Administratie"] = metadata.Administratie,
            ["Periode"] = metadata.Periode,
            ["GeneratedAt"] = metadata.GegenereerdOp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
        };

        return razorRenderer.RenderAsync($"/Templates/{templateNaam}", model, viewData, ct);
    }
}
