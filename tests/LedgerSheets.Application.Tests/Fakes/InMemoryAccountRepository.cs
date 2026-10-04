using LedgerSheets.Application.Abstractions;
using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Application.Tests.Fakes;

/// <summary>In-memory <see cref="IAccountRepository"/> used by the application tests.</summary>
internal sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly Dictionary<Guid, Account> _accounts = [];

    /// <summary>How often the accounts were read; proves that a batch proposal reads them once.</summary>
    public int CountTimesRead { get; private set; }

    public Task<IReadOnlyList<Account>> AllAsync(CancellationToken ct = default)
    {
        CountTimesRead++;

        return Task.FromResult<IReadOnlyList<Account>>(
            _accounts.Values.OrderBy(r => r.Number, StringComparer.Ordinal).ToList());
    }

    public Task<Account?> FindOnNumberAsync(string number, CancellationToken ct = default) =>
        Task.FromResult(_accounts.Values.FirstOrDefault(
            r => string.Equals(r.Number, number, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        if (_accounts.ContainsKey(account.Id))
        {
            throw new DomainException("Deze rekening bestaat al in de opslag.");
        }

        _accounts.Add(account.Id, account);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        if (!_accounts.ContainsKey(account.Id))
        {
            throw new DomainException("Deze rekening bestaat niet in de opslag.");
        }

        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }
}
