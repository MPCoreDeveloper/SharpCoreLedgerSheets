using System.Globalization;
using System.Text;
using FinOverzicht.Domain.Rapporteren;
using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Reports.Models;

namespace FinOverzicht.Reports.Services;

/// <summary>
/// Exports reports to RFC 4180 comma-separated values.
/// </summary>
/// <remarks>
/// The separator is a comma and all numbers are written with the invariant culture, so the files
/// import predictably into any spreadsheet regardless of the machine's regional settings.
/// </remarks>
public sealed class CsvReportExporter(IReportLabels labels) : IReportExporter
{
    private const char Scheidingsteken = ',';

    /// <inheritdoc />
    public string NaarCsv(FinancieelOverzichtModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = new StringBuilder();
        Kop(builder, model);
        BalansRegels(builder, model.Overzicht.Balans);
        return builder.ToString();
    }

    /// <inheritdoc />
    public string NaarVolledigeCsv(FinancieelOverzichtModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = new StringBuilder();
        Kop(builder, model);

        BalansRegels(builder, model.Overzicht.Balans);
        WinstVerliesRegels(builder, model.Overzicht.WinstVerlies);
        KasstroomRegels(builder, model.Overzicht.Kasstroom);
        RatioRegels(builder, model.Overzicht.Ratio);

        return builder.ToString();
    }

    private static void Kop(StringBuilder builder, FinancieelOverzichtModel model)
    {
        var metadata = model.Metadata;

        builder.AppendLine(Regel(metadata.Titel));

        if (!string.IsNullOrWhiteSpace(metadata.Administratie))
        {
            builder.AppendLine(Regel(metadata.Administratie));
        }

        if (!string.IsNullOrWhiteSpace(metadata.Periode))
        {
            builder.AppendLine(Regel(metadata.Periode));
        }

        builder.AppendLine(Regel(metadata.GegenereerdOp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)));
        builder.AppendLine();
    }

    private void BalansRegels(StringBuilder builder, Balans balans)
    {
        builder.AppendLine(Regel(labels.Balans));
        builder.AppendLine(Regel(labels.Rekening, labels.Omschrijving, labels.Bedrag));

        Sectie(builder, labels.Activa, balans.Activa);
        builder.AppendLine(Regel(string.Empty, $"{labels.Totaal} {labels.Activa}", Bedrag(balans.TotaalActiva)));
        builder.AppendLine();

        Sectie(builder, labels.Passiva, balans.Passiva);
        builder.AppendLine(Regel(string.Empty, $"{labels.Totaal} {labels.Passiva}", Bedrag(balans.TotaalPassiva)));
        builder.AppendLine();

        Sectie(builder, labels.EigenVermogen, balans.EigenVermogen);
        builder.AppendLine(Regel(string.Empty, labels.Resultaat, Bedrag(balans.Resultaat)));
        builder.AppendLine(Regel(string.Empty, $"{labels.Totaal} {labels.EigenVermogen}", Bedrag(balans.EigenVermogenInclusiefResultaat)));
        builder.AppendLine();
    }

    private void WinstVerliesRegels(StringBuilder builder, WinstVerlies winstVerlies)
    {
        builder.AppendLine(Regel(labels.WinstEnVerlies));
        builder.AppendLine(Regel(labels.Rekening, labels.Omschrijving, labels.Bedrag));

        Sectie(builder, labels.Opbrengsten, winstVerlies.Opbrengsten);
        builder.AppendLine(Regel(string.Empty, $"{labels.Totaal} {labels.Opbrengsten}", Bedrag(winstVerlies.TotaalOpbrengsten)));

        Sectie(builder, labels.Kosten, winstVerlies.Kosten);
        builder.AppendLine(Regel(string.Empty, $"{labels.Totaal} {labels.Kosten}", Bedrag(winstVerlies.TotaalKosten)));
        builder.AppendLine(Regel(string.Empty, labels.Resultaat, Bedrag(winstVerlies.Resultaat)));
        builder.AppendLine();
    }

    private void KasstroomRegels(StringBuilder builder, KasstroomOverzicht kasstroom)
    {
        builder.AppendLine(Regel(labels.Kasstroomoverzicht));
        builder.AppendLine(Regel(labels.Omschrijving, labels.Bedrag));

        KasstroomSectie(builder, labels.Operationeel, kasstroom.Operationeel, kasstroom.NettoOperationeel);
        KasstroomSectie(builder, labels.Investering, kasstroom.Investering, kasstroom.NettoInvestering);
        KasstroomSectie(builder, labels.Financiering, kasstroom.Financiering, kasstroom.NettoFinanciering);

        builder.AppendLine(Regel(labels.BeginSaldo, Bedrag(kasstroom.BeginSaldo)));
        builder.AppendLine(Regel(labels.NettoMutatie, Bedrag(kasstroom.NettoMutatie)));
        builder.AppendLine(Regel(labels.EindSaldo, Bedrag(kasstroom.EindSaldo)));
        builder.AppendLine();
    }

    private void RatioRegels(StringBuilder builder, RatioOverzicht ratio)
    {
        builder.AppendLine(Regel(labels.Ratio));
        builder.AppendLine(Regel(labels.Omschrijving, labels.Bedrag));

        builder.AppendLine(Regel(labels.CurrentRatio, RatioWaarde(ratio.CurrentRatio)));
        builder.AppendLine(Regel(labels.QuickRatio, RatioWaarde(ratio.QuickRatio)));
        builder.AppendLine(Regel(labels.Solvabiliteit, Percentage(ratio.Solvabiliteit)));
        builder.AppendLine(Regel(labels.NettoWinstmarge, Percentage(ratio.NettoWinstmarge)));
        builder.AppendLine(Regel(labels.RentabiliteitEigenVermogen, Percentage(ratio.RentabiliteitEigenVermogen)));
    }

    private static void Sectie(StringBuilder builder, string titel, IReadOnlyList<SaldoRegel> regels)
    {
        builder.AppendLine(Regel(titel));

        foreach (var regel in regels)
        {
            builder.AppendLine(Regel(regel.Nummer ?? string.Empty, regel.Omschrijving, Bedrag(regel.Bedrag)));
        }
    }

    private void KasstroomSectie(StringBuilder builder, string titel, IReadOnlyList<SaldoRegel> regels, decimal netto)
    {
        builder.AppendLine(Regel(titel));

        foreach (var regel in regels)
        {
            builder.AppendLine(Regel(string.Empty, regel.Omschrijving, Bedrag(regel.Bedrag)));
        }

        builder.AppendLine(Regel(string.Empty, $"{labels.Totaal} {titel}", Bedrag(netto)));
    }

    private static string Bedrag(decimal waarde) => waarde.ToString("0.00", CultureInfo.InvariantCulture);

    private static string RatioWaarde(decimal? waarde) =>
        waarde.HasValue ? waarde.Value.ToString("0.00", CultureInfo.InvariantCulture) : string.Empty;

    private static string Percentage(decimal? waarde) =>
        waarde.HasValue ? (waarde.Value * 100m).ToString("0.0", CultureInfo.InvariantCulture) : string.Empty;

    private static string Regel(params string[] velden) =>
        string.Join(Scheidingsteken, velden.Select(Escape));

    private static string Escape(string veld)
    {
        if (string.IsNullOrEmpty(veld))
        {
            return string.Empty;
        }

        if (veld.Contains(Scheidingsteken, StringComparison.Ordinal) ||
            veld.Contains('"', StringComparison.Ordinal) ||
            veld.Contains('\n', StringComparison.Ordinal))
        {
            return $"\"{veld.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return veld;
    }
}
