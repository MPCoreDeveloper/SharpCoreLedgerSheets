using LedgerSheets.Application.Abstractions;
using LedgerSheets.Domain.Accounting;
using SharpCoreDB.Interfaces;

namespace LedgerSheets.Infrastructure.Storage;

/// <summary>
/// SharpCoreDB-backed <see cref="IAccountRepository"/>.
/// </summary>
internal sealed class SharpCoreDbAccountRepository(IDatabase database)
    : SharpCoreDbRepository(database), IAccountRepository
{
    private const string Columns =
        "id, nummer, naam, soort, is_actief, is_vlottend, is_liquide_middel, is_voorraad";

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
            ["@nummer"] = number,
        };

        var rows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {Columns} FROM {LedgerSchema.TableAccounts} WHERE nummer = @nummer", parameters));

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
                "VALUES (@id, @nummer, @naam, @soort, @is_actief, @is_vlottend, @is_liquide_middel, @is_voorraad)",
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
                $"UPDATE {LedgerSchema.TableAccounts} SET nummer = @nummer, naam = @naam, soort = @soort, " +
                "is_actief = @is_actief, is_vlottend = @is_vlottend, is_liquide_middel = @is_liquide_middel, " +
                "is_voorraad = @is_voorraad WHERE id = @id",
                Parameters(account));
            Database.Flush();
        });

        return Task.CompletedTask;
    }

    private static Dictionary<string, object?> Parameters(Account account) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["@id"] = ToText(account.Id),
            ["@nummer"] = account.Number,
            ["@naam"] = account.Name,
            ["@soort"] = (int)account.Type,
            ["@is_actief"] = account.IsActive ? 1 : 0,
            ["@is_vlottend"] = account.IsCurrent ? 1 : 0,
            ["@is_liquide_middel"] = account.IsCashEquivalent ? 1 : 0,
            ["@is_voorraad"] = account.IsInventory ? 1 : 0,
        };

    private static Account ToAccount(IReadOnlyDictionary<string, object> row) =>
        new(
            ReadGuid(row, "id"),
            ReadText(row, "nummer"),
            ReadText(row, "naam"),
            (AccountType)ReadInteger(row, "soort"),
            ReadBool(row, "is_actief"),
            ReadBool(row, "is_vlottend"),
            ReadBool(row, "is_liquide_middel"),
            ReadBool(row, "is_voorraad"));
}
