using System.Text;
using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Reports.Abstractions;
using SharpCoreLedgerSheets.Reports.Models;
using SharpCoreLedgerSheets.Reports.Services;
using Microsoft.AspNetCore.Mvc;

namespace SharpCoreLedgerSheets.Web.Pages;

/// <summary>
/// Report viewer: picks a report and a year, shows the rendered HTML in a frame and offers the
/// same content as a CSV download.
/// </summary>
public sealed class ReportModel(
    IReportingService reporting,
    IAdministrationService administration,
    IReportRenderer renderer,
    IReportExporter exporter,
    IReportLabels labels,
    ReportRegistry registry) : AppPageModel
{
    public int Year { get; private set; }

    public string Key { get; private set; } = ReportKeys.Overview;

    public IReadOnlyList<ReportRegistry.ReportDefinition> Reports { get; } = registry.All();

    public string TitleFrom(ReportRegistry.ReportDefinition definition) => labels[definition.TitleKey];

    public string DescriptionFrom(ReportRegistry.ReportDefinition definition) => labels[definition.DescriptionKey];

    public void OnGet(string? report, int? year)
    {
        Year = year ?? DateTime.Today.Year;
        Key = FindDefinition(report).Key;
    }

    /// <summary>Returns the rendered report as a standalone HTML document (loaded in a frame).</summary>
    public async Task<IActionResult> OnGetHtmlAsync(string? report, int? year, CancellationToken ct)
    {
        var (definition, model) = await BuildModelAsync(report, year ?? DateTime.Today.Year, ct);
        var html = await renderer.RenderHtmlAsync(definition.TemplateName, model, ct);
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>Returns the complete overview (all statements plus ratios) as CSV.</summary>
    public async Task<IActionResult> OnGetCsvAsync(string? report, int? year, CancellationToken ct)
    {
        var yearValue = year ?? DateTime.Today.Year;
        var (definition, model) = await BuildModelAsync(report, yearValue, ct);
        var csv = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(exporter.ToCompleteCsv(model)))
            .ToArray();

        return File(csv, "text/csv; charset=utf-8", $"sharpcoreledgersheets-{definition.Key}-{yearValue}.csv");
    }

    private ReportRegistry.ReportDefinition FindDefinition(string? report) =>
        registry.Find(report ?? ReportKeys.Overview) ?? registry.Find(ReportKeys.Overview)!;

    private async Task<(ReportRegistry.ReportDefinition Definition, FinancialOverviewModel Model)> BuildModelAsync(
        string? report,
        int year,
        CancellationToken ct)
    {
        var definition = FindDefinition(report);
        var settings = await administration.GetSettingsAsync(ct).ConfigureAwait(false);
        var period = settings.PeriodFor(year);
        var overview = await reporting.BuildOverviewAsync(period, ct).ConfigureAwait(false);

        var model = new FinancialOverviewModel(overview)
        {
            Metadata = new ReportMetadata
            {
                Title = labels[definition.TitleKey],
                Description = labels[definition.DescriptionKey],
                Administration = settings.CompanyName,
                Period = $"{period.Start:d} – {period.Closing:d}",
                GeneratedOn = DateTime.UtcNow,
            },
        };

        return (definition, model);
    }
}
