using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreLedgerSheets.Domain.Services;

namespace SharpCoreLedgerSheets.Domain.Tests.Reporting;

/// <summary>
/// The VAT return is read from the VAT accounts, so the expectations below are hand-computed from the
/// bookings: a 121 sale at 21% and a 100 purchase at 21% net out to zero.
/// </summary>
public sealed class VatReturnBuilderTests
{
    private static readonly Period Q1 = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
    private static readonly Period Q2 = new(new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));

    private readonly Ledger _ledger = new();
    private readonly VatReturnBuilder _builder = new();

    [Fact]
    public void BuildOn_ReportsPerRateOutputAndInputVat()
    {
        var accounts = Seed();
        Post(_ledger, new DateOnly(2026, 2, 10), "Verkoop", (accounts.Bank, 121m, 0m), (accounts.Revenue, 0m, 100m), (accounts.VatOut21, 0m, 21m));
        Post(_ledger, new DateOnly(2026, 2, 12), "Inkoop", (accounts.Costs, 100m, 0m), (accounts.VatIn21, 21m, 0m), (accounts.Bank, 0m, 121m));

        var vat = _builder.BuildOn(_ledger, VatScheme.Dutch, Q1);

        var line = vat.Lines.Single(l => l.RateCode == "21");
        Assert.Equal(21m, line.OutputVat);
        Assert.Equal(21m, line.InputVat);
        Assert.Equal(0m, line.Balance);
        Assert.Equal(21m, vat.TotalOutputVat);
        Assert.Equal(21m, vat.TotalInputVat);
        Assert.Equal(0m, vat.Balance);
    }

    [Fact]
    public void BuildOn_ShowsWhatHasToBePaidWhenOutputExceedsInput()
    {
        var accounts = Seed();
        Post(_ledger, new DateOnly(2026, 2, 10), "Verkoop", (accounts.Bank, 121m, 0m), (accounts.Revenue, 0m, 100m), (accounts.VatOut21, 0m, 21m));

        var vat = _builder.BuildOn(_ledger, VatScheme.Dutch, Q1);

        Assert.True(vat.IsPayable);
        Assert.Equal(21m, vat.Payable);
        Assert.Equal(0m, vat.Reclaimable);
    }

    [Fact]
    public void BuildOn_ShowsWhatCanBeReclaimedWhenInputExceedsOutput()
    {
        var accounts = Seed();
        Post(_ledger, new DateOnly(2026, 2, 12), "Inkoop", (accounts.Costs, 100m, 0m), (accounts.VatIn21, 21m, 0m), (accounts.Bank, 0m, 121m));

        var vat = _builder.BuildOn(_ledger, VatScheme.Dutch, Q1);

        Assert.False(vat.IsPayable);
        Assert.Equal(0m, vat.Payable);
        Assert.Equal(21m, vat.Reclaimable);
    }

    [Fact]
    public void BuildOn_IgnoresBookingsOutsideThePeriod()
    {
        var accounts = Seed();
        Post(_ledger, new DateOnly(2026, 5, 10), "Verkoop Q2", (accounts.Bank, 121m, 0m), (accounts.Revenue, 0m, 100m), (accounts.VatOut21, 0m, 21m));

        var firstQuarter = _builder.BuildOn(_ledger, VatScheme.Dutch, Q1);
        var secondQuarter = _builder.BuildOn(_ledger, VatScheme.Dutch, Q2);

        Assert.Equal(0m, firstQuarter.TotalOutputVat);
        Assert.Equal(21m, secondQuarter.TotalOutputVat);
    }

    [Fact]
    public void BuildOn_KeepsTheReducedRateOnItsOwnLine()
    {
        var accounts = Seed();
        Post(_ledger, new DateOnly(2026, 2, 10), "Verkoop 9%", (accounts.Bank, 109m, 0m), (accounts.Revenue, 0m, 100m), (accounts.VatOut9!, 0m, 9m));

        var vat = _builder.BuildOn(_ledger, VatScheme.Dutch, Q1);

        Assert.Equal(9m, vat.Lines.Single(l => l.RateCode == "9").OutputVat);
        Assert.Equal(0m, vat.Lines.Single(l => l.RateCode == "21").OutputVat);
        Assert.Equal(9m, vat.Payable);
        Assert.NotNull(accounts.VatOut9);
        Assert.NotNull(accounts.VatIn9);
    }

    [Fact]
    public void BuildOn_ReportsZero_ForARateWithoutAccountsInTheChart()
    {
        // Only the 21% accounts exist: the 9% and 0% rates must not throw, they simply report nothing.
        var accounts = Seed(withReducedRate: false);

        var vat = _builder.BuildOn(_ledger, VatScheme.Dutch, Q1);

        Assert.Equal(3, vat.Lines.Count);
        Assert.All(vat.Lines, line => Assert.Equal(0m, line.OutputVat));
        Assert.Equal(0m, vat.Balance);
        Assert.True(vat.IsPayable);
    }

    [Fact]
    public void BuildOn_ReportsNothing_ForAnEmptyScheme()
    {
        var vat = _builder.BuildOn(_ledger, VatScheme.None, Q1);

        Assert.Empty(vat.Lines);
        Assert.Equal(0m, vat.TotalOutputVat);
        Assert.Equal(0m, vat.Balance);
    }

    private VatAccounts Seed(bool withReducedRate = true)
    {
        var bank = _ledger.AddAccount("1100", "Bank", AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var revenue = _ledger.AddAccount("8000", "Omzet", AccountType.Revenue);
        var costs = _ledger.AddAccount("7000", "Inkoop", AccountType.Costs);
        var vatOut21 = _ledger.AddAccount("2100", "Btw 21%", AccountType.Liabilities, isCurrent: true);
        var vatIn21 = _ledger.AddAccount("1110", "Voorbelasting 21%", AccountType.Assets, isCurrent: true);

        var vatOut9 = withReducedRate
            ? _ledger.AddAccount("2101", "Btw 9%", AccountType.Liabilities, isCurrent: true)
            : null;
        var vatIn9 = withReducedRate
            ? _ledger.AddAccount("1111", "Voorbelasting 9%", AccountType.Assets, isCurrent: true)
            : null;

        return new VatAccounts(bank, revenue, costs, vatOut21, vatIn21, vatOut9, vatIn9);
    }

    private static void Post(Ledger ledger, DateOnly date, string description, params (Account Account, decimal Debit, decimal Credit)[] lines)
    {
        var entry = JournalEntry.New(date, description);

        foreach (var (account, debit, credit) in lines)
        {
            entry.AddLine(account.Id, debit, credit);
        }

        ledger.Post(entry);
    }

    private sealed record VatAccounts(
        Account Bank,
        Account Revenue,
        Account Costs,
        Account VatOut21,
        Account VatIn21,
        Account? VatOut9,
        Account? VatIn9);
}
