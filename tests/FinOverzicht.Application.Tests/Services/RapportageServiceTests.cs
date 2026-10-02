using FinOverzicht.Domain.Boekhouden;

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

        Assert.True(overzicht.IsConsistent);
        Assert.True(overzicht.Balans.IsInBalans);
        Assert.True(overzicht.Kasstroom.IsInBalans);
    }

    [Fact]
    public async Task StelOverzichtOpAsync_MatchesExpectedTotals()
    {
        var administratie = new TestAdministratie();
        await administratie.SeedAsync(Ct);

        var overzicht = await administratie.Rapportage.StelOverzichtOpAsync(TestAdministratie.Jaar2026, Ct);

        Assert.Equal(18_300m, overzicht.Balans.TotaalActiva);
        Assert.Equal(6_000m, overzicht.Balans.TotaalPassiva);
        Assert.Equal(2_300m, overzicht.Balans.Resultaat);
        Assert.Equal(2_300m, overzicht.WinstVerlies.Resultaat);
        Assert.Equal(14_500m, overzicht.Kasstroom.NettoMutatie);
    }

    [Fact]
    public async Task StelOverzichtOpAsync_ComputesRatios()
    {
        var administratie = new TestAdministratie();
        await administratie.SeedAsync(Ct);

        var overzicht = await administratie.Rapportage.StelOverzichtOpAsync(TestAdministratie.Jaar2026, Ct);

        Assert.Equal(7.65m, overzicht.Ratio.CurrentRatio);
        Assert.Equal(7.25m, overzicht.Ratio.QuickRatio);
        Assert.Equal(0.6721m, overzicht.Ratio.Solvabiliteit);
    }

    [Fact]
    public async Task StelOverzichtOpAsync_TakesBalanceSheetAtPeriodEnd()
    {
        var administratie = new TestAdministratie();
        await administratie.SeedAsync(Ct);

        var eersteHalfjaar = new Periode(new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 31));
        var overzicht = await administratie.Rapportage.StelOverzichtOpAsync(eersteHalfjaar, Ct);

        // All scenario entries are dated 2026-06-15, so the balance sheet is still empty.
        Assert.Equal(0m, overzicht.Balans.TotaalActiva);
        Assert.Equal(0m, overzicht.WinstVerlies.Resultaat);
    }

    [Fact]
    public async Task StelOverzichtOpAsync_WorksOnEmptyAdministration()
    {
        var administratie = new TestAdministratie();

        var overzicht = await administratie.Rapportage.StelOverzichtOpAsync(TestAdministratie.Jaar2026, Ct);

        Assert.Equal(0m, overzicht.Balans.TotaalActiva);
        Assert.Null(overzicht.Ratio.CurrentRatio);
        Assert.True(overzicht.IsConsistent);
    }
}
