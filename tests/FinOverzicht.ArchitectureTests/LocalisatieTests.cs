using System.Globalization;
using FinOverzicht.Web.Localization;
using FinOverzicht.Web.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace FinOverzicht.ArchitectureTests;

/// <summary>
/// Verifies that every supported language ships a complete set of translations and that the report
/// labels resolve through the localizer.
/// </summary>
public sealed class LocalisatieTests
{
    private static readonly string[] OndersteundeTalen = ["nl", "en", "de", "fr", "es", "it", "pt", "pl"];

    private static readonly string[] Sleutels =
    [
        // Statement and report labels.
        "Balans", "WinstEnVerlies", "Kasstroomoverzicht", "Ratio",
        "Activa", "Passiva", "EigenVermogen", "Resultaat",
        "Opbrengsten", "Kosten", "Totaal",
        "Operationeel", "Investering", "Financiering",
        "BeginSaldo", "EindSaldo", "NettoMutatie",
        "CurrentRatio", "QuickRatio", "Solvabiliteit",
        "NettoWinstmarge", "RentabiliteitEigenVermogen",
        "Rekening", "Omschrijving", "Bedrag",
        "GeenGegevens", "GegenereerdOp", "Periode",
        "InBalans", "NietInBalans",
        "OverzichtTitel", "OverzichtOmschrijving",
        // User interface.
        "AppTitel", "MenuDashboard", "MenuRekeningen", "MenuBoekingen", "MenuRapport",
        "Taal", "DownloadCsv", "Afdrukken",
        "Nummer", "Naam", "Soort", "Datum",
        "Boeken", "Toevoegen", "Debet", "Credit",
        "Referentie", "Jaar", "Vlottend", "LiquideMiddel", "Voorraad", "Nieuw",
        // Bank import.
        "MenuBankimport", "AfschriftImporteren", "OpenstaandeMutaties", "Bankrekening", "Bestand",
        "Formaat", "Automatisch", "Camt053", "Csv", "Importeren",
        "ImportResultaat", "AllesAlBekend", "Tegenrekening", "Voorstel", "Zekerheid",
        "GeenVoorstel", "Negeren",
    ];

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    [InlineData("pt")]
    [InlineData("pl")]
    public void IedereTaal_BevatAlleSleutels(string cultuur)
    {
        MetCultuur(cultuur, localizer =>
        {
            foreach (var sleutel in Sleutels)
            {
                var waarde = localizer[sleutel];
                Assert.False(
                    waarde.ResourceNotFound,
                    $"sleutel '{sleutel}' ontbreekt voor cultuur '{cultuur}'");
                Assert.False(
                    string.IsNullOrWhiteSpace(waarde.Value),
                    $"sleutel '{sleutel}' heeft geen waarde voor cultuur '{cultuur}'");
            }
        });
    }

    [Fact]
    public void AlleOndersteundeTalen_ZijnGetest()
    {
        Assert.Equal(OndersteundeTalen, Talen.Ondersteund);
    }

    [Fact]
    public void Nederlands_WijktAfVanEngels()
    {
        MetCultuur("nl", l => Assert.Equal("Balans", l["Balans"].Value));
        MetCultuur("en", l => Assert.Equal("Balance sheet", l["Balans"].Value));
    }

    [Fact]
    public void DuitsEnFrans_ZijnVertaald()
    {
        MetCultuur("de", l => Assert.Equal("Kapitalflussrechnung", l["Kasstroomoverzicht"].Value));
        MetCultuur("fr", l => Assert.Equal("Tableau des flux de trésorerie", l["Kasstroomoverzicht"].Value));
    }

    [Fact]
    public void LocalizedReportLabels_VertaaltDeRapportLabels()
    {
        MetCultuur("en", localizer =>
        {
            var labels = new LocalizedReportLabels(localizer);

            Assert.Equal("Balance sheet", labels.Balans);
            Assert.Equal("Cash flow statement", labels.Kasstroomoverzicht);
            Assert.Equal("Operating activities", labels.Operationeel);
            Assert.Equal("Balanced", labels.InBalans);
            Assert.Equal("Account", labels["Rekening"]);
        });
    }

    [Fact]
    public void OnbekendeCultuur_ValtTerugOpNederlands()
    {
        MetCultuur("sv", localizer =>
        {
            // Swedish is not supported, so the neutral (Dutch) resource file applies.
            Assert.Equal("Balans", localizer["Balans"].Value);
        });
    }

    private static void MetCultuur(string cultuur, Action<IStringLocalizer<SharedResource>> controle)
    {
        var origineel = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultuur);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddLocalization();

            using var provider = services.BuildServiceProvider();
            controle(provider.GetRequiredService<IStringLocalizer<SharedResource>>());
        }
        finally
        {
            CultureInfo.CurrentUICulture = origineel;
        }
    }
}
