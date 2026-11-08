using System.Globalization;
using System.Reflection;
using SharpCoreLedgerSheets.Application.Exceptions;
using SharpCoreLedgerSheets.Domain.Common;
using SharpCoreLedgerSheets.Web.Localization;
using SharpCoreLedgerSheets.Web.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace SharpCoreLedgerSheets.ArchitectureTests;

/// <summary>
/// Verifies that every supported language ships a complete set of translations and that the report
/// labels resolve through the localizer.
/// </summary>
public sealed class LocalizationTests
{
    private static readonly string[] SupportedLanguages = ["nl", "en", "de", "fr", "es", "it", "pt", "pl"];

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
        // Starter chart of accounts.
        "StarterChart", "StarterChartHint", "StarterChartLoad", "StarterChartCreated",
        "StarterChartDutch", "StarterChartInternational",
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
    public void Dutch_IsTheDefaultCulture()
    {
        // The product targets the Dutch MKB first, so Dutch is the default culture and the first
        // entry in the language picker. The neutral (English) resource file stays the fallback for a
        // key a translation is missing - see UnknownCulture_FallsBackOnEnglish.
        Assert.Equal("nl", Languages.Default);
        Assert.Equal("nl", Languages.Supported[0]);
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

    [Fact]
    public void EveryMessageCode_HasATranslationForDutchAndEnglish()
    {
        var codes = typeof(MessageCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false })
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(codes);

        foreach (var culture in new[] { "nl", "en" })
        {
            WithCulture(culture, localizer =>
            {
                foreach (var code in codes)
                {
                    var value = localizer[ExceptionMessages.KeyPrefix + code];

                    Assert.False(
                        value.ResourceNotFound,
                        $"message code '{code}' is missing for culture '{culture}'");
                }
            });
        }
    }

    [Fact]
    public void ExceptionMessages_TranslatesADomainRule()
    {
        var failure = new NotFoundException(
            MessageCode.AccountNotFound, "Rekening '{0}' is niet gevonden.", "1000");

        WithCulture("nl", localizer => Assert.Equal(
            "Rekening '1000' is niet gevonden.", ExceptionMessages.Describe(failure, localizer)));

        WithCulture("en", localizer => Assert.Equal(
            "Account '1000' was not found.", ExceptionMessages.Describe(failure, localizer)));
    }

    [Fact]
    public void ExceptionMessages_FallsBackWhenThereIsNothingToTranslate()
    {
        // A developer-facing failure without a code is shown as it is.
        WithCulture("en", localizer => Assert.Equal(
            "boom", ExceptionMessages.Describe(new InvalidOperationException("boom"), localizer)));

        // An unknown code falls back to the message the engine carries (Dutch, the primary market).
        WithCulture("en", localizer => Assert.Equal(
            "Onvertaald",
            ExceptionMessages.Describe(new DomainException("NoSuchCode", "Onvertaald"), localizer)));
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
