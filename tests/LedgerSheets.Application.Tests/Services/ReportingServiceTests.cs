using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Application.Tests.Services;

public sealed class ReportingServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BuildOverviewAsync_ProducesConsistentStatements()
    {
        var administration = new TestAdministration();
        await administration.SeedAsync(Ct);

        var overview = await administration.Reporting.BuildOverviewAsync(TestAdministration.Year2026, Ct);

        Assert.True(overview.IsConsistent);
        Assert.True(overview.BalanceSheet.IsBalanced);
        Assert.True(overview.CashFlow.IsBalanced);
    }

    [Fact]
    public async Task BuildOverviewAsync_MatchesExpectedTotals()
    {
        var administration = new TestAdministration();
        await administration.SeedAsync(Ct);

        var overview = await administration.Reporting.BuildOverviewAsync(TestAdministration.Year2026, Ct);

        Assert.Equal(18_300m, overview.BalanceSheet.TotalAssets);
        Assert.Equal(6_000m, overview.BalanceSheet.TotalLiabilities);
        Assert.Equal(2_300m, overview.BalanceSheet.Result);
        Assert.Equal(2_300m, overview.ProfitAndLoss.Result);
        Assert.Equal(14_500m, overview.CashFlow.NetMovement);
    }

    [Fact]
    public async Task BuildOverviewAsync_ComputesRatios()
    {
        var administration = new TestAdministration();
        await administration.SeedAsync(Ct);

        var overview = await administration.Reporting.BuildOverviewAsync(TestAdministration.Year2026, Ct);

        Assert.Equal(7.65m, overview.Ratio.CurrentRatio);
        Assert.Equal(7.25m, overview.Ratio.QuickRatio);
        Assert.Equal(0.6721m, overview.Ratio.Solvency);
    }

    [Fact]
    public async Task BuildOverviewAsync_TakesBalanceSheetAtPeriodEnd()
    {
        var administration = new TestAdministration();
        await administration.SeedAsync(Ct);

        var firstHalfYear = new Period(new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 31));
        var overview = await administration.Reporting.BuildOverviewAsync(firstHalfYear, Ct);

        // All scenario entries are dated 2026-06-15, so the balance sheet is still empty.
        Assert.Equal(0m, overview.BalanceSheet.TotalAssets);
        Assert.Equal(0m, overview.ProfitAndLoss.Result);
    }

    [Fact]
    public async Task BuildOverviewAsync_WorksOnEmptyAdministration()
    {
        var administration = new TestAdministration();

        var overview = await administration.Reporting.BuildOverviewAsync(TestAdministration.Year2026, Ct);

        Assert.Equal(0m, overview.BalanceSheet.TotalAssets);
        Assert.Null(overview.Ratio.CurrentRatio);
        Assert.True(overview.IsConsistent);
    }
}
