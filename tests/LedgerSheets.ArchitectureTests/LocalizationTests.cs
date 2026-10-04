using System.Globalization;
using LedgerSheets.Web.Localization;
using LedgerSheets.Web.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace LedgerSheets.ArchitectureTests;

/// <summary>
/// Verifies that every supported language ships a complete set of translations and that the report
/// labels resolve through the localizer.
/// </summary>
public sealed class LocalizationTests
{
    private static readonly string[] SupportedLanguages = ["en", "nl", "de", "fr", "es", "it", "pt", "pl"];

    private static readonly string[] Keys =
    [
        // Statement and report labels.
        "BalanceSheet", "ProfitAndLoss", "CashFlowStatement", "Ratio",
        "Assets", "Liabilities", "Equity", "Result",
        "Revenue", "Costs", "Total",
        "Operating", "Investing", "Financing",
        "OpeningBalance", "ClosingBalance", "NetMovement",
        "CurrentRatio", "QuickRatio", "Solvency",
        "NetProfitMargin", "ReturnOnEquity",
        "Account", "Description", "Amount",
        "NoData", "GeneratedOn", "Period",
        "Balanced", "NotBalanced",
        "OverviewTitle", "OverviewDescription",
        // User interface.
        "AppTitle", "MenuDashboard", "MenuAccounts", "MenuJournal", "MenuReport",
        "Language", "DownloadCsv", "Print",
        "Number", "Name", "Type", "Date",
        "Post", "Add", "Debit", "Credit",
        "Reference", "Year", "Current", "CashEquivalent", "Inventory", "New",
        // Bank import.
        "MenuBankImport", "StatementImport", "OpenMovements", "BankAccount", "File",
        "Format", "Automatic", "Camt053", "Csv", "Import",
        "ImportResult", "AllAlreadyKnown", "CounterAccount", "Proposal", "Confidence",
        "NoProposal", "Ignore",
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
    public void EveryLanguage_ContainsAllKeys(string culture)
    {
        WithCulture(culture, localizer =>
        {
            foreach (var key in Keys)
            {
                var value = localizer[key];
                Assert.False(
                    value.ResourceNotFound,
                    $"key '{key}' is missing for culture '{culture}'");
                Assert.False(
                    string.IsNullOrWhiteSpace(value.Value),
                    $"key '{key}' has no value for culture '{culture}'");
            }
        });
    }

    [Fact]
    public void AllSupportedLanguages_AreTested()
    {
        Assert.Equal(SupportedLanguages, Languages.Supported);
    }

    [Fact]
    public void Dutch_DiffersFromEnglish()
    {
        // 'nl' resolves through SharedResource.nl.resx (the Dutch text); 'and' resolves through the
        // neutral file, which now holds the English text (mapping.md 13).
        WithCulture("nl", l => Assert.Equal("Balans", l["BalanceSheet"].Value));
        WithCulture("en", l => Assert.Equal("Balance sheet", l["BalanceSheet"].Value));
    }

    [Fact]
    public void GermanAndFrench_AreTranslated()
    {
        WithCulture("de", l => Assert.Equal("Kapitalflussrechnung", l["CashFlowStatement"].Value));
        WithCulture("fr", l => Assert.Equal("Tableau des flux de trésorerie", l["CashFlowStatement"].Value));
    }

    [Fact]
    public void LocalizedReportLabels_TranslatesTheReportLabels()
    {
        WithCulture("en", localizer =>
        {
            var labels = new LocalizedReportLabels(localizer);

            Assert.Equal("Balance sheet", labels.BalanceSheet);
            Assert.Equal("Cash flow statement", labels.CashFlowStatement);
            Assert.Equal("Operating activities", labels.Operating);
            Assert.Equal("Balanced", labels.Balanced);
            Assert.Equal("Account", labels["Account"]);
        });
    }

    [Fact]
    public void UnknownCulture_FallsBackOnEnglish()
    {
        WithCulture("sv", localizer =>
        {
            // Swedish is not supported, so the neutral (English) resource file applies.
            Assert.Equal("Balance sheet", localizer["BalanceSheet"].Value);
        });
    }

    private static void WithCulture(string culture, Action<IStringLocalizer<SharedResource>> check)
    {
        var original = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddLocalization();

            using var provider = services.BuildServiceProvider();
            check(provider.GetRequiredService<IStringLocalizer<SharedResource>>());
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
