using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Reporting;

namespace SharpCoreLedgerSheets.Domain.Services;

/// <summary>
/// Default implementation of <see cref="ICashFlowBuilder"/>.
/// </summary>
/// <remarks>
/// The statement is derived directly from the ledger. Every journal entry that touches a cash
/// account (<see cref="Account.IsCashEquivalent"/>) produces a cash movement which is classified
/// by its counterpart accounts:
/// <list type="bullet">
///   <item>revenue or expense counterpart = operating activity;</item>
///   <item>non-cash asset counterpart = investing activity;</item>
///   <item>liability or equity counterpart = financing activity.</item>
/// </list>
/// Movements are aggregated per category and description.
/// </remarks>
public sealed class CashFlowBuilder : ICashFlowBuilder
{
    /// <inheritdoc />
    public CashFlowOverview BuildOn(Ledger ledger, Period period)
    {
        ArgumentNullException.ThrowIfNull(ledger);

        var accounts = ledger.Accounts.ToDictionary(r => r.Id);
        var cash = ledger.Accounts.Where(r => r.IsCashEquivalent).Select(r => r.Id).ToHashSet();

        var openingBalance = BalanceCash(ledger, cash, period.Start == DateOnly.MinValue ? null : period.Start.AddDays(-1));
        var closingBalance = BalanceCash(ledger, cash, period.Closing);

        var perCategory = new Dictionary<CashFlowCategory, Dictionary<string, decimal>>(3)
        {
            [CashFlowCategory.Operating] = [],
            [CashFlowCategory.Investing] = [],
            [CashFlowCategory.Financing] = [],
        };

        foreach (var entry in ledger.Entries.Where(p => period.Contains(p.Date)))
        {
            if (entry.Lines.All(r => !cash.Contains(r.AccountId)))
            {
                continue;
            }

            var cashMovement = entry.Lines.Where(r => cash.Contains(r.AccountId)).Sum(r => r.Movement);
            if (cashMovement == 0m)
            {
                continue;
            }

            var counterparties = entry.Lines.Where(r => !cash.Contains(r.AccountId)).ToList();
            var category = DetermineCategory(counterparties, accounts);
            var description = string.IsNullOrWhiteSpace(entry.Description) ? "Overige kasstroom" : entry.Description;

            var categoryLines = perCategory[category];
            categoryLines[description] = categoryLines.TryGetValue(description, out var current)
                ? current + cashMovement
                : cashMovement;
        }

        return new CashFlowOverview(
            ToLines(perCategory[CashFlowCategory.Operating]),
            ToLines(perCategory[CashFlowCategory.Investing]),
            ToLines(perCategory[CashFlowCategory.Financing]),
            openingBalance,
            closingBalance,
            period);
    }

    private static decimal BalanceCash(Ledger ledger, IReadOnlySet<Guid> cash, DateOnly? through)
    {
        if (cash.Count == 0)
        {
            return 0m;
        }

        return cash.Sum(accountId => ledger.Balance(accountId, through));
    }

    private static CashFlowCategory DetermineCategory(
        IReadOnlyList<JournalEntryLine> counterparties,
        IReadOnlyDictionary<Guid, Account> accounts)
    {
        var types = counterparties
            .Where(r => accounts.ContainsKey(r.AccountId))
            .Select(r => accounts[r.AccountId].Type)
            .ToHashSet();

        if (types.Contains(AccountType.Revenue) || types.Contains(AccountType.Costs))
        {
            return CashFlowCategory.Operating;
        }

        if (types.Contains(AccountType.Assets))
        {
            return CashFlowCategory.Investing;
        }

        if (types.Contains(AccountType.Liabilities) || types.Contains(AccountType.Equity))
        {
            return CashFlowCategory.Financing;
        }

        return CashFlowCategory.Operating;
    }

    private static List<BalanceLine> ToLines(Dictionary<string, decimal> movements) =>
        movements
            .Where(kv => kv.Value != 0m)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new BalanceLine(null, kv.Key, kv.Value))
            .ToList();
}
