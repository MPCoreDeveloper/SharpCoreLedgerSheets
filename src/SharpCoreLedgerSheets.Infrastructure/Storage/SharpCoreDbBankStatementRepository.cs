using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

/// <summary>
/// SharpCoreDB-backed <see cref="IBankStatementRepository"/>.
/// </summary>
/// <remarks>
/// A stored movement only ever changes status (open, booked or ignored) and the journal entry it was
/// booked with, so <see cref="UpdateAsync"/> writes just those two columns.
/// </remarks>
internal sealed class SharpCoreDbBankStatementRepository(IDatabase database)
    : SharpCoreDbRepository(database), IBankStatementRepository
{
    private const string Columns =
        "id, bank_account, date, amount, currency, reference, counterparty, iban, description, status, entry_id";

    /// <inheritdoc />
    public Task<IReadOnlyList<BankTransaction>> AllAsync(
        BankTransactionStatus? status = null,
        CancellationToken ct = default)
    {
        var rows = status is null
            ? WithLock(() => Database.ExecuteQuery(
                $"SELECT {Columns} FROM {BankStatementSchema.TableMovements}"))
            : WithLock(() => Database.ExecuteQuery(
                $"SELECT {Columns} FROM {BankStatementSchema.TableMovements} WHERE status = @status",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@status"] = (int)status.Value,
                }));

        IReadOnlyList<BankTransaction> transactions = rows
            .Select(ToTransaction)
            .OrderBy(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Reference, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(transactions);
    }

    /// <inheritdoc />
    public Task<BankTransaction?> FindAsync(Guid id, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["@id"] = ToText(id),
        };

        var rows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {Columns} FROM {BankStatementSchema.TableMovements} WHERE id = @id", parameters));

        return Task.FromResult(rows.Count == 0 ? null : ToTransaction(rows[0]));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ExistingKeysAsync(CancellationToken ct = default)
    {
        var rows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {Columns} FROM {BankStatementSchema.TableMovements}"));

        // The key is derived from the movement itself, so reading the rows keeps the normalisation in
        // one place instead of duplicating it here.
        IReadOnlyList<string> keys = rows
            .Select(row => ToTransaction(row).Key)
            .ToList();

        return Task.FromResult(keys);
    }

    /// <inheritdoc />
    public Task AddAsync(IEnumerable<BankTransaction> transactions, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        var added = transactions.ToList();

        if (added.Count == 0)
        {
            return Task.CompletedTask;
        }

        WithLock(() =>
        {
            foreach (var transaction in added)
            {
                Database.ExecuteSQL(
                    $"INSERT INTO {BankStatementSchema.TableMovements} ({Columns}) VALUES (@id, @bank_account, " +
                    "@date, @amount, @currency, @reference, @counterparty, @iban, @description, @status, @entry_id)",
                    Parameters(transaction));
            }

            // A single flush: an import writes a whole statement in one go.
            Database.Flush();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(BankTransaction transaction, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        WithLock(() =>
        {
            Database.ExecuteSQL(
                $"UPDATE {BankStatementSchema.TableMovements} SET status = @status, entry_id = @entry_id WHERE id = @id",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@id"] = ToText(transaction.Id),
                    ["@status"] = (int)transaction.Status,
                    ["@entry_id"] = transaction.JournalEntryId is { } entry ? ToText(entry) : null,
                });
            Database.Flush();
        });

        return Task.CompletedTask;
    }

    private static Dictionary<string, object?> Parameters(BankTransaction transaction) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["@id"] = ToText(transaction.Id),
            ["@bank_account"] = transaction.BankAccountNumber,
            ["@date"] = ToText(transaction.Date),
            ["@amount"] = transaction.Movement.Value,
            ["@currency"] = transaction.Movement.Currency.Code,
            ["@reference"] = transaction.Reference,
            ["@counterparty"] = transaction.CounterpartyName,
            ["@iban"] = transaction.CounterpartyIban,
            ["@description"] = transaction.Description,
            ["@status"] = (int)transaction.Status,
            ["@entry_id"] = transaction.JournalEntryId is { } entry ? ToText(entry) : null,
        };

    private static BankTransaction ToTransaction(IReadOnlyDictionary<string, object> row)
    {
        var transaction = new BankTransaction(
            ReadGuid(row, "id"),
            ReadText(row, "bank_account"),
            ReadDate(row, "date"),
            new Amount(ReadAmount(row, "amount"), Currency.FromCode(ReadText(row, "currency"))),
            ReadText(row, "reference"),
            ReadOptionalText(row, "counterparty"),
            ReadOptionalText(row, "iban"),
            ReadOptionalText(row, "description"));

        var postId = ReadGuid(row, "entry_id");

        return (BankTransactionStatus)ReadInteger(row, "status") switch
        {
            BankTransactionStatus.Posted when postId != Guid.Empty => transaction.MarkPosted(postId),
            BankTransactionStatus.Ignored => transaction.Ignore(),
            _ => transaction,
        };
    }
}
