using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

/// <summary>
/// SharpCoreDB-backed <see cref="ICounterAccountRuleRepository"/>.
/// </summary>
/// <remarks>
/// A keyword identifies a rule, so storing one replaces the row with that keyword: an exception such
/// as "the rule for this keyword now points at another account" can never leave two conflicting rules
/// behind.
/// </remarks>
internal sealed class SharpCoreDbCounterAccountRuleRepository(IDatabase database)
    : SharpCoreDbRepository(database), ICounterAccountRuleRepository
{
    private const string Columns = "keyword, counter_account, confirmations";

    /// <inheritdoc />
    public Task<IReadOnlyList<CounterAccountRule>> AllAsync(CancellationToken ct = default)
    {
        var rows = WithLock(() => Database.ExecuteQuery(
            $"SELECT {Columns} FROM {BankStatementSchema.TableMatchRules}"));

        IReadOnlyList<CounterAccountRule> lines = rows
            .Select(ToLine)
            .OrderByDescending(line => line.Confirmations)
            .ThenBy(line => line.Keyword, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(lines);
    }

    /// <inheritdoc />
    public Task SaveAsync(CounterAccountRule line, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(line);

        WithLock(() =>
        {
            Database.ExecuteSQL(
                $"DELETE FROM {BankStatementSchema.TableMatchRules} WHERE keyword = @keyword",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@keyword"] = line.Keyword,
                });

            Database.ExecuteSQL(
                $"INSERT INTO {BankStatementSchema.TableMatchRules} ({Columns}) " +
                "VALUES (@keyword, @counter_account, @confirmations)",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@keyword"] = line.Keyword,
                    ["@counter_account"] = line.CounterAccountNumber,
                    ["@confirmations"] = line.Confirmations,
                });

            Database.Flush();
        });

        return Task.CompletedTask;
    }

    private static CounterAccountRule ToLine(IReadOnlyDictionary<string, object> row) =>
        new(ReadText(row, "keyword"), ReadText(row, "counter_account"), ReadInteger(row, "confirmations"));
}
