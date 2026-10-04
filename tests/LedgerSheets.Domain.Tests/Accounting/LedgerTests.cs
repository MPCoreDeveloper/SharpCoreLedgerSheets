using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Tests.Accounting;

public sealed class LedgerTests
{
    [Fact]
    public void DefaultsToEuro()
    {
        Assert.Equal(Currency.Eur, new Ledger().Currency);
    }

    [Fact]
    public void AddAccount_AddsAccountWithGeneratedId()
    {
        var ledger = new Ledger();

        var account = ledger.AddAccount("1000", "Kas", AccountType.Assets);

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Single(ledger.Accounts);
    }

    [Fact]
    public void AddAccount_ThrowsForDuplicateNumberCaseInsensitive()
    {
        var ledger = new Ledger();
        ledger.AddAccount("1000", "Kas", AccountType.Assets);

        var exception = Assert.Throws<DomainException>(
            () => ledger.AddAccount("1000", "Kas 2", AccountType.Assets));

        Assert.Contains("bestaat al", exception.Message);
    }

    [Fact]
    public void FindAccount_FindsByNumberCaseInsensitive()
    {
        var ledger = new Ledger();
        var cash = ledger.AddAccount("1000", "Kas", AccountType.Assets);

        Assert.Same(cash, ledger.FindAccount("1000"));
        Assert.Null(ledger.FindAccount("1000 "));
    }

    [Fact]
    public void Post_AddsPostAndComputesSignedBalance()
    {
        var (ledger, debit, credit) = TestLedger.WithTwoAccounts();
        ledger.Post(JournalEntry.New(TestLedger.Date, "Test").Debit(debit.Id, 250m).Credit(credit.Id, 250m));

        Assert.Equal(250m, ledger.Balance(debit.Id));
        Assert.Equal(-250m, ledger.Balance(credit.Id));
    }

    [Fact]
    public void Post_ThrowsForUnknownAccount()
    {
        var ledger = new Ledger();
        var post = JournalEntry.New(TestLedger.Date, "Test")
            .Debit(Guid.NewGuid(), 10m)
            .Credit(Guid.NewGuid(), 10m);

        var exception = Assert.Throws<DomainException>(() => ledger.Post(post));

        Assert.Contains("onbekende rekening", exception.Message);
    }

    [Fact]
    public void Post_ThrowsForInactiveAccount()
    {
        var (ledger, debit, credit) = TestLedger.WithTwoAccounts();
        debit.Deactivate();
        var post = JournalEntry.New(TestLedger.Date, "Test").Debit(debit.Id, 10m).Credit(credit.Id, 10m);

        var exception = Assert.Throws<DomainException>(() => ledger.Post(post));

        Assert.Contains("niet actief", exception.Message);
    }

    [Fact]
    public void Post_ThrowsWhenPostingSameEntryTwice()
    {
        var (ledger, debit, credit) = TestLedger.WithTwoAccounts();
        var post = JournalEntry.New(TestLedger.Date, "Test").Debit(debit.Id, 10m).Credit(credit.Id, 10m);
        ledger.Post(post);

        var exception = Assert.Throws<DomainException>(() => ledger.Post(post));

        Assert.Contains("al geboekt", exception.Message);
    }

    [Fact]
    public void Post_ThrowsForUnbalancedEntry()
    {
        var (ledger, debit, credit) = TestLedger.WithTwoAccounts();
        var post = JournalEntry.New(TestLedger.Date, "Test").Debit(debit.Id, 10m).Credit(credit.Id, 5m);

        Assert.Throws<DomainException>(() => ledger.Post(post));
    }

    [Fact]
    public void Balance_RespectsUpperDateBoundary()
    {
        var (ledger, debit, credit) = TestLedger.WithTwoAccounts();
        ledger.Post(JournalEntry.New(new DateOnly(2026, 1, 31), "Januari").Debit(debit.Id, 100m).Credit(credit.Id, 100m));
        ledger.Post(JournalEntry.New(new DateOnly(2026, 2, 28), "Februari").Debit(debit.Id, 50m).Credit(credit.Id, 50m));

        Assert.Equal(100m, ledger.Balance(debit.Id, new DateOnly(2026, 1, 31)));
        Assert.Equal(150m, ledger.Balance(debit.Id, new DateOnly(2026, 2, 28)));
    }

    [Fact]
    public void MovementInPeriod_OnlyCountsEntriesInsidePeriod()
    {
        var (ledger, debit, credit) = TestLedger.WithTwoAccounts();
        ledger.Post(JournalEntry.New(new DateOnly(2025, 12, 31), "Previous year").Debit(debit.Id, 100m).Credit(credit.Id, 100m));
        ledger.Post(JournalEntry.New(new DateOnly(2026, 3, 1), "Dit year").Debit(debit.Id, 40m).Credit(credit.Id, 40m));

        Assert.Equal(40m, ledger.MovementInPeriod(debit.Id, Period.ForYear(2026)));
    }

    [Fact]
    public void CheckTotal_IsZeroForBalancedLedger()
    {
        Assert.Equal(0m, TestLedger.WithScenario().CheckTotal());
    }

    [Fact]
    public void Entries_AreReadOnly()
    {
        var ledger = TestLedger.WithScenario();

        Assert.Equal(8, ledger.Entries.Count);
        Assert.IsAssignableFrom<IReadOnlyList<JournalEntry>>(ledger.Entries);
    }
}
