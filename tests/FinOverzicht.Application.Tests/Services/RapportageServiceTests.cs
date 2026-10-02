using FinOverzicht.Domain.Boekhouden;
using FluentAssertions;

namespace FinOverzicht.Application.Tests.Services;

public sealed class RapportageServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task StelOverzichtOpAsync_ProducesConsistentStatements()
    {
        var administratie = new TestAdministratie();
        await administratie.SeedAsync(Ct);

        var overzicht = await administratie.Rapportage.StelOverzichtOpAsync(TestAdministratie.Jaar2026, Ct);

        overzicht.IsConsistent.Should().BeTrue();
        overzicht.Balans.IsInBalans.Should().BeTrue();
        overzicht.Kasstroom.IsInBalans.Should().BeTrue();
    }

    [Fact]
    public async Task StelOverzichtOpAsync_MatchesExpectedTotals()
    {
        var administratie = new TestAdministratie();
        await administratie.SeedAsync(Ct);

        var overzicht = await administratie.Rapportage.StelOverzichtOpAsync(TestAdministratie.Jaar2026, Ct);

        overzicht.Balans.TotaalActiva.Should().Be(18_300m);
        overzicht.Balans.TotaalPassiva.Should().Be(6_000m);
        overzicht.Balans.Resultaat.Should().Be(2_300m);
        overzicht.WinstVerlies.Resultaat.Should().Be(2_300m);
        overzicht.Kasstroom.NettoMutatie.Should().Be(14_500m);
    }

    [Fact]
    public async Task StelOverzichtOpAsync_ComputesRatios()
    {
        var administratie = new TestAdministratie();
        await administratie.SeedAsync(Ct);

        var overzicht = await administratie.Rapportage.StelOverzichtOpAsync(TestAdministratie.Jaar2026, Ct);

        overzicht.Ratio.CurrentRatio.Should().Be(7.65m);
        overzicht.Ratio.QuickRatio.Should().Be(7.25m);
        overzicht.Ratio.Solvabiliteit.Should().Be(0.6721m);
    }

    [Fact]
    public async Task StelOverzichtOpAsync_TakesBalanceSheetAtPeriodEnd()
    {
        var administratie = new TestAdministratie();
        await administratie.SeedAsync(Ct);

        var eersteHalfjaar = new Periode(new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 31));
        var overzicht = await administratie.Rapportage.StelOverzichtOpAsync(eersteHalfjaar, Ct);

        // All scenario entries are dated 2026-06-15, so the balance sheet is still empty.
        overzicht.Balans.TotaalActiva.Should().Be(0m);
        overzicht.WinstVerlies.Resultaat.Should().Be(0m);
    }

    [Fact]
    public async Task StelOverzichtOpAsync_WorksOnEmptyAdministration()
    {
        var administratie = new TestAdministratie();

        var overzicht = await administratie.Rapportage.StelOverzichtOpAsync(TestAdministratie.Jaar2026, Ct);

        overzicht.Balans.TotaalActiva.Should().Be(0m);
        overzicht.Ratio.CurrentRatio.Should().BeNull();
        overzicht.IsConsistent.Should().BeTrue();
    }
}
