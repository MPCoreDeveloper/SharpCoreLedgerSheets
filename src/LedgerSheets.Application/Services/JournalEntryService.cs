using LedgerSheets.Application.Abstractions;
using LedgerSheets.Application.Exceptions;
using LedgerSheets.Application.Models;
using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Application.Services;

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

        var post = JournalEntry.New(command.Date, command.Description, command.Reference);

        foreach (var line in command.Lines)
        {
            if (!accounts.TryGetValue(line.AccountNumber, out var account))
            {
                throw new NotFoundException($"Rekening '{line.AccountNumber}' bestaat niet.");
            }

            if (!account.IsActive)
            {
                throw new DomainException($"Rekening '{account.Number}' ({account.Name}) is niet actief.");
            }

            post.AddLine(account.Id, line.Debit, line.Credit, line.Description);
        }

        post.Validate();
        await journalEntryRepository.AddAsync(post, ct).ConfigureAwait(false);
        return post;
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
