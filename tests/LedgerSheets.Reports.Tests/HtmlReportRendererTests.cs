using LedgerSheets.Reports.Abstractions;

namespace LedgerSheets.Reports.Tests;

public sealed class HtmlReportRendererTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("BalanceSheetReport")]
    [InlineData("ProfitAndLossReport")]
    [InlineData("CashFlowReport")]
    [InlineData("FinancialOverviewReport")]
    public async Task RenderHtmlAsync_ProducesSelfContainedHtmlDocument(string template)
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync(template, TestOverview.CreateModel("Test report"), Ct);

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<title>Test report — LedgerSheets</title>", html);
        Assert.Contains("<style>", html);
        Assert.Contains("Testonderneming BV", html);
        Assert.Contains($"© {DateTime.Now.Year} LedgerSheets", html);
        // Self-contained: no external stylesheet or script references (works offline / when printed).
        Assert.DoesNotContain("rel=\"stylesheet\"", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_BalanceSheetReport_ContainsAccountsAndTotals()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("BalanceSheetReport", TestOverview.CreateModel(), Ct);

        Assert.Contains("Balance sheet", html);
        Assert.Contains("Assets", html);
        Assert.Contains("Liabilities", html);
        Assert.Contains("Equity", html);
        Assert.Contains("Cash", html);
        Assert.Contains("Creditors", html);
        Assert.Contains("Result", html);
        Assert.Contains("Balanced", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_ProfitAndLossReport_ContainsRevenueAndExpenses()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("ProfitAndLossReport", TestOverview.CreateModel(), Ct);

        Assert.Contains("Profit and loss statement", html);
        Assert.Contains("Revenue", html);
        Assert.Contains("Expenses", html);
        Assert.Contains("Operating expenses", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_CashFlowReport_ContainsThreeSections()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("CashFlowReport", TestOverview.CreateModel(), Ct);

        Assert.Contains("Cash flow statement", html);
        Assert.Contains("Operating activities", html);
        Assert.Contains("Investing activities", html);
        Assert.Contains("Financing activities", html);
        Assert.Contains("Opening balance", html);
        Assert.Contains("Closing balance", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_OverviewReport_ContainsRatios()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("FinancialOverviewReport", TestOverview.CreateModel(), Ct);

        Assert.Contains("Key ratios", html);
        Assert.Contains("Current ratio", html);
        Assert.Contains("Quick ratio", html);
        Assert.Contains("Solvency", html);
        Assert.Contains("Return on equity", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_ThrowsForUnknownTemplate()
    {
        await using var context = new ReportTestContext();

        var exception = await Assert.ThrowsAsync<ReportRenderingException>(
            () => context.Renderer.RenderHtmlAsync("ExistsNotReport", TestOverview.CreateModel(), Ct));

        Assert.Equal("/Templates/ExistsNotReport", exception.ReportName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenderHtmlAsync_ThrowsForBlankTemplateName(string template)
    {
        await using var context = new ReportTestContext();

        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Renderer.RenderHtmlAsync(template, TestOverview.CreateModel(), Ct));
    }

    [Fact]
    public async Task RenderHtmlAsync_ThrowsForNullModel()
    {
        await using var context = new ReportTestContext();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.Renderer.RenderHtmlAsync<Reports.Models.FinancialOverviewModel>("BalanceSheetReport", null!, Ct));
    }

    [Fact]
    public async Task RenderHtmlAsync_UsesRegisteredLabels()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("BalanceSheetReport", TestOverview.CreateModel(), Ct);

        Assert.Contains(context.Labels.BalanceSheet, html);
        Assert.Contains(context.Labels.Assets, html);
    }
}
