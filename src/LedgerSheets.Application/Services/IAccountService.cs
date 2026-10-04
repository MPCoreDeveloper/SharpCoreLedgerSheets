using LedgerSheets.Application.Models;
using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Application.Services;

/// <summary>
/// Use cases for managing general-ledger accounts.
/// </summary>
public interface IAccountService
{
    /// <summary>Returns all accounts ordered by number.</summary>
    Task<IReadOnlyList<Account>> AllAsync(CancellationToken ct = default);

    /// <summary>Creates a new account.</summary>
    /// <exception cref="Domain.Common.DomainException">Thrown when the number already exists.</exception>
    Task<Account> AddAsync(AddAccountCommand command, CancellationToken ct = default);

    /// <summary>Renames an account.</summary>
    Task RenameAsync(string number, string newName, CancellationToken ct = default);

    /// <summary>Deactivates an account so it can no longer receive new entries.</summary>
    Task DeactivateAsync(string number, CancellationToken ct = default);
}
