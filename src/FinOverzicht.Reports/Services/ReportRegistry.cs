using FinOverzicht.Reports.Models;

namespace FinOverzicht.Reports.Services;

/// <summary>
/// Registry of the reports the engine can produce.
/// </summary>
/// <remarks>
/// Titles and descriptions are stored as <see cref="Abstractions.IReportLabels"/> keys rather than
/// literal text, so a single registration serves every culture.
/// </remarks>
public sealed class ReportRegistry
{
    private readonly List<ReportDefinition> _rapporten = [];

    /// <summary>Registers a report. The key must be unique.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the key is already registered.</exception>
    public void Registreer(
        string sleutel,
        string titelSleutel,
        string omschrijvingSleutel,
        string templateNaam,
        ReportFormat[]? formaten = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sleutel);
        ArgumentException.ThrowIfNullOrWhiteSpace(titelSleutel);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateNaam);

        if (_rapporten.Any(r => string.Equals(r.Sleutel, sleutel, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Er is al een rapport geregistreerd met sleutel '{sleutel}'.");
        }

        _rapporten.Add(new ReportDefinition(
            sleutel,
            titelSleutel,
            omschrijvingSleutel,
            templateNaam,
            formaten ?? [ReportFormat.Html, ReportFormat.Csv]));
    }

    /// <summary>Returns every registered report.</summary>
    public IReadOnlyList<ReportDefinition> Alle() => _rapporten.AsReadOnly();

    /// <summary>Finds a report by key (case-insensitive).</summary>
    public ReportDefinition? Zoek(string sleutel) =>
        _rapporten.FirstOrDefault(r => string.Equals(r.Sleutel, sleutel, StringComparison.OrdinalIgnoreCase));

    /// <summary>Describes a registered report.</summary>
    /// <param name="Sleutel">The unique key.</param>
    /// <param name="TitelSleutel">The label key that resolves the title.</param>
    /// <param name="OmschrijvingSleutel">The label key that resolves the description.</param>
    /// <param name="TemplateNaam">The Razor template name.</param>
    /// <param name="Formaten">The supported output formats.</param>
    public sealed record ReportDefinition(
        string Sleutel,
        string TitelSleutel,
        string OmschrijvingSleutel,
        string TemplateNaam,
        ReportFormat[] Formaten);

    /// <summary>Creates the registry with FinOverzicht's four built-in reports.</summary>
    public static ReportRegistry MetStandaardRapporten()
    {
        var registry = new ReportRegistry();
        registry.Registreer(ReportSleutels.Overzicht, nameof(Abstractions.IReportLabels.OverzichtTitel), nameof(Abstractions.IReportLabels.OverzichtOmschrijving), "FinancieelOverzichtReport");
        registry.Registreer(ReportSleutels.Balans, nameof(Abstractions.IReportLabels.Balans), nameof(Abstractions.IReportLabels.Activa), "BalansReport");
        registry.Registreer(ReportSleutels.WinstVerlies, nameof(Abstractions.IReportLabels.WinstEnVerlies), nameof(Abstractions.IReportLabels.Opbrengsten), "WinstVerliesReport");
        registry.Registreer(ReportSleutels.Kasstroom, nameof(Abstractions.IReportLabels.Kasstroomoverzicht), nameof(Abstractions.IReportLabels.Operationeel), "KasstroomReport");
        return registry;
    }
}
