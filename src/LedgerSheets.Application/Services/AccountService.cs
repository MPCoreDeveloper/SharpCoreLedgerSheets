using LedgerSheets.Application.Abstractions;
using LedgerSheets.Application.Exceptions;
using LedgerSheets.Application.Models;
using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Application.Services;

/// <summary>
/// Default <see cref="IAccountService"/>.
/// </summary>
public sealed class AccountService(IAccountRepository repository) : IAccountService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<Account>> AllAsync(CancellationToken ct = default) =>
        repository.AllAsync(ct);

    /// <inheritdoc />
    public async Task<Account> AddAsync(AddAccountCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await repository.FindOnNumberAsync(command.Number, ct).ConfigureAwait(false) is not null)
        {
            throw new DomainException($"Rekeningnummer '{command.Number}' bestaat al.");
        }

        var account = new Account(
            Guid.NewGuid(),
            command.Number,
            command.Name,
            command.Type,
            isActive: true,
            isCurrent: command.Current,
            isCashEquivalent: command.CashEquivalent,
            isInventory: command.Inventory);

        await repository.AddAsync(account, ct).ConfigureAwait(false);
        return account;
    }

    /// <inheritdoc />
    public async Task RenameAsync(string number, string newName, CancellationToken ct = default)
    {
        var account = await FindAsync(number, ct).ConfigureAwait(false);
        account.Rename(newName);
        await repository.UpdateAsync(account, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeactivateAsync(string number, CancellationToken ct = default)
    {
        var account = await FindAsync(number, ct).ConfigureAwait(false);
        account.Deactivate();
        await repository.UpdateAsync(account, ct).ConfigureAwait(false);
    }

    private async Task<Account> FindAsync(string number, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        return await repository.FindOnNumberAsync(number, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Rekening '{number}' is niet gevonden.");
    }
}
