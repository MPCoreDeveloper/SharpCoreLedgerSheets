using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Domain.Tests;

/// <summary>
/// Builds a small but complete ledger used across the domain tests.
/// </summary>
/// <remarks>
/// Scenario (all entries dated 2026):
/// <list type="number">
///   <item>start capital: cash 10.000 / equity 10.000</item>
///   <item>bank loan: bank 5.000 / loan 5.000</item>
///   <item>inventory on credit: inventory 2.000 / creditors 2.000</item>
///   <item>cash sale: cash 4.000 / revenue 4.000</item>
///   <item>cost of goods sold: expense 1.200 / inventory 1.200</item>
///   <item>machine bought from bank: machinery 3.000 / bank 3.000</item>
///   <item>expense paid from bank: expense 500 / bank 500</item>
///   <item>loan repayment from bank: loan 1.000 / bank 1.000</item>
/// </list>
/// Resulting totals: assets 18.300, liabilities 6.000, equity 10.000, result 2.300.
/// </remarks>
internal static class TestLedger
{
    public const string Kas = "1000";
    public const string Bank = "1100";
    public const string Inventory = "1300";
    public const string Machines = "1500";
    public const string Creditors = "2000";
    public const string BankLoan = "2100";
    public const string Equity = "3000";
    public const string Costs = "4000";
    public const string Omzet = "8000";

    /// <summary>2026 is the financial year used by the scenario.</summary>
    public static readonly DateOnly Date = new(2026, 6, 15);

    /// <summary>Full-year period for the scenario.</summary>
    public static Period Year2026 => Period.ForYear(2026);

    /// <summary>Creates the ledger with the complete scenario.</summary>
    public static Ledger WithScenario()
    {
        var ledger = new Ledger();

        var cash = ledger.AddAccount(Kas, "Kas", AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var bank = ledger.AddAccount(Bank, "Bank", AccountType.Assets, isCurrent: true, isCashEquivalent: true);
        var inventory = ledger.AddAccount(Inventory, "Inventory", AccountType.Assets, isCurrent: true, isInventory: true);
        var machines = ledger.AddAccount(Machines, "Machines", AccountType.Assets);
        var creditors = ledger.AddAccount(Creditors, "Creditors", AccountType.Liabilities, isCurrent: true);
        var bankLoan = ledger.AddAccount(BankLoan, "BankLoan", AccountType.Liabilities);
        var equity = ledger.AddAccount(Equity, "Eigen vermogen", AccountType.Equity);
        var costs = ledger.AddAccount(Costs, "Bedrijfskosten", AccountType.Costs);
        var revenue = ledger.AddAccount(Omzet, "Omzet", AccountType.Revenue);

        ledger.Post(JournalEntry.New(Date, "Startkapitaal")
            .Debit(cash.Id, 10_000m)
            .Credit(equity.Id, 10_000m));

        ledger.Post(JournalEntry.New(Date, "Banklening ontvangen")
            .Debit(bank.Id, 5_000m)
            .Credit(bankLoan.Id, 5_000m));

        ledger.Post(JournalEntry.New(Date, "Inkoop voorraad op rekening")
            .Debit(inventory.Id, 2_000m)
            .Credit(creditors.Id, 2_000m));

        ledger.Post(JournalEntry.New(Date, "Contante verkoop")
            .Debit(cash.Id, 4_000m)
            .Credit(revenue.Id, 4_000m));

        ledger.Post(JournalEntry.New(Date, "Inkoopwaarde verkopen")
            .Debit(costs.Id, 1_200m)
            .Credit(inventory.Id, 1_200m));

        ledger.Post(JournalEntry.New(Date, "Machine aangeschaft")
            .Debit(machines.Id, 3_000m)
            .Credit(bank.Id, 3_000m));

        ledger.Post(JournalEntry.New(Date, "Bedrijfskosten betaald")
            .Debit(costs.Id, 500m)
            .Credit(bank.Id, 500m));

        ledger.Post(JournalEntry.New(Date, "Aflossing banklening")
            .Debit(bankLoan.Id, 1_000m)
            .Credit(bank.Id, 1_000m));

        return ledger;
    }

    /// <summary>Creates an empty ledger with only two accounts (debit/credit pair).</summary>
    public static (Ledger Ledger, Account Debit, Account Credit) WithTwoAccounts(
        AccountType debitType = AccountType.Assets,
        AccountType creditType = AccountType.Liabilities)
    {
        var ledger = new Ledger();
        var debit = ledger.AddAccount("9999", "Debetrekening", debitType);
        var credit = ledger.AddAccount("8888", "Creditrekening", creditType);
        return (ledger, debit, credit);
    }
}
