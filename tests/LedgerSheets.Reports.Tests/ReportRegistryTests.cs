using LedgerSheets.Reports.Abstractions;
using LedgerSheets.Reports.Models;
using LedgerSheets.Reports.Services;

namespace LedgerSheets.Reports.Tests;

public sealed class ReportRegistryTests
{
    private static readonly string[] DefaultReportKeys =
    [
        ReportKeys.Overview,
        ReportKeys.BalanceSheet,
        ReportKeys.ProfitAndLoss,
        ReportKeys.CashFlow,
    ];

    private readonly ReportRegistry _registry = new();

    [Fact]
    public void Register_AddsReport()
    {
        _registry.Register("Test", "BalanceSheet", "Assets", "BalanceSheetReport");

        var report = _registry.Find("Test");

        Assert.NotNull(report);
        Assert.Equal("BalanceSheetReport", report.TemplateName);
        Assert.Contains(ReportFormat.Html, report.Formaten);
    }

    [Fact]
    public void Register_IsCaseInsensitiveForDuplicates()
    {
        _registry.Register("Test", "BalanceSheet", "Assets", "BalanceSheetReport");

        Assert.Throws<InvalidOperationException>(
            () => _registry.Register("test", "BalanceSheet", "Assets", "BalanceSheetReport"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_ThrowsForBlankKey(string key)
    {
        Assert.Throws<ArgumentException>(
            () => _registry.Register(key, "BalanceSheet", "Assets", "BalanceSheetReport"));
    }

    [Fact]
    public void Find_ReturnsNullForUnknownKey()
    {
        Assert.Null(_registry.Find("Unknown"));
    }

    [Fact]
    public void WithDefaultReports_RegistersFourReports()
    {
        var registry = ReportRegistry.WithDefaultReports();

        Assert.Equal(4, registry.All().Count);
        Assert.Equal(DefaultReportKeys, registry.All().Select(r => r.Key));
    }

    [Fact]
    public void WithDefaultReports_EveryTemplateNameIsRegistered()
    {
        var registry = ReportRegistry.WithDefaultReports();

        Assert.All(registry.All(), r => Assert.False(string.IsNullOrWhiteSpace(r.TemplateName)));
    }
}
