using FinOverzicht.Reports.Abstractions;

namespace FinOverzicht.Reports.Tests;

public sealed class HtmlReportRendererTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("BalansReport")]
    [InlineData("WinstVerliesReport")]
    [InlineData("KasstroomReport")]
    [InlineData("FinancieelOverzichtReport")]
    public async Task RenderHtmlAsync_ProducesSelfContainedHtmlDocument(string template)
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync(template, TestOverzicht.MaakModel("Testrapport"), Ct);

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<title>Testrapport — FinOverzicht</title>", html);
        Assert.Contains("<style>", html);
        Assert.Contains("Testonderneming BV", html);
        Assert.Contains($"© {DateTime.Now.Year} FinOverzicht", html);
        // Self-contained: no external stylesheet or script references (works offline / when printed).
        Assert.DoesNotContain("rel=\"stylesheet\"", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_BalansReport_ContainsAccountsAndTotals()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("BalansReport", TestOverzicht.MaakModel(), Ct);

        Assert.Contains("Balans", html);
        Assert.Contains("Activa", html);
        Assert.Contains("Passiva", html);
        Assert.Contains("Eigen vermogen", html);
        Assert.Contains("Kas", html);
        Assert.Contains("Crediteuren", html);
        Assert.Contains("Resultaat", html);
        Assert.Contains("In balans", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_WinstVerliesReport_ContainsRevenueAndExpenses()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("WinstVerliesReport", TestOverzicht.MaakModel(), Ct);

        Assert.Contains("Winst- en verliesrekening", html);
        Assert.Contains("Opbrengsten", html);
        Assert.Contains("Kosten", html);
        Assert.Contains("Omzet", html);
        Assert.Contains("Bedrijfskosten", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_KasstroomReport_ContainsThreeSections()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("KasstroomReport", TestOverzicht.MaakModel(), Ct);

        Assert.Contains("Kasstroomoverzicht", html);
        Assert.Contains("Operationele activiteiten", html);
        Assert.Contains("Investeringsactiviteiten", html);
        Assert.Contains("Financieringsactiviteiten", html);
        Assert.Contains("Beginsaldo", html);
        Assert.Contains("Eindsaldo", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_OverzichtReport_ContainsRatios()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("FinancieelOverzichtReport", TestOverzicht.MaakModel(), Ct);

        Assert.Contains("Kengetallen", html);
        Assert.Contains("Current ratio", html);
        Assert.Contains("Quick ratio", html);
        Assert.Contains("Solvabiliteit", html);
        Assert.Contains("Rentabiliteit eigen vermogen", html);
    }

    [Fact]
    public async Task RenderHtmlAsync_ThrowsForUnknownTemplate()
    {
        await using var context = new ReportTestContext();

        var exception = await Assert.ThrowsAsync<ReportRenderingException>(
            () => context.Renderer.RenderHtmlAsync("BestaatNietReport", TestOverzicht.MaakModel(), Ct));

        Assert.Equal("/Templates/BestaatNietReport", exception.RapportNaam);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenderHtmlAsync_ThrowsForBlankTemplateName(string template)
    {
        await using var context = new ReportTestContext();

        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Renderer.RenderHtmlAsync(template, TestOverzicht.MaakModel(), Ct));
    }

    [Fact]
    public async Task RenderHtmlAsync_ThrowsForNullModel()
    {
        await using var context = new ReportTestContext();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.Renderer.RenderHtmlAsync<Reports.Models.FinancieelOverzichtModel>("BalansReport", null!, Ct));
    }

    [Fact]
    public async Task RenderHtmlAsync_UsesRegisteredLabels()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("BalansReport", TestOverzicht.MaakModel(), Ct);

        Assert.Contains(context.Labels.Balans, html);
        Assert.Contains(context.Labels.Activa, html);
    }
}
