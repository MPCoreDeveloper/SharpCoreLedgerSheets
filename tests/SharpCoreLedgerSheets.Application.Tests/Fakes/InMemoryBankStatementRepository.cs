using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Banking;

namespace SharpCoreLedgerSheets.Application.Tests.Fakes;

/// <summary>In-memory <see cref="IBankStatementRepository"/> used by the application tests.</summary>
internal sealed class InMemoryBankStatementRepository : IBankStatementRepository
{
    private readonly Dictionary<Guid, BankTransaction> _transactions = [];

    public Task<IReadOnlyList<BankTransaction>> AllAsync(
        BankTransactionStatus? status = null,
        CancellationToken ct = default)
    {
        IEnumerable<BankTransaction> query = _transactions.Values;

        if (status.HasValue)
        {
            query = query.Where(transaction => transaction.Status == status.Value);
        }

        return Task.FromResult<IReadOnlyList<BankTransaction>>(
            query.OrderBy(transaction => transaction.Date)
                .ThenBy(transaction => transaction.Reference, StringComparer.Ordinal)
                .ToList());
    }

    public Task<BankTransaction?> FindAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_transactions.TryGetValue(id, out var transaction) ? transaction : null);

    public Task<IReadOnlyList<string>> ExistingKeysAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(_transactions.Values.Select(t => t.Key).ToList());

    public Task AddAsync(IEnumerable<BankTransaction> transactions, CancellationToken ct = default)
    {
        foreach (var transaction in transactions)
        {
            _transactions[transaction.Id] = transaction;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(BankTransaction transaction, CancellationToken ct = default)
    {
        _transactions[transaction.Id] = transaction;
        return Task.CompletedTask;
    }
}
