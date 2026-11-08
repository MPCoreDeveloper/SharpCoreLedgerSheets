using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;

namespace SharpCoreLedgerSheets.Infrastructure.Tests;

public sealed class SharpCoreDbSettingsRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetAsync_ReturnsTheDefaults_ForAFreshDatabase()
    {
        await using var context = new SharpCoreDbTestContext();

        var settings = await context.Settings.GetAsync(Ct);

        Assert.Equal(AdministrationSettings.DefaultCompanyName, settings.CompanyName);
        Assert.Equal(AdministrationSettings.CalendarYearStart, settings.FiscalYearStartMonth);
        Assert.Equal(Currency.Eur, settings.Currency);
        Assert.Equal(["21", "9", "0"], settings.Vat.Rates.Select(rate => rate.Code));
    }

    [Fact]
    public async Task SaveAsync_RoundTripsTheSettingsAndSurvivesReopen()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.Settings.SaveAsync(
            new AdministrationSettings("Bram Retail", fiscalYearStartMonth: 4), Ct);

        await context.HeropenAsync();

        var settings = await context.Settings.GetAsync(Ct);

        Assert.Equal("Bram Retail", settings.CompanyName);
        Assert.Equal(4, settings.FiscalYearStartMonth);
        Assert.Equal(new DateOnly(2026, 4, 1), settings.PeriodFor(2026).Start);
        Assert.Equal("2100", settings.Vat.Require("21").OutputAccountNumber);
        Assert.Equal("1110", settings.Vat.Require("21").InputAccountNumber);
    }

    [Fact]
    public async Task SaveAsync_ReplacesThePreviousSettings()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.Settings.SaveAsync(new AdministrationSettings("Eerste", fiscalYearStartMonth: 2), Ct);
        await context.Settings.SaveAsync(new AdministrationSettings("Tweede", fiscalYearStartMonth: 7), Ct);

        var settings = await context.Settings.GetAsync(Ct);

        Assert.Equal("Tweede", settings.CompanyName);
        Assert.Equal(7, settings.FiscalYearStartMonth);
    }

    [Fact]
    public async Task SaveAsync_KeepsAnEmptyVatScheme()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.Settings.SaveAsync(AdministrationSettings.Default.WithVatScheme(VatScheme.None), Ct);

        var settings = await context.Settings.GetAsync(Ct);

        Assert.True(settings.Vat.IsEmpty);
    }
}
