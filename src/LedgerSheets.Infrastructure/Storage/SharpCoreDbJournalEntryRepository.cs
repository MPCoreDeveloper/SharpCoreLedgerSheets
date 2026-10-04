using LedgerSheets.Application.Abstractions;
using LedgerSheets.Domain.Accounting;
using SharpCoreDB.Interfaces;

namespace LedgerSheets.Infrastructure.Storage;

/// <summary>
/// SharpCoreDB-backed <see cref="IJournalEntryRepository"/>.
/// </summary>
/// <remarks>
/// A journal entry is stored as one row in <c>journalEntries</c> plus one row per line in
/// <c>journalEntry_lines</c>. The period filter is applied in memory: the volumes are small and
/// this keeps the query free of string-collation assumptions.
/// </remarks>
internal sealed class SharpCoreDbJournalEntryRepository(IDatabase database)
    : SharpCoreDbRepository(database), IJournalEntryRepository
{
    private const string PostColumns = "id, datum, omschrijving, referentie";

    private const string LineColumns =
        "id, post_id, volgnummer, rekening_id, debet, credit, omschrijving";

    /// <inheritdoc />
    public Task<IReadOnlyList<JournalEntry>> AllAsync(Period? period = null, CancellationToken ct = default)
    {
        var postRows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {PostColumns} FROM {LedgerSchema.TableEntries}"));

        var lineRows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {LineColumns} FROM {LedgerSchema.TableLines}"));

        var linesPerPost = lineRows
            .GroupBy(row => ReadGuid(row, "post_id"))
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(row => ReadInteger(row, "volgnummer")).ToList());

        var entries = new List<JournalEntry>(postRows.Count);

        foreach (var row in postRows)
        {
            var id = ReadGuid(row, "id");
            var date = ReadDate(row, "datum");

            if (period.HasValue && !period.Value.Contains(date))
            {
                continue;
            }

            var post = new JournalEntry(id, date, ReadText(row, "omschrijving"), ReadOptioneleText(row, "referentie"));

            if (linesPerPost.TryGetValue(id, out var lines))
            {
                foreach (var line in lines)
                {
                    post.AddLine(
                        ReadGuid(line, "rekening_id"),
                        ReadAmount(line, "debet"),
                        ReadAmount(line, "credit"),
                        ReadOptioneleText(line, "omschrijving"));
                }
            }

            entries.Add(post);
        }

        IReadOnlyList<JournalEntry> result = entries
            .OrderBy(post => post.Date)
            .ThenBy(post => post.Id)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task AddAsync(JournalEntry post, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        WithLock(() =>
        {
            Database.ExecuteSQL(
                $"INSERT INTO {LedgerSchema.TableEntries} ({PostColumns}) " +
                "VALUES (@id, @datum, @omschrijving, @referentie)",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@id"] = ToText(post.Id),
                    ["@datum"] = ToText(post.Date),
                    ["@omschrijving"] = post.Description,
                    ["@referentie"] = post.Reference ?? string.Empty,
                });

            var sequenceNumber = 0;

            foreach (var line in post.Lines)
            {
                Database.ExecuteSQL(
                    $"INSERT INTO {LedgerSchema.TableLines} ({LineColumns}) " +
                    "VALUES (@id, @post_id, @volgnummer, @rekening_id, @debet, @credit, @omschrijving)",
                    new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["@id"] = ToText(Guid.NewGuid()),
                        ["@post_id"] = ToText(post.Id),
                        ["@volgnummer"] = sequenceNumber++,
                        ["@rekening_id"] = ToText(line.AccountId),
                        ["@debet"] = line.Debit,
                        ["@credit"] = line.Credit,
                        ["@omschrijving"] = line.Description ?? string.Empty,
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
            Database.ExecuteSQL($"DELETE FROM {LedgerSchema.TableLines} WHERE post_id = @id", parameters);
            Database.ExecuteSQL($"DELETE FROM {LedgerSchema.TableEntries} WHERE id = @id", parameters);
            Database.Flush();
        });

        return Task.CompletedTask;
    }
}
