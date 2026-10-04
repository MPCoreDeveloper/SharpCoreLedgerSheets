using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Application.Abstractions;

/// <summary>
/// Read/write port for the general-ledger accounts ("grootboekrekeningen").
/// </summary>
/// <remarks>
/// A ledger owns its storage; there is exactly one administration per database, which keeps the
/// contract simple for the target audience (small businesses and individuals).
/// </remarks>
public interface IAccountRepository
{
    /// <summary>Returns all accounts ordered by number.</summary>
    Task<IReadOnlyList<Account>> AllAsync(CancellationToken ct = default);

    /// <summary>Finds an account by its ledger number (case-insensitive), or <see langword="null"/>.</summary>
    Task<Account?> FindOnNumberAsync(string number, CancellationToken ct = default);

    /// <summary>Persists a new account.</summary>
    Task AddAsync(Account account, CancellationToken ct = default);

    /// <summary>Persists changes to an existing account.</summary>
    Task UpdateAsync(Account account, CancellationToken ct = default);
}
