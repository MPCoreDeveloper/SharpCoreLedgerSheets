using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Application.Abstractions;

/// <summary>
/// Read/write port for journal entries ("journalEntries").
/// </summary>
public interface IJournalEntryRepository
{
    /// <summary>
    /// Returns all posted entries ordered by date, optionally restricted to a period.
    /// </summary>
    Task<IReadOnlyList<JournalEntry>> AllAsync(Period? period = null, CancellationToken ct = default);

    /// <summary>Persists a new journal entry (including its lines).</summary>
    Task AddAsync(JournalEntry post, CancellationToken ct = default);

    /// <summary>Removes a journal entry and its lines.</summary>
    Task RemoveAsync(Guid id, CancellationToken ct = default);
}
