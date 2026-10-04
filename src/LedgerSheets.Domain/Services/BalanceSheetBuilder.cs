using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Reporting;

namespace LedgerSheets.Domain.Services;

/// <summary>
/// Default implementation of <see cref="IBalanceSheetBuilder"/>.
/// </summary>
/// <remarks>
/// Accounts are valued at their cumulative signed balance up to the reporting date. Assets and
/// expenses have a normal debit balance; liabilities, equity and revenue have a normal credit
/// balance and are therefore sign-flipped for display. Accounts with a zero balance are omitted.
/// </remarks>
public sealed class BalanceSheetBuilder : IBalanceSheetBuilder
{
    /// <inheritdoc />
    public BalanceSheet BuildOn(Ledger ledger, DateOnly perDate)
    {
        ArgumentNullException.ThrowIfNull(ledger);

        var assets = new List<BalanceLine>();
        var liabilities = new List<BalanceLine>();
        var equity = new List<BalanceLine>();
        var result = 0m;

        foreach (var account in ledger.Accounts.OrderBy(r => r.Number, StringComparer.Ordinal))
        {
            var balance = ledger.Balance(account.Id, perDate);
            if (balance == 0m)
            {
                continue;
            }

            switch (account.Type)
            {
                case AccountType.Assets:
                    assets.Add(new BalanceLine(account.Number, account.Name, balance)
                    {
                        Current = account.IsCurrent,
                        Inventory = account.IsInventory,
                        CashEquivalent = account.IsCashEquivalent,
                    });
                    break;

                case AccountType.Liabilities:
                    liabilities.Add(new BalanceLine(account.Number, account.Name, -balance)
                    {
                        Current = account.IsCurrent,
                    });
                    break;

                case AccountType.Equity:
                    equity.Add(new BalanceLine(account.Number, account.Name, -balance));
                    break;

                case AccountType.Revenue:
                case AccountType.Costs:
                    // Revenue has a credit balance, expenses a debit balance; negating keeps
                    // "revenue minus expenses" correct for both.
                    result -= balance;
                    break;

                default:
                    break;
            }
        }

        return new BalanceSheet(
            assets,
            liabilities,
            equity,
            assets.Sum(r => r.Amount),
            liabilities.Sum(r => r.Amount),
            equity.Sum(r => r.Amount),
            result,
            perDate);
    }
}
