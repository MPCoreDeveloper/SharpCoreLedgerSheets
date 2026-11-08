using SharpCoreLedgerSheets.Application.Tests.Fakes;
using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Application.Tests.Services;

public sealed class LedgerLoaderTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LoadAsync_RehydratesAccountsAndEntriesWithOriginalIds()
    {
        var administration = new TestAdministration();
        await administration.SeedAsync(Ct);

        var ledger = await administration.Loader.LoadAsync(Ct);

        Assert.Equal(9, ledger.Accounts.Count);
        Assert.Equal(8, ledger.Entries.Count);
        Assert.Equal(0m, ledger.CheckTotal());
        Assert.All(ledger.Accounts, r => Assert.NotEqual(Guid.Empty, r.Id));
    }

    [Fact]
    public async Task LoadAsync_PreservesBalancesAcrossRehydration()
    {
        var administration = new TestAdministration();
        await administration.SeedAsync(Ct);

        var ledger = await administration.Loader.LoadAsync(Ct);

        var cash = ledger.FindAccount("1000");
        Assert.NotNull(cash);
        Assert.Equal(14_000m, ledger.Balance(cash!.Id));
    }

    [Fact]
    public async Task LoadAsync_AllowsInactiveAccountsInHistoricalEntries()
    {
        var accounts = new InMemoryAccountRepository();
        var entries = new InMemoryJournalEntryRepository();
        var accountService = new Application.Services.AccountService(accounts);
        var postService = new Application.Services.JournalEntryService(accounts, entries);
        var loader = new Application.Services.LedgerLoader(accounts, entries);

        await accountService.AddAsync(new Application.Models.AddAccountCommand("1000", "Kas", AccountType.Assets, Current: true, CashEquivalent: true), Ct);
        await accountService.AddAsync(new Application.Models.AddAccountCommand("8000", "Omzet", AccountType.Revenue), Ct);
        await postService.PostAsync(new Application.Models.PostJournalEntryCommand(
            new DateOnly(2026, 1, 10), "Verkoop",
            [new("1000", 100m, 0m), new("8000", 0m, 100m)]), Ct);

        await accountService.DeactivateAsync("8000", Ct);

        var ledger = await loader.LoadAsync(Ct);

        Assert.Single(ledger.Entries);
    }
}
