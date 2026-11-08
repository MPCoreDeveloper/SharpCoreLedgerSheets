using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Infrastructure.Tests;

public sealed class SharpCoreDbAccountRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AddAsync_And_AllAsync_RoundTripAllProperties()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.Accounts.AddAsync(
            new Account(Guid.NewGuid(), "1300", "Inventory", AccountType.Assets,
                isActive: true, isCurrent: true, isCashEquivalent: false, isInventory: true), Ct);

        var accounts = await context.Accounts.AllAsync(Ct);

        var account = Assert.Single(accounts);
        Assert.Equal("1300", account.Number);
        Assert.Equal("Inventory", account.Name);
        Assert.Equal(AccountType.Assets, account.Type);
        Assert.True(account.IsActive);
        Assert.True(account.IsCurrent);
        Assert.False(account.IsCashEquivalent);
        Assert.True(account.IsInventory);
    }

    [Fact]
    public async Task AllAsync_OrdersByNumber()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.Accounts.AddAsync(new Account(Guid.NewGuid(), "8000", "Omzet", AccountType.Revenue), Ct);
        await context.Accounts.AddAsync(new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets), Ct);

        var accounts = await context.Accounts.AllAsync(Ct);

        Assert.Collection(
            accounts,
            r => Assert.Equal("1000", r.Number),
            r => Assert.Equal("8000", r.Number));
    }

    [Fact]
    public async Task FindOnNumberAsync_FindsAccount()
    {
        await using var context = new SharpCoreDbTestContext();
        var cash = new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets, isCashEquivalent: true);
        await context.Accounts.AddAsync(cash, Ct);

        var found = await context.Accounts.FindOnNumberAsync("1000", Ct);

        Assert.NotNull(found);
        Assert.Equal(cash.Id, found.Id);
        Assert.True(found.IsCashEquivalent);
    }

    [Fact]
    public async Task FindOnNumberAsync_ReturnsNullWhenMissing()
    {
        await using var context = new SharpCoreDbTestContext();

        var found = await context.Accounts.FindOnNumberAsync("9999", Ct);

        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateAsync_PersistsNameAndFlags()
    {
        await using var context = new SharpCoreDbTestContext();
        var account = new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets, isCashEquivalent: true);
        await context.Accounts.AddAsync(account, Ct);

        account.Rename("Kas en bank");
        account.Deactivate();
        await context.Accounts.UpdateAsync(account, Ct);

        var found = await context.Accounts.FindOnNumberAsync("1000", Ct);

        Assert.NotNull(found);
        Assert.Equal("Kas en bank", found.Name);
        Assert.False(found.IsActive);
    }

    [Fact]
    public async Task Data_SurvivesReopen()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.Accounts.AddAsync(
            new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets, isCashEquivalent: true), Ct);

        await context.HeropenAsync();

        var accounts = await context.Accounts.AllAsync(Ct);
        Assert.Single(accounts, r => r.Number == "1000" && r.IsCashEquivalent);
    }

    [Fact]
    public async Task FindOnNumberAsync_ThrowsForBlankNumber()
    {
        await using var context = new SharpCoreDbTestContext();

        await Assert.ThrowsAsync<ArgumentException>(() => context.Accounts.FindOnNumberAsync("  ", Ct));
    }
}
