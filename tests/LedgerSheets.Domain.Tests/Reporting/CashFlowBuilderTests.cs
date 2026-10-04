using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Services;

namespace LedgerSheets.Domain.Tests.Reporting;

public sealed class CashFlowBuilderTests
{
    private readonly CashFlowBuilder _builder = new();
    private readonly Ledger _ledger = TestLedger.WithScenario();

    [Fact]
    public void BuildOn_SplitCashFlowsIntoThreeCategories()
    {
        var overview = _builder.BuildOn(_ledger, TestLedger.Year2026);

        Assert.Equal(3_500m, overview.NetOperating);
        Assert.Equal(-3_000m, overview.NetInvesting);
        Assert.Equal(14_000m, overview.NetFinancing);
    }

    [Fact]
    public void BuildOn_ReconcilesOpeningAndClosingBalance()
    {
        var overview = _builder.BuildOn(_ledger, TestLedger.Year2026);

        Assert.Equal(0m, overview.OpeningBalance);
        Assert.Equal(14_500m, overview.ClosingBalance);
        Assert.Equal(14_500m, overview.NetMovement);
        Assert.True(overview.IsBalanced);
    }

    [Fact]
    public void BuildOn_ClassifiesExpensePaymentAsOperating()
    {
        var overview = _builder.BuildOn(_ledger, TestLedger.Year2026);

        Assert.Contains(overview.Operating, r => r.Description == "Bedrijfskosten betaald" && r.Amount == -500m);
        Assert.Contains(overview.Operating, r => r.Description == "Contante verkoop" && r.Amount == 4_000m);
    }

    [Fact]
    public void BuildOn_ClassifiesAssetPurchaseAsInvesting()
    {
        var overview = _builder.BuildOn(_ledger, TestLedger.Year2026);

        Assert.Single(overview.Investing, r => r.Amount == -3_000m);
    }

    [Fact]
    public void BuildOn_ClassifiesCapitalAndLoansAsFinancing()
    {
        var overview = _builder.BuildOn(_ledger, TestLedger.Year2026);

        Assert.Contains(overview.Financing, r => r.Amount == 10_000m);
        Assert.Contains(overview.Financing, r => r.Amount == 5_000m);
        Assert.Contains(overview.Financing, r => r.Amount == -1_000m);
    }

    [Fact]
    public void BuildOn_IgnoresEntriesWithoutCashMovement()
    {
        var ledger = new Ledger();
        var inventory = ledger.AddAccount("1300", "Inventory", AccountType.Assets, isCurrent: true, isInventory: true);
        var creditors = ledger.AddAccount("2000", "Creditors", AccountType.Liabilities, isCurrent: true);
        ledger.Post(JournalEntry.New(new DateOnly(2026, 3, 1), "Inkoop op rekening")
            .Debit(inventory.Id, 750m).Credit(creditors.Id, 750m));

        var overview = _builder.BuildOn(ledger, Period.ForYear(2026));

        Assert.Empty(overview.Operating);
        Assert.Empty(overview.Investing);
        Assert.Empty(overview.Financing);
        Assert.True(overview.IsBalanced);
    }

    [Fact]
    public void BuildOn_UsesOpeningBalanceFromPreviousPeriod()
    {
        var ledger = new Ledger();
        var cash = ledger.AddAccount("1000", "Kas", AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var revenue = ledger.AddAccount("8000", "Omzet", AccountType.Revenue);
        ledger.Post(JournalEntry.New(new DateOnly(2025, 6, 1), "Verkoop 2025").Debit(cash.Id, 800m).Credit(revenue.Id, 800m));
        ledger.Post(JournalEntry.New(new DateOnly(2026, 6, 1), "Verkoop 2026").Debit(cash.Id, 200m).Credit(revenue.Id, 200m));

        var overview = _builder.BuildOn(ledger, Period.ForYear(2026));

        Assert.Equal(800m, overview.OpeningBalance);
        Assert.Equal(1_000m, overview.ClosingBalance);
        Assert.Equal(200m, overview.NetMovement);
    }

    [Fact]
    public void BuildOn_ThrowsForNullLedger()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.BuildOn(null!, TestLedger.Year2026));
    }
}
