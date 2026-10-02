using FinOverzicht.Reports.Abstractions;
using FluentAssertions;

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

        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<title>Testrapport — FinOverzicht</title>");
        html.Should().Contain("<style>");
        html.Should().Contain("Testonderneming BV");
        html.Should().Contain($"© {DateTime.Now.Year} FinOverzicht");
        // Self-contained: no external stylesheet or script references (works offline / when printed).
        html.Should().NotContain("rel=\"stylesheet\"");
        html.Should().NotContain("<script");
    }

    [Fact]
    public async Task RenderHtmlAsync_BalansReport_ContainsAccountsAndTotals()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("BalansReport", TestOverzicht.MaakModel(), Ct);

        html.Should().Contain("Balans");
        html.Should().Contain("Activa");
        html.Should().Contain("Passiva");
        html.Should().Contain("Eigen vermogen");
        html.Should().Contain("Kas");
        html.Should().Contain("Crediteuren");
        html.Should().Contain("Resultaat");
        html.Should().Contain("In balans");
    }

    [Fact]
    public async Task RenderHtmlAsync_WinstVerliesReport_ContainsRevenueAndExpenses()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("WinstVerliesReport", TestOverzicht.MaakModel(), Ct);

        html.Should().Contain("Winst- en verliesrekening");
        html.Should().Contain("Opbrengsten");
        html.Should().Contain("Kosten");
        html.Should().Contain("Omzet");
        html.Should().Contain("Bedrijfskosten");
    }

    [Fact]
    public async Task RenderHtmlAsync_KasstroomReport_ContainsThreeSections()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("KasstroomReport", TestOverzicht.MaakModel(), Ct);

        html.Should().Contain("Kasstroomoverzicht");
        html.Should().Contain("Operationele activiteiten");
        html.Should().Contain("Investeringsactiviteiten");
        html.Should().Contain("Financieringsactiviteiten");
        html.Should().Contain("Beginsaldo");
        html.Should().Contain("Eindsaldo");
    }

    [Fact]
    public async Task RenderHtmlAsync_OverzichtReport_ContainsRatios()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("FinancieelOverzichtReport", TestOverzicht.MaakModel(), Ct);

        html.Should().Contain("Kengetallen");
        html.Should().Contain("Current ratio");
        html.Should().Contain("Quick ratio");
        html.Should().Contain("Solvabiliteit");
        html.Should().Contain("Rentabiliteit eigen vermogen");
    }

    [Fact]
    public async Task RenderHtmlAsync_ThrowsForUnknownTemplate()
    {
        await using var context = new ReportTestContext();

        var act = () => context.Renderer.RenderHtmlAsync("BestaatNietReport", TestOverzicht.MaakModel(), Ct);

        var exception = await act.Should().ThrowAsync<ReportRenderingException>();
        exception.Which.RapportNaam.Should().Be("/Templates/BestaatNietReport");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RenderHtmlAsync_ThrowsForBlankTemplateName(string template)
    {
        await using var context = new ReportTestContext();

        var act = () => context.Renderer.RenderHtmlAsync(template, TestOverzicht.MaakModel(), Ct);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RenderHtmlAsync_ThrowsForNullModel()
    {
        await using var context = new ReportTestContext();

        var act = () => context.Renderer.RenderHtmlAsync<Reports.Models.FinancieelOverzichtModel>("BalansReport", null!, Ct);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RenderHtmlAsync_UsesRegisteredLabels()
    {
        await using var context = new ReportTestContext();

        var html = await context.Renderer.RenderHtmlAsync("BalansReport", TestOverzicht.MaakModel(), Ct);

        html.Should().Contain(context.Labels.Balans);
        html.Should().Contain(context.Labels.Activa);
    }
}
