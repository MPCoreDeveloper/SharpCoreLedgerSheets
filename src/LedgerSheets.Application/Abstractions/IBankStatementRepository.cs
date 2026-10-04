using LedgerSheets.Domain.Banking;

namespace LedgerSheets.Application.Abstractions;

/// <summary>
/// Read/write port for imported bank transactions.
/// </summary>
/// <remarks>
/// Importing the same statement twice must never duplicate a movement, which is why
/// <see cref="ExistingKeysAsync"/> exists: the import compares the keys of the file with the
/// keys that are already stored.
/// </remarks>
public interface IBankStatementRepository
{
    /// <summary>Returns the transactions ordered by date, optionally restricted to one status.</summary>
    Task<IReadOnlyList<BankTransaction>> AllAsync(
        BankTransactionStatus? status = null,
        CancellationToken ct = default);

    /// <summary>Finds a transaction by its identifier, or <see langword="null"/>.</summary>
    Task<BankTransaction?> FindAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the import keys (<see cref="BankTransaction.Key"/>) of everything already stored.</summary>
    Task<IReadOnlyList<string>> ExistingKeysAsync(CancellationToken ct = default);

    /// <summary>Persists new transactions.</summary>
    Task AddAsync(IEnumerable<BankTransaction> transactions, CancellationToken ct = default);

    /// <summary>Persists a changed transaction, for example after booking or ignoring it.</summary>
    Task UpdateAsync(BankTransaction transaction, CancellationToken ct = default);
}
