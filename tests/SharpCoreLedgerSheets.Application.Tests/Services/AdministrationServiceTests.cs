using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Application.Tests.Fakes;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Tests.Services;

public sealed class AdministrationServiceTests
{
    private readonly InMemoryAccountRepository _accounts = new();
    private readonly InMemoryJournalEntryRepository _entries = new();
    private readonly InMemorySettingsRepository _settings = new();
    private readonly AdministrationService _service;

    public AdministrationServiceTests()
    {
        var journal = new JournalEntryService(_accounts, _entries);
        _service = new AdministrationService(_settings, _accounts, _entries, journal);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetSettingsAsync_ReturnsTheDefaults_WhenNothingIsStored()
    {
        var settings = await _service.GetSettingsAsync(Ct);

        Assert.Equal(AdministrationSettings.DefaultCompanyName, settings.CompanyName);
        Assert.True(settings.UsesCalendarYear);
    }

    [Fact]
    public async Task SaveSettingsAsync_StoresTheSettings()
    {
        await _service.SaveSettingsAsync(new AdministrationSettings("Bram Retail", fiscalYearStartMonth: 4), Ct);

        Assert.Equal("Bram Retail", _settings.Current.CompanyName);
        Assert.Equal(4, _settings.Current.FiscalYearStartMonth);
    }

    [Fact]
    public async Task ReplaceOpeningBalancesAsync_PostsOneEntryDatedTheDayBeforeTheBookYear()
    {
        await SeedChartAsync();

        await _service.ReplaceOpeningBalancesAsync(2026,
        [
            new OpeningBalanceLine("1000", 2500m, 0m),
            new OpeningBalanceLine("4000", 0m, 2500m),
        ], Ct);

        var entry = Assert.Single(await _entries.AllAsync(ct: Ct));
        Assert.Equal(new DateOnly(2025, 12, 31), entry.Date);
        Assert.Equal(AdministrationService.OpeningReferenceFor(2026), entry.Reference);
        Assert.Equal(2, entry.Lines.Count);
    }

    [Fact]
    public async Task ReplaceOpeningBalancesAsync_HonoursABrokenBookYear()
    {
        await SeedChartAsync();
        _settings.Set(new AdministrationSettings("Sanne", fiscalYearStartMonth: 4));

        await _service.ReplaceOpeningBalancesAsync(2026,
        [
            new OpeningBalanceLine("1000", 100m, 0m),
            new OpeningBalanceLine("4000", 0m, 100m),
        ], Ct);

        var entry = Assert.Single(await _entries.AllAsync(ct: Ct));
        Assert.Equal(new DateOnly(2026, 3, 31), entry.Date);
    }

    [Fact]
    public async Task OpeningBalancesAsync_ReturnsWhatWasEntered()
    {
        await SeedChartAsync();
        await _service.ReplaceOpeningBalancesAsync(2026,
        [
            new OpeningBalanceLine("1000", 2500m, 0m),
            new OpeningBalanceLine("4000", 0m, 2500m),
        ], Ct);

        var lines = await _service.OpeningBalancesAsync(2026, Ct);

        Assert.Equal(2, lines.Count);
        Assert.Contains(lines, line => line.AccountNumber == "1000" && line.Debit == 2500m && line.Credit == 0m);
        Assert.Contains(lines, line => line.AccountNumber == "4000" && line.Debit == 0m && line.Credit == 2500m);
    }

    [Fact]
    public async Task OpeningBalancesAsync_ReturnsNothing_WhenNoneWereEntered()
    {
        await SeedChartAsync();

        Assert.Empty(await _service.OpeningBalancesAsync(2026, Ct));
    }

    [Fact]
    public async Task ReplaceOpeningBalancesAsync_ReplacesInsteadOfDuplicating()
    {
        await SeedChartAsync();
        await _service.ReplaceOpeningBalancesAsync(2026,
        [
            new OpeningBalanceLine("1000", 100m, 0m),
            new OpeningBalanceLine("4000", 0m, 100m),
        ], Ct);

        await _service.ReplaceOpeningBalancesAsync(2026,
        [
            new OpeningBalanceLine("1000", 300m, 0m),
            new OpeningBalanceLine("4000", 0m, 300m),
        ], Ct);

        var entry = Assert.Single(await _entries.AllAsync(ct: Ct));
        Assert.Equal(300m, entry.TotalDebit);
    }

    [Fact]
    public async Task ReplaceOpeningBalancesAsync_WithNoLines_ClearsThem()
    {
        await SeedChartAsync();
        await _service.ReplaceOpeningBalancesAsync(2026,
        [
            new OpeningBalanceLine("1000", 100m, 0m),
            new OpeningBalanceLine("4000", 0m, 100m),
        ], Ct);

        await _service.ReplaceOpeningBalancesAsync(2026, [], Ct);

        Assert.Empty(await _entries.AllAsync(ct: Ct));
    }

    [Fact]
    public async Task ReplaceOpeningBalancesAsync_RejectsUnbalancedLines()
    {
        await SeedChartAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.ReplaceOpeningBalancesAsync(2026,
        [
            new OpeningBalanceLine("1000", 100m, 0m),
            new OpeningBalanceLine("4000", 0m, 90m),
        ], Ct));

        Assert.Equal(MessageCode.OpeningBalanceNotBalanced, exception.Code);
        Assert.Empty(await _entries.AllAsync(ct: Ct));
    }

    [Fact]
    public async Task ReplaceOpeningBalancesAsync_RejectsANegativeAmount()
    {
        await SeedChartAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.ReplaceOpeningBalancesAsync(2026,
        [
            new OpeningBalanceLine("1000", -100m, 0m),
            new OpeningBalanceLine("4000", 0m, -100m),
        ], Ct));

        Assert.Equal(MessageCode.AmountMustBePositive, exception.Code);
    }

    [Fact]
    public async Task ReplaceOpeningBalancesAsync_RejectsALineWithBothDebitAndCredit()
    {
        await SeedChartAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.ReplaceOpeningBalancesAsync(2026,
        [
            new OpeningBalanceLine("1000", 100m, 100m),
            new OpeningBalanceLine("4000", 0m, 0m),
        ], Ct));

        Assert.Equal(MessageCode.LineDebitAndCredit, exception.Code);
    }

    private async Task SeedChartAsync()
    {
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets, isCurrent: true, isCashEquivalent: true), Ct);
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "4000", "Eigen vermogen", AccountType.Equity), Ct);
    }
}
