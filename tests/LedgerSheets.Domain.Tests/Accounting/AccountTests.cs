using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Tests.Accounting;

public sealed class AccountTests
{
    [Fact]
    public void Constructor_ThrowsForEmptyId()
    {
        Assert.Throws<DomainException>(() => new Account(Guid.Empty, "1000", "Kas", AccountType.Assets));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsForBlankNumber(string number)
    {
        Assert.Throws<DomainException>(() => new Account(Guid.NewGuid(), number, "Kas", AccountType.Assets));
    }

    [Fact]
    public void Constructor_ThrowsForBlankName()
    {
        Assert.Throws<DomainException>(() => new Account(Guid.NewGuid(), "1000", " ", AccountType.Assets));
    }

    [Fact]
    public void Constructor_ThrowsWhenCashFlagOnNonAsset()
    {
        Assert.Throws<DomainException>(
            () => new Account(Guid.NewGuid(), "2000", "Creditors", AccountType.Liabilities, isCashEquivalent: true));
    }

    [Fact]
    public void Constructor_ThrowsWhenInventoryFlagOnNonAsset()
    {
        Assert.Throws<DomainException>(
            () => new Account(Guid.NewGuid(), "4000", "Costs", AccountType.Costs, isInventory: true));
    }

    [Fact]
    public void Constructor_TrimsNumberAndName()
    {
        var account = new Account(Guid.NewGuid(), " 1000 ", " Kas ", AccountType.Assets);

        Assert.Equal("1000", account.Number);
        Assert.Equal("Kas", account.Name);
    }

    [Theory]
    [InlineData(AccountType.Assets, true)]
    [InlineData(AccountType.Costs, true)]
    [InlineData(AccountType.Liabilities, false)]
    [InlineData(AccountType.Equity, false)]
    [InlineData(AccountType.Revenue, false)]
    public void HasDebitBalance_ReflectsNormalBalance(AccountType type, bool expected)
    {
        var account = new Account(Guid.NewGuid(), "1000", "Test", type);

        Assert.Equal(expected, account.HasDebitBalance);
    }

    [Fact]
    public void DeactivateAndActivate_ToggleIsActive()
    {
        var account = new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets);

        account.Deactivate();
        Assert.False(account.IsActive);

        account.Activate();
        Assert.True(account.IsActive);
    }

    [Fact]
    public void Rename_UpdatesName()
    {
        var account = new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets);

        account.Rename("Kas en bank");

        Assert.Equal("Kas en bank", account.Name);
    }

    [Fact]
    public void Rename_ThrowsForBlankName()
    {
        var account = new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets);

        Assert.Throws<DomainException>(() => account.Rename("  "));
    }

    [Fact]
    public void SetCashEquivalent_ThrowsForNonAsset()
    {
        var account = new Account(Guid.NewGuid(), "2000", "Creditors", AccountType.Liabilities);

        Assert.Throws<DomainException>(() => account.SetCashEquivalent(true));
    }
}
