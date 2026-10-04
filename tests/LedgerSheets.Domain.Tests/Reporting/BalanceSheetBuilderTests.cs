using LedgerSheets.Domain.Services;

namespace LedgerSheets.Domain.Tests.Reporting;

public sealed class BalanceSheetBuilderTests
{
    private readonly BalanceSheetBuilder _builder = new();
    private readonly Domain.Accounting.Ledger _ledger = TestLedger.WithScenario();

    [Fact]
    public void BuildOn_TotalsMatchScenario()
    {
        var balanceSheet = _builder.BuildOn(_ledger, TestLedger.Date);

        Assert.Equal(18_300m, balanceSheet.TotalAssets);
        Assert.Equal(6_000m, balanceSheet.TotalLiabilities);
        Assert.Equal(10_000m, balanceSheet.TotalEquity);
        Assert.Equal(2_300m, balanceSheet.Result);
    }

    [Fact]
    public void BuildOn_Balances()
    {
        var balanceSheet = _builder.BuildOn(_ledger, TestLedger.Date);

        Assert.True(balanceSheet.IsBalanced);
        Assert.Equal(0m, balanceSheet.Difference);
        Assert.Equal(balanceSheet.TotalCreditSide, balanceSheet.TotalAssets);
    }

    [Fact]
    public void BuildOn_PresentsCreditAccountsAsPositiveAmounts()
    {
        var balanceSheet = _builder.BuildOn(_ledger, TestLedger.Date);

        Assert.Equal(2_000m, balanceSheet.Liabilities.Single(r => r.Number == TestLedger.Creditors).Amount);
        Assert.Equal(4_000m, balanceSheet.Liabilities.Single(r => r.Number == TestLedger.BankLoan).Amount);
        Assert.Equal(10_000m, balanceSheet.Equity.Single().Amount);
    }

    [Fact]
    public void BuildOn_CopiesClassificationFlags()
    {
        var balanceSheet = _builder.BuildOn(_ledger, TestLedger.Date);

        Assert.True(balanceSheet.Assets.Single(r => r.Number == TestLedger.Kas).CashEquivalent);
        Assert.True(balanceSheet.Assets.Single(r => r.Number == TestLedger.Inventory).Inventory);
        Assert.True(balanceSheet.Liabilities.Single(r => r.Number == TestLedger.Creditors).Current);
        Assert.False(balanceSheet.Assets.Single(r => r.Number == TestLedger.Machines).Current);
    }

    [Fact]
    public void BuildOn_OmitsAccountsWithoutBalance()
    {
        var ledger = new Domain.Accounting.Ledger();
        var cash = ledger.AddAccount("1000", "Kas", Domain.Accounting.AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var bank = ledger.AddAccount("1100", "Bank", Domain.Accounting.AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var revenue = ledger.AddAccount("8000", "Omzet", Domain.Accounting.AccountType.Revenue);
        ledger.Post(LedgerSheets.Domain.Accounting.JournalEntry.New(new DateOnly(2026, 5, 1), "Verkoop")
            .Debit(cash.Id, 500m).Credit(revenue.Id, 500m));

        var balanceSheet = _builder.BuildOn(ledger, TestLedger.Date);

        Assert.Single(balanceSheet.Assets, r => r.Number == cash.Number);
        Assert.DoesNotContain(balanceSheet.Assets, r => r.Number == bank.Number);
    }

    [Fact]
    public void BuildOn_RespectsAsOfDate()
    {
        var ledger = new Domain.Accounting.Ledger();
        var cash = ledger.AddAccount("1000", "Kas", Domain.Accounting.AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var revenue = ledger.AddAccount("8000", "Omzet", Domain.Accounting.AccountType.Revenue);
        ledger.Post(LedgerSheets.Domain.Accounting.JournalEntry.New(new DateOnly(2026, 5, 1), "Verkoop")
            .Debit(cash.Id, 1_000m).Credit(revenue.Id, 1_000m));

        var before = _builder.BuildOn(ledger, new DateOnly(2026, 4, 30));
        var after = _builder.BuildOn(ledger, new DateOnly(2026, 5, 1));

        Assert.Equal(0m, before.TotalAssets);
        Assert.Equal(1_000m, after.TotalAssets);
    }

    [Fact]
    public void BuildOn_ThrowsForNullLedger()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.BuildOn(null!, TestLedger.Date));
    }
}
