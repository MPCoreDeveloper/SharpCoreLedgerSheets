using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Application.Services;

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

    /// <summary>
    /// Creates every account of a starter chart that the administration does not have yet, so a new
    /// administration starts from a complete, conventional chart instead of an empty page.
    /// </summary>
    /// <param name="chart">The chart to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of accounts that were created.</returns>
    /// <remarks>
    /// Existing ledger numbers are never overwritten or renamed: the operation is idempotent and safe to
    /// repeat, which is what makes it usable as a "give me a base chart" button.
    /// </remarks>
    Task<int> ApplyStarterChartAsync(StarterChart chart, CancellationToken ct = default);
}
