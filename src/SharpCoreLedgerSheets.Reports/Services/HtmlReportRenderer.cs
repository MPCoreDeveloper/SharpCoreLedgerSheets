using System.Globalization;
using SharpCoreLedgerSheets.Reports.Abstractions;
using SharpCoreLedgerSheets.Reports.Models;
using SharpCoreLedgerSheets.Reports.Reporting;

namespace SharpCoreLedgerSheets.Reports.Services;

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
    public Task<string> RenderHtmlAsync<TModel>(string templateName, TModel model, CancellationToken ct = default)
        where TModel : ReportBaseViewModel
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(model);

        var metadata = model.Metadata;

        var viewData = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ReportCss"] = ReportAssets.Css,
            ["ReportLogo"] = ReportAssets.LogoDataUri,
            ["Labels"] = labels,
            ["Title"] = metadata.Title,
            ["Description"] = metadata.Description,
            ["Administration"] = metadata.Administration,
            ["Period"] = metadata.Period,
            ["GeneratedAt"] = metadata.GeneratedOn.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
        };

        return razorRenderer.RenderAsync($"/Templates/{templateName}", model, viewData, ct);
    }
}
