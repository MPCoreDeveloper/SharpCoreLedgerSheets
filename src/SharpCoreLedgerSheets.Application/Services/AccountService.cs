using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Application.Exceptions;
using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Services;

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
            throw new DomainException(MessageCode.AccountNumberExists, "Rekeningnummer '{0}' bestaat al.", command.Number);
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

    /// <inheritdoc />
    public async Task<int> ApplyStarterChartAsync(StarterChart chart, CancellationToken ct = default)
    {
        var existing = await repository.AllAsync(ct).ConfigureAwait(false);

        // Case-insensitive because ledger numbers are numbers: "1000" and "1000 " are the same account.
        var numbers = existing
            .Select(account => account.Number)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;

        foreach (var template in StarterCharts.For(chart))
        {
            if (!numbers.Add(template.Number))
            {
                continue;
            }

            await repository.AddAsync(
                new Account(
                    Guid.NewGuid(),
                    template.Number,
                    template.Name,
                    template.Type,
                    isActive: true,
                    isCurrent: template.IsCurrent,
                    isCashEquivalent: template.IsCashEquivalent,
                    isInventory: template.IsInventory),
                ct).ConfigureAwait(false);

            created++;
        }

        return created;
    }

    private async Task<Account> FindAsync(string number, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        return await repository.FindOnNumberAsync(number, ct).ConfigureAwait(false)
            ?? throw new NotFoundException(MessageCode.AccountNotFound, "Rekening '{0}' is niet gevonden.", number);
    }
}
