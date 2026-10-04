using LedgerSheets.Application.Abstractions;
using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Application.Services;

/// <summary>
/// Default <see cref="ILedgerLoader"/>: rehydrates the ledger from the repositories.
/// </summary>
public sealed class LedgerLoader(
    IAccountRepository accountRepository,
    IJournalEntryRepository journalEntryRepository) : ILedgerLoader
{
    /// <inheritdoc />
    public async Task<Ledger> LoadAsync(CancellationToken ct = default)
    {
        var ledger = new Ledger();

        foreach (var account in await accountRepository.AllAsync(ct).ConfigureAwait(false))
        {
            ledger.RegisterAccount(account);
        }

        foreach (var post in await journalEntryRepository.AllAsync(period: null, ct).ConfigureAwait(false))
        {
            ledger.Restore(post);
        }

        return ledger;
    }
}
