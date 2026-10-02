using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Services;
using FinOverzicht.Application.Models;
using FinOverzicht.Reports.Models;

namespace FinOverzicht.Reports.Tests;

/// <summary>
/// Builds a small, fully consistent financial overview for the report tests.
/// </summary>
/// <remarks>
/// Scenario: cash 5.000 + bank loan 3.000 + cash sale 2.000 - expenses 800 (paid from bank)
/// + inventory 600 on credit. Assets 9.800 = liabilities 3.600 + equity 5.000 + result 1.200.
/// </remarks>
internal static class TestOverzicht
{
    private static readonly DateOnly Datum = new(2026, 6, 15);

    internal static FinancieelOverzicht Maak()
    {
        var grootboek = new Grootboek();
        var kas = grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var bank = grootboek.VoegRekeningToe("1100", "Bank", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var voorraad = grootboek.VoegRekeningToe("1300", "Voorraad", RekeningSoort.Activa, isVlottend: true, isVoorraad: true);
        var crediteuren = grootboek.VoegRekeningToe("2000", "Crediteuren", RekeningSoort.Passiva, isVlottend: true);
        var lening = grootboek.VoegRekeningToe("2100", "Banklening", RekeningSoort.Passiva);
        var eigenVermogen = grootboek.VoegRekeningToe("3000", "Eigen vermogen", RekeningSoort.EigenVermogen);
        var kosten = grootboek.VoegRekeningToe("4000", "Bedrijfskosten", RekeningSoort.Kosten);
        var omzet = grootboek.VoegRekeningToe("8000", "Omzet", RekeningSoort.Opbrengsten);

        grootboek.Boek(Journaalpost.Nieuw(Datum, "Startkapitaal").Debet(kas.Id, 5_000m).Credit(eigenVermogen.Id, 5_000m));
        grootboek.Boek(Journaalpost.Nieuw(Datum, "Banklening").Debet(bank.Id, 3_000m).Credit(lening.Id, 3_000m));
        grootboek.Boek(Journaalpost.Nieuw(Datum, "Contante verkoop").Debet(kas.Id, 2_000m).Credit(omzet.Id, 2_000m));
        grootboek.Boek(Journaalpost.Nieuw(Datum, "Bedrijfskosten").Debet(kosten.Id, 800m).Credit(bank.Id, 800m));
        grootboek.Boek(Journaalpost.Nieuw(Datum, "Inkoop op rekening").Debet(voorraad.Id, 600m).Credit(crediteuren.Id, 600m));

        var periode = Periode.VoorJaar(2026);
        var balans = new BalansOpsteller().StelOp(grootboek, periode.Eind);
        var winstVerlies = new WinstVerliesOpsteller().StelOp(grootboek, periode);
        var kasstroom = new KasstroomOpsteller().StelOp(grootboek, periode);
        var ratio = new RatioBerekening().Bereken(balans, winstVerlies);

        return new FinancieelOverzicht(balans, winstVerlies, kasstroom, ratio, periode);
    }

    internal static FinancieelOverzichtModel MaakModel(string titel = "Financieel overzicht") =>
        new(Maak())
        {
            Metadata = new ReportMetadata
            {
                Titel = titel,
                Omschrijving = "Testrapport",
                Periode = "2026-01-01 t/m 2026-12-31",
                Administratie = "Testonderneming BV",
                GegenereerdOp = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            },
        };
}
