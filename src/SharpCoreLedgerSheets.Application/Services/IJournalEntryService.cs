using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Application.Services;

/// <summary>
/// Use cases for posting and querying journal entries.
/// </summary>
public interface IJournalEntryService
{
    /// <summary>Returns all entries ordered by date, optionally restricted to a period.</summary>
    Task<IReadOnlyList<JournalEntry>> AllAsync(Period? period = null, CancellationToken ct = default);

    /// <summary>Posts a new journal entry after resolving and validating the account numbers.</summary>
    Task<JournalEntry> PostAsync(PostJournalEntryCommand command, CancellationToken ct = default);

    /// <summary>Removes a journal entry.</summary>
    Task RemoveAsync(Guid id, CancellationToken ct = default);
}
