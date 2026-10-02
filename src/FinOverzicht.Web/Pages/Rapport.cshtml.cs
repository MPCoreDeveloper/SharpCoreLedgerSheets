using System.Text;
using FinOverzicht.Application.Models;
using FinOverzicht.Application.Services;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Reports.Models;
using FinOverzicht.Reports.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinOverzicht.Web.Pages;

/// <summary>
/// Report viewer: picks a report and a year, shows the rendered HTML in a frame and offers the
/// same content as a CSV download.
/// </summary>
public sealed class RapportModel(
    IRapportageService rapportage,
    IReportRenderer renderer,
    IReportExporter exporter,
    IReportLabels labels,
    ReportRegistry registry) : FinPageModel
{
    public int Jaar { get; private set; }

    public string Sleutel { get; private set; } = ReportSleutels.Overzicht;

    public IReadOnlyList<ReportRegistry.ReportDefinition> Rapporten { get; } = registry.Alle();

    public string TitelVan(ReportRegistry.ReportDefinition definitie) => labels[definitie.TitelSleutel];

    public string OmschrijvingVan(ReportRegistry.ReportDefinition definitie) => labels[definitie.OmschrijvingSleutel];

    public void OnGet(string? rapport, int? jaar)
    {
        Jaar = jaar ?? DateTime.Today.Year;
        Sleutel = ZoekDefinitie(rapport).Sleutel;
    }

    /// <summary>Returns the rendered report as a standalone HTML document (loaded in a frame).</summary>
    public async Task<IActionResult> OnGetHtmlAsync(string? rapport, int? jaar, CancellationToken ct)
    {
        var (definitie, model) = await BouwModelAsync(rapport, jaar ?? DateTime.Today.Year, ct);
        var html = await renderer.RenderHtmlAsync(definitie.TemplateNaam, model, ct);
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>Returns the complete overview (all statements plus ratios) as CSV.</summary>
    public async Task<IActionResult> OnGetCsvAsync(string? rapport, int? jaar, CancellationToken ct)
    {
        var jaarWaarde = jaar ?? DateTime.Today.Year;
        var (definitie, model) = await BouwModelAsync(rapport, jaarWaarde, ct);
        var csv = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(exporter.NaarVolledigeCsv(model)))
            .ToArray();

        return File(csv, "text/csv; charset=utf-8", $"finoverzicht-{definitie.Sleutel}-{jaarWaarde}.csv");
    }

    private ReportRegistry.ReportDefinition ZoekDefinitie(string? rapport) =>
        registry.Zoek(rapport ?? ReportSleutels.Overzicht) ?? registry.Zoek(ReportSleutels.Overzicht)!;

    private async Task<(ReportRegistry.ReportDefinition Definitie, FinancieelOverzichtModel Model)> BouwModelAsync(
        string? rapport,
        int jaar,
        CancellationToken ct)
    {
        var definitie = ZoekDefinitie(rapport);
        var periode = Periode.VoorJaar(jaar);
        var overzicht = await rapportage.StelOverzichtOpAsync(periode, ct).ConfigureAwait(false);

        var model = new FinancieelOverzichtModel(overzicht)
        {
            Metadata = new ReportMetadata
            {
                Titel = labels[definitie.TitelSleutel],
                Omschrijving = labels[definitie.OmschrijvingSleutel],
                Periode = $"{periode.Start:d} – {periode.Eind:d}",
                GegenereerdOp = DateTime.UtcNow,
            },
        };

        return (definitie, model);
    }
}
