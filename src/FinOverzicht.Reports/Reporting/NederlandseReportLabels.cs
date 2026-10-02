using System.Globalization;
using FinOverzicht.Reports.Abstractions;

namespace FinOverzicht.Reports.Reporting;

/// <summary>
/// Dutch labels, used as the built-in default so the report engine works without a host.
/// </summary>
/// <remarks>
/// The web application replaces this registration with an <c>IStringLocalizer</c>-backed
/// implementation so reports follow the request culture.
/// </remarks>
public sealed class NederlandseReportLabels : IReportLabels
{
    /// <inheritdoc />
    public string Balans => "Balans";

    /// <inheritdoc />
    public string WinstEnVerlies => "Winst- en verliesrekening";

    /// <inheritdoc />
    public string Kasstroomoverzicht => "Kasstroomoverzicht";

    /// <inheritdoc />
    public string Ratio => "Kengetallen";

    /// <inheritdoc />
    public string Activa => "Activa";

    /// <inheritdoc />
    public string Passiva => "Passiva";

    /// <inheritdoc />
    public string EigenVermogen => "Eigen vermogen";

    /// <inheritdoc />
    public string Resultaat => "Resultaat";

    /// <inheritdoc />
    public string Opbrengsten => "Opbrengsten";

    /// <inheritdoc />
    public string Kosten => "Kosten";

    /// <inheritdoc />
    public string Totaal => "Totaal";

    /// <inheritdoc />
    public string Operationeel => "Operationele activiteiten";

    /// <inheritdoc />
    public string Investering => "Investeringsactiviteiten";

    /// <inheritdoc />
    public string Financiering => "Financieringsactiviteiten";

    /// <inheritdoc />
    public string BeginSaldo => "Beginsaldo";

    /// <inheritdoc />
    public string EindSaldo => "Eindsaldo";

    /// <inheritdoc />
    public string NettoMutatie => "Netto kasstroom";

    /// <inheritdoc />
    public string CurrentRatio => "Current ratio";

    /// <inheritdoc />
    public string QuickRatio => "Quick ratio";

    /// <inheritdoc />
    public string Solvabiliteit => "Solvabiliteit";

    /// <inheritdoc />
    public string NettoWinstmarge => "Netto winstmarge";

    /// <inheritdoc />
    public string RentabiliteitEigenVermogen => "Rentabiliteit eigen vermogen";

    /// <inheritdoc />
    public string Rekening => "Rekening";

    /// <inheritdoc />
    public string Omschrijving => "Omschrijving";

    /// <inheritdoc />
    public string Bedrag => "Bedrag";

    /// <inheritdoc />
    public string GeenGegevens => "Geen gegevens beschikbaar voor deze periode.";

    /// <inheritdoc />
    public string GegenereerdOp => "Gegenereerd op";

    /// <inheritdoc />
    public string Periode => "Periode";

    /// <inheritdoc />
    public string InBalans => "In balans";

    /// <inheritdoc />
    public string NietInBalans => "Niet in balans";

    /// <inheritdoc />
    public string OverzichtTitel => "Financieel overzicht";

    /// <inheritdoc />
    public string OverzichtOmschrijving => "Balans, resultaat, kasstroom en kengetallen in een document.";

    /// <inheritdoc />
    public string this[string sleutel] => sleutel switch
    {
        nameof(Balans) => Balans,
        nameof(WinstEnVerlies) => WinstEnVerlies,
        nameof(Kasstroomoverzicht) => Kasstroomoverzicht,
        nameof(Ratio) => Ratio,
        nameof(Activa) => Activa,
        nameof(Passiva) => Passiva,
        nameof(EigenVermogen) => EigenVermogen,
        nameof(Resultaat) => Resultaat,
        nameof(Opbrengsten) => Opbrengsten,
        nameof(Kosten) => Kosten,
        nameof(Totaal) => Totaal,
        nameof(Operationeel) => Operationeel,
        nameof(Investering) => Investering,
        nameof(Financiering) => Financiering,
        nameof(BeginSaldo) => BeginSaldo,
        nameof(EindSaldo) => EindSaldo,
        nameof(NettoMutatie) => NettoMutatie,
        nameof(CurrentRatio) => CurrentRatio,
        nameof(QuickRatio) => QuickRatio,
        nameof(Solvabiliteit) => Solvabiliteit,
        nameof(NettoWinstmarge) => NettoWinstmarge,
        nameof(RentabiliteitEigenVermogen) => RentabiliteitEigenVermogen,
        nameof(Rekening) => Rekening,
        nameof(Omschrijving) => Omschrijving,
        nameof(Bedrag) => Bedrag,
        nameof(GeenGegevens) => GeenGegevens,
        nameof(GegenereerdOp) => GegenereerdOp,
        nameof(Periode) => Periode,
        nameof(InBalans) => InBalans,
        nameof(NietInBalans) => NietInBalans,
        nameof(OverzichtTitel) => OverzichtTitel,
        nameof(OverzichtOmschrijving) => OverzichtOmschrijving,
        _ => throw new ArgumentOutOfRangeException(
            nameof(sleutel),
            sleutel,
            string.Create(CultureInfo.InvariantCulture, $"Onbekende label-sleutel: '{sleutel}'.")),
    };
}
