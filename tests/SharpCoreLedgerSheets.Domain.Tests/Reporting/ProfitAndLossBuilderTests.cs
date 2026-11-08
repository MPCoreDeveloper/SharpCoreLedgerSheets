using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Services;

namespace SharpCoreLedgerSheets.Domain.Tests.Reporting;

public sealed class ProfitAndLossBuilderTests
{
    private readonly ProfitAndLossBuilder _builder = new();
    private readonly Ledger _ledger = TestLedger.WithScenario();

    [Fact]
    public void BuildOn_ComputesRevenueCostsAndResult()
    {
        var report = _builder.BuildOn(_ledger, TestLedger.Year2026);

        Assert.Equal(4_000m, report.TotalRevenue);
        Assert.Equal(1_700m, report.TotalCosts);
        Assert.Equal(2_300m, report.Result);
        Assert.True(report.IsProfit);
        Assert.False(report.IsLoss);
    }

    [Fact]
    public void BuildOn_PresentsRevenueAndCostsAsPositiveAmounts()
    {
        var report = _builder.BuildOn(_ledger, TestLedger.Year2026);

        Assert.Single(report.Revenue, r => r.Number == TestLedger.Omzet && r.Amount == 4_000m);
        Assert.Single(report.Costs, r => r.Number == TestLedger.Costs && r.Amount == 1_700m);
    }

    [Fact]
    public void BuildOn_ExcludesBalanceSheetAccounts()
    {
        var report = _builder.BuildOn(_ledger, TestLedger.Year2026);

        Assert.All(report.Revenue, r => Assert.Equal(TestLedger.Omzet, r.Number));
        Assert.All(report.Costs, r => Assert.Equal(TestLedger.Costs, r.Number));
    }

    [Fact]
    public void BuildOn_OnlyCountsEntriesInsidePeriod()
    {
        var ledger = new Ledger();
        var cash = ledger.AddAccount("1000", "Kas", AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var revenue = ledger.AddAccount("8000", "Omzet", AccountType.Revenue);
        ledger.Post(JournalEntry.New(new DateOnly(2025, 12, 31), "Previous year").Debit(cash.Id, 900m).Credit(revenue.Id, 900m));
        ledger.Post(JournalEntry.New(new DateOnly(2026, 6, 1), "Dit year").Debit(cash.Id, 100m).Credit(revenue.Id, 100m));

        var report = _builder.BuildOn(ledger, Period.ForYear(2026));

        Assert.Equal(100m, report.TotalRevenue);
    }

    [Fact]
    public void BuildOn_ReportsLossWhenCostsExceedRevenue()
    {
        var ledger = new Ledger();
        var cash = ledger.AddAccount("1000", "Kas", AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var costs = ledger.AddAccount("4000", "Costs", AccountType.Costs);
        ledger.Post(JournalEntry.New(new DateOnly(2026, 6, 1), "Costs").Debit(costs.Id, 500m).Credit(cash.Id, 500m));

        var report = _builder.BuildOn(ledger, Period.ForYear(2026));

        Assert.Equal(-500m, report.Result);
        Assert.True(report.IsLoss);
        Assert.Null(report.NetMargin);
    }

    [Fact]
    public void BuildOn_ThrowsForNullLedger()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.BuildOn(null!, TestLedger.Year2026));
    }
}
