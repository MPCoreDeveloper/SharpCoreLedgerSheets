using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Application.Exceptions;
using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Services;

/// <summary>
/// Default <see cref="IJournalEntryService"/>.
/// </summary>
/// <remarks>
/// Account numbers are resolved to account identities here so the domain stays free of
/// presentation concerns, and so a typo results in a clear, localised error message.
/// </remarks>
public sealed class JournalEntryService(
    IAccountRepository accountRepository,
    IJournalEntryRepository journalEntryRepository) : IJournalEntryService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<JournalEntry>> AllAsync(Period? period = null, CancellationToken ct = default) =>
        journalEntryRepository.AllAsync(period, ct);

    /// <inheritdoc />
    public async Task<JournalEntry> PostAsync(PostJournalEntryCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var accounts = (await accountRepository.AllAsync(ct).ConfigureAwait(false))
            .ToDictionary(r => r.Number, StringComparer.OrdinalIgnoreCase);

        var entry = JournalEntry.New(command.Date, command.Description, command.Reference);

        foreach (var line in command.Lines)
        {
            if (!accounts.TryGetValue(line.AccountNumber, out var account))
            {
                throw new NotFoundException($"Rekening '{line.AccountNumber}' bestaat niet.");
            }

            if (!account.IsActive)
            {
                throw new DomainException(MessageCode.AccountInactive, "Rekening '{0}' ({1}) is niet actief.", account.Number, account.Name);
            }

            entry.AddLine(account.Id, line.Debit, line.Credit, line.Description);
        }

        entry.Validate();
        await journalEntryRepository.AddAsync(entry, ct).ConfigureAwait(false);
        return entry;
    }

    /// <inheritdoc />
    public Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Geef een geldig id op.", nameof(id));
        }

        return journalEntryRepository.RemoveAsync(id, ct);
    }
}
