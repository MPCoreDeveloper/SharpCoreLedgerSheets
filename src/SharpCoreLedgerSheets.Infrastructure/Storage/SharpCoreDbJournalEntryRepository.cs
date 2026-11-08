using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

/// <summary>
/// SharpCoreDB-backed <see cref="IJournalEntryRepository"/>.
/// </summary>
/// <remarks>
/// A journal entry is stored as one row in <c>entries</c> plus one row per line in
/// <c>entry_lines</c>. The period filter is applied in memory: the volumes are small and
/// this keeps the query free of string-collation assumptions.
/// </remarks>
internal sealed class SharpCoreDbJournalEntryRepository(IDatabase database)
    : SharpCoreDbRepository(database), IJournalEntryRepository
{
    private const string EntryColumns = "id, date, description, reference";

    private const string LineColumns =
        "id, entry_id, sequence, account_id, debit, credit, description";

    /// <inheritdoc />
    public Task<IReadOnlyList<JournalEntry>> AllAsync(Period? period = null, CancellationToken ct = default)
    {
        var entryRows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {EntryColumns} FROM {LedgerSchema.TableEntries}"));

        var lineRows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {LineColumns} FROM {LedgerSchema.TableLines}"));

        var linesPerEntry = lineRows
            .GroupBy(row => ReadGuid(row, "entry_id"))
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(row => ReadInteger(row, "sequence")).ToList());

        var entries = new List<JournalEntry>(entryRows.Count);

        foreach (var row in entryRows)
        {
            var id = ReadGuid(row, "id");
            var date = ReadDate(row, "date");

            if (period.HasValue && !period.Value.Contains(date))
            {
                continue;
            }

            var entry = new JournalEntry(id, date, ReadText(row, "description"), ReadOptionalText(row, "reference"));

            if (linesPerEntry.TryGetValue(id, out var lines))
            {
                foreach (var line in lines)
                {
                    entry.AddLine(
                        ReadGuid(line, "account_id"),
                        ReadAmount(line, "debit"),
                        ReadAmount(line, "credit"),
                        ReadOptionalText(line, "description"));
                }
            }

            entries.Add(entry);
        }

        IReadOnlyList<JournalEntry> result = entries
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.Id)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task AddAsync(JournalEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        WithLock(() =>
        {
            Database.ExecuteSQL(
                $"INSERT INTO {LedgerSchema.TableEntries} ({EntryColumns}) " +
                "VALUES (@id, @date, @description, @reference)",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@id"] = ToText(entry.Id),
                    ["@date"] = ToText(entry.Date),
                    ["@description"] = entry.Description,
                    ["@reference"] = entry.Reference ?? string.Empty,
                });

            var sequenceNumber = 0;

            foreach (var line in entry.Lines)
            {
                Database.ExecuteSQL(
                    $"INSERT INTO {LedgerSchema.TableLines} ({LineColumns}) " +
                    "VALUES (@id, @entry_id, @sequence, @account_id, @debit, @credit, @description)",
                    new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["@id"] = ToText(Guid.NewGuid()),
                        ["@entry_id"] = ToText(entry.Id),
                        ["@sequence"] = sequenceNumber++,
                        ["@account_id"] = ToText(line.AccountId),
                        ["@debit"] = line.Debit,
                        ["@credit"] = line.Credit,
                        ["@description"] = line.Description ?? string.Empty,
                    });
            }

            Database.Flush();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["@id"] = ToText(id),
        };

        WithLock(() =>
        {
            Database.ExecuteSQL($"DELETE FROM {LedgerSchema.TableLines} WHERE entry_id = @id", parameters);
            Database.ExecuteSQL($"DELETE FROM {LedgerSchema.TableEntries} WHERE id = @id", parameters);
            Database.Flush();
        });

        return Task.CompletedTask;
    }
}
