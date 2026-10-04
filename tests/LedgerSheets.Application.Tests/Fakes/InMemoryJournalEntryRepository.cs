using LedgerSheets.Application.Abstractions;
using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Application.Tests.Fakes;

/// <summary>In-memory <see cref="IJournalEntryRepository"/> used by the application tests.</summary>
internal sealed class InMemoryJournalEntryRepository : IJournalEntryRepository
{
    private readonly Dictionary<Guid, JournalEntry> _entries = [];

    public Task<IReadOnlyList<JournalEntry>> AllAsync(Period? period = null, CancellationToken ct = default)
    {
        IEnumerable<JournalEntry> query = _entries.Values;

        if (period.HasValue)
        {
            var threshold = period.Value;
            query = query.Where(post => threshold.Contains(post.Date));
        }

        return Task.FromResult<IReadOnlyList<JournalEntry>>(query.OrderBy(p => p.Date).ToList());
    }

    public Task AddAsync(JournalEntry post, CancellationToken ct = default)
    {
        _entries[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        _entries.Remove(id);
        return Task.CompletedTask;
    }
}
