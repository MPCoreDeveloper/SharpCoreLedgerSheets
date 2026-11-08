using SharpCoreLedgerSheets.Domain.Services;

namespace SharpCoreLedgerSheets.Domain.Tests.Reporting;

public sealed class RatioCalculatorTests
{
    private readonly BalanceSheetBuilder _balanceSheetBuilder = new();
    private readonly ProfitAndLossBuilder _profitAndLossBuilder = new();
    private readonly RatioCalculator _calculation = new();

    [Fact]
    public void Calculate_ComputesAllRatiosForScenario()
    {
        var ledger = TestLedger.WithScenario();
        var balanceSheet = _balanceSheetBuilder.BuildOn(ledger, TestLedger.Date);
        var profitAndLoss = _profitAndLossBuilder.BuildOn(ledger, TestLedger.Year2026);

        var ratios = _calculation.Calculate(balanceSheet, profitAndLoss);

        Assert.Equal(7.65m, ratios.CurrentRatio);
        Assert.Equal(7.25m, ratios.QuickRatio);
        Assert.Equal(0.6721m, ratios.Solvency);
        Assert.Equal(0.575m, ratios.NetProfitMargin);
        Assert.Equal(0.1870m, ratios.ReturnOnEquity);
    }

    [Fact]
    public void Calculate_ReturnsNullWhenDenominatorIsZero()
    {
        var ledger = new Domain.Accounting.Ledger();
        ledger.AddAccount("1000", "Kas", Domain.Accounting.AccountType.Assets, isCurrent: true, isCashEquivalent: true);

        var balanceSheet = _balanceSheetBuilder.BuildOn(ledger, TestLedger.Date);
        var profitAndLoss = _profitAndLossBuilder.BuildOn(ledger, TestLedger.Year2026);

        var ratios = _calculation.Calculate(balanceSheet, profitAndLoss);

        Assert.Null(ratios.CurrentRatio);
        Assert.Null(ratios.Solvency);
        Assert.Null(ratios.NetProfitMargin);
        Assert.Null(ratios.ReturnOnEquity);
    }

    [Fact]
    public void Calculate_QuickRatioIsLowerWhenInventoryPresent()
    {
        var ledger = TestLedger.WithScenario();
        var balanceSheet = _balanceSheetBuilder.BuildOn(ledger, TestLedger.Date);
        var profitAndLoss = _profitAndLossBuilder.BuildOn(ledger, TestLedger.Year2026);

        var ratios = _calculation.Calculate(balanceSheet, profitAndLoss);

        Assert.True(ratios.QuickRatio < ratios.CurrentRatio!.Value);
    }

    [Fact]
    public void Empty_HasNoValues()
    {
        var empty = Domain.Reporting.RatioOverview.Empty;

        Assert.Null(empty.CurrentRatio);
        Assert.Null(empty.QuickRatio);
        Assert.Null(empty.Solvency);
        Assert.Null(empty.NetProfitMargin);
        Assert.Null(empty.ReturnOnEquity);
    }

    [Fact]
    public void Calculate_ThrowsForNullArguments()
    {
        var balanceSheet = _balanceSheetBuilder.BuildOn(TestLedger.WithScenario(), TestLedger.Date);
        var profitAndLoss = _profitAndLossBuilder.BuildOn(TestLedger.WithScenario(), TestLedger.Year2026);

        Assert.Throws<ArgumentNullException>(() => _calculation.Calculate(null!, profitAndLoss));
        Assert.Throws<ArgumentNullException>(() => _calculation.Calculate(balanceSheet, null!));
    }
}
