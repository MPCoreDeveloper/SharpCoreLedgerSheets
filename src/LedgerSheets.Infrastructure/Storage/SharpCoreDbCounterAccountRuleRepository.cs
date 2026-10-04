using LedgerSheets.Application.Abstractions;
using LedgerSheets.Domain.Banking;
using SharpCoreDB.Interfaces;

namespace LedgerSheets.Infrastructure.Storage;

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
    private const string Columns = "trefwoord, tegenrekening, bevestigingen";

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
                $"DELETE FROM {BankStatementSchema.TableMatchRules} WHERE trefwoord = @trefwoord",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@trefwoord"] = line.Keyword,
                });

            Database.ExecuteSQL(
                $"INSERT INTO {BankStatementSchema.TableMatchRules} ({Columns}) " +
                "VALUES (@trefwoord, @tegenrekening, @bevestigingen)",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@trefwoord"] = line.Keyword,
                    ["@tegenrekening"] = line.CounterAccountNumber,
                    ["@bevestigingen"] = line.Confirmations,
                });

            Database.Flush();
        });

        return Task.CompletedTask;
    }

    private static CounterAccountRule ToLine(IReadOnlyDictionary<string, object> row) =>
        new(ReadText(row, "trefwoord"), ReadText(row, "tegenrekening"), ReadInteger(row, "bevestigingen"));
}
