using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

/// <summary>
/// SharpCoreDB-backed <see cref="IAccountRepository"/>.
/// </summary>
internal sealed class SharpCoreDbAccountRepository(IDatabase database)
    : SharpCoreDbRepository(database), IAccountRepository
{
    private const string Columns =
        "id, number, name, type, is_active, is_current, is_cash_equivalent, is_inventory";

    /// <inheritdoc />
    public Task<IReadOnlyList<Account>> AllAsync(CancellationToken ct = default)
    {
        var rows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {Columns} FROM {LedgerSchema.TableAccounts}"));

        IReadOnlyList<Account> accounts = rows
            .Select(ToAccount)
            .OrderBy(r => r.Number, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(accounts);
    }

    /// <inheritdoc />
    public Task<Account?> FindOnNumberAsync(string number, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["@number"] = number,
        };

        var rows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {Columns} FROM {LedgerSchema.TableAccounts} WHERE number = @number", parameters));

        return Task.FromResult(rows.Count == 0 ? null : ToAccount(rows[0]));
    }

    /// <inheritdoc />
    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        WithLock(() =>
        {
            Database.ExecuteSQL(
                $"INSERT INTO {LedgerSchema.TableAccounts} ({Columns}) " +
                "VALUES (@id, @number, @name, @type, @is_active, @is_current, @is_cash_equivalent, @is_inventory)",
                Parameters(account));
            Database.Flush();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        WithLock(() =>
        {
            Database.ExecuteSQL(
                $"UPDATE {LedgerSchema.TableAccounts} SET number = @number, name = @name, type = @type, " +
                "is_active = @is_active, is_current = @is_current, is_cash_equivalent = @is_cash_equivalent, " +
                "is_inventory = @is_inventory WHERE id = @id",
                Parameters(account));
            Database.Flush();
        });

        return Task.CompletedTask;
    }

    private static Dictionary<string, object?> Parameters(Account account) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["@id"] = ToText(account.Id),
            ["@number"] = account.Number,
            ["@name"] = account.Name,
            ["@type"] = (int)account.Type,
            ["@is_active"] = account.IsActive ? 1 : 0,
            ["@is_current"] = account.IsCurrent ? 1 : 0,
            ["@is_cash_equivalent"] = account.IsCashEquivalent ? 1 : 0,
            ["@is_inventory"] = account.IsInventory ? 1 : 0,
        };

    private static Account ToAccount(IReadOnlyDictionary<string, object> row) =>
        new(
            ReadGuid(row, "id"),
            ReadText(row, "number"),
            ReadText(row, "name"),
            (AccountType)ReadInteger(row, "type"),
            ReadBool(row, "is_active"),
            ReadBool(row, "is_current"),
            ReadBool(row, "is_cash_equivalent"),
            ReadBool(row, "is_inventory"));
}
