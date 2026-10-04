using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Services;
using LedgerSheets.Application.Models;
using LedgerSheets.Reports.Models;

namespace LedgerSheets.Reports.Tests;

/// <summary>
/// Builds a small, fully consistent financial overview for the report tests.
/// </summary>
/// <remarks>
/// Scenario: cash 5.000 + bank loan 3.000 + cash sale 2.000 - expenses 800 (paid from bank)
/// + inventory 600 on credit. Assets 9.800 = liabilities 3.600 + equity 5.000 + result 1.200.
/// </remarks>
internal static class TestOverview
{
    private static readonly DateOnly Date = new(2026, 6, 15);

    internal static FinancialOverview Create()
    {
        var ledger = new Ledger();
        var cash = ledger.AddAccount("1000", "Cash", AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var bank = ledger.AddAccount("1100", "Bank", AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var inventory = ledger.AddAccount("1300", "Inventory", AccountType.Assets, isCurrent: true, isInventory: true);
        var creditors = ledger.AddAccount("2000", "Creditors", AccountType.Liabilities, isCurrent: true);
        var loan = ledger.AddAccount("2100", "BankLoan", AccountType.Liabilities);
        var equity = ledger.AddAccount("3000", "Equity", AccountType.Equity);
        var costs = ledger.AddAccount("4000", "Operating expenses", AccountType.Costs);
        var revenue = ledger.AddAccount("8000", "Revenue", AccountType.Revenue);

        ledger.Post(JournalEntry.New(Date, "Opening capital").Debit(cash.Id, 5_000m).Credit(equity.Id, 5_000m));
        ledger.Post(JournalEntry.New(Date, "BankLoan").Debit(bank.Id, 3_000m).Credit(loan.Id, 3_000m));
        ledger.Post(JournalEntry.New(Date, "Cash sale").Debit(cash.Id, 2_000m).Credit(revenue.Id, 2_000m));
        ledger.Post(JournalEntry.New(Date, "Operating expenses").Debit(costs.Id, 800m).Credit(bank.Id, 800m));
        ledger.Post(JournalEntry.New(Date, "Purchase on credit").Debit(inventory.Id, 600m).Credit(creditors.Id, 600m));

        var period = Period.ForYear(2026);
        var balanceSheet = new BalanceSheetBuilder().BuildOn(ledger, period.Closing);
        var profitAndLoss = new ProfitAndLossBuilder().BuildOn(ledger, period);
        var cashFlow = new CashFlowBuilder().BuildOn(ledger, period);
        var ratio = new RatioCalculator().Calculate(balanceSheet, profitAndLoss);

        return new FinancialOverview(balanceSheet, profitAndLoss, cashFlow, ratio, period);
    }

    internal static FinancialOverviewModel CreateModel(string title = "Financial overview") =>
        new(Create())
        {
            Metadata = new ReportMetadata
            {
                Title = title,
                Description = "Test report",
                Period = "2026-01-01 t/m 2026-12-31",
                Administration = "Testonderneming BV",
                GeneratedOn = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            },
        };
}
