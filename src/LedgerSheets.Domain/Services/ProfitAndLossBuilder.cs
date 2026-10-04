using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Reporting;

namespace LedgerSheets.Domain.Services;

/// <summary>
/// Default implementation of <see cref="IProfitAndLossBuilder"/>.
/// </summary>
/// <remarks>
/// Only revenue and expense accounts contribute; the amounts are the movements within the period.
/// Accounts with no movement in the period are omitted.
/// </remarks>
public sealed class ProfitAndLossBuilder : IProfitAndLossBuilder
{
    /// <inheritdoc />
    public ProfitAndLoss BuildOn(Ledger ledger, Period period)
    {
        ArgumentNullException.ThrowIfNull(ledger);

        var revenue = new List<BalanceLine>();
        var costs = new List<BalanceLine>();

        foreach (var account in ledger.Accounts.OrderBy(r => r.Number, StringComparer.Ordinal))
        {
            var movement = ledger.MovementInPeriod(account.Id, period);
            if (movement == 0m)
            {
                continue;
            }

            switch (account.Type)
            {
                case AccountType.Revenue:
                    // Revenue has a credit balance; negate so the reported amount is positive.
                    revenue.Add(new BalanceLine(account.Number, account.Name, -movement));
                    break;

                case AccountType.Costs:
                    costs.Add(new BalanceLine(account.Number, account.Name, movement));
                    break;

                default:
                    break;
            }
        }

        return new ProfitAndLoss(
            revenue,
            costs,
            revenue.Sum(r => r.Amount),
            costs.Sum(r => r.Amount),
            period);
    }
}
