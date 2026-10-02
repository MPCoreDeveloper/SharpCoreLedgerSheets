using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Web.Resources;
using Microsoft.Extensions.Localization;

namespace FinOverzicht.Web.Localization;

/// <summary>
/// Supplies the report engine with labels from the shared resource files, so every report follows
/// the culture of the current request.
/// </summary>
public sealed class LocalizedReportLabels(IStringLocalizer<SharedResource> localizer) : IReportLabels
{
    /// <inheritdoc />
    public string Balans => localizer[nameof(Balans)];

    /// <inheritdoc />
    public string WinstEnVerlies => localizer[nameof(WinstEnVerlies)];

    /// <inheritdoc />
    public string Kasstroomoverzicht => localizer[nameof(Kasstroomoverzicht)];

    /// <inheritdoc />
    public string Ratio => localizer[nameof(Ratio)];

    /// <inheritdoc />
    public string Activa => localizer[nameof(Activa)];

    /// <inheritdoc />
    public string Passiva => localizer[nameof(Passiva)];

    /// <inheritdoc />
    public string EigenVermogen => localizer[nameof(EigenVermogen)];

    /// <inheritdoc />
    public string Resultaat => localizer[nameof(Resultaat)];

    /// <inheritdoc />
    public string Opbrengsten => localizer[nameof(Opbrengsten)];

    /// <inheritdoc />
    public string Kosten => localizer[nameof(Kosten)];

    /// <inheritdoc />
    public string Totaal => localizer[nameof(Totaal)];

    /// <inheritdoc />
    public string Operationeel => localizer[nameof(Operationeel)];

    /// <inheritdoc />
    public string Investering => localizer[nameof(Investering)];

    /// <inheritdoc />
    public string Financiering => localizer[nameof(Financiering)];

    /// <inheritdoc />
    public string BeginSaldo => localizer[nameof(BeginSaldo)];

    /// <inheritdoc />
    public string EindSaldo => localizer[nameof(EindSaldo)];

    /// <inheritdoc />
    public string NettoMutatie => localizer[nameof(NettoMutatie)];

    /// <inheritdoc />
    public string CurrentRatio => localizer[nameof(CurrentRatio)];

    /// <inheritdoc />
    public string QuickRatio => localizer[nameof(QuickRatio)];

    /// <inheritdoc />
    public string Solvabiliteit => localizer[nameof(Solvabiliteit)];

    /// <inheritdoc />
    public string NettoWinstmarge => localizer[nameof(NettoWinstmarge)];

    /// <inheritdoc />
    public string RentabiliteitEigenVermogen => localizer[nameof(RentabiliteitEigenVermogen)];

    /// <inheritdoc />
    public string Rekening => localizer[nameof(Rekening)];

    /// <inheritdoc />
    public string Omschrijving => localizer[nameof(Omschrijving)];

    /// <inheritdoc />
    public string Bedrag => localizer[nameof(Bedrag)];

    /// <inheritdoc />
    public string GeenGegevens => localizer[nameof(GeenGegevens)];

    /// <inheritdoc />
    public string GegenereerdOp => localizer[nameof(GegenereerdOp)];

    /// <inheritdoc />
    public string Periode => localizer[nameof(Periode)];

    /// <inheritdoc />
    public string InBalans => localizer[nameof(InBalans)];

    /// <inheritdoc />
    public string NietInBalans => localizer[nameof(NietInBalans)];

    /// <inheritdoc />
    public string OverzichtTitel => localizer[nameof(OverzichtTitel)];

    /// <inheritdoc />
    public string OverzichtOmschrijving => localizer[nameof(OverzichtOmschrijving)];

    /// <inheritdoc />
    public string this[string sleutel] => localizer[sleutel];
}
