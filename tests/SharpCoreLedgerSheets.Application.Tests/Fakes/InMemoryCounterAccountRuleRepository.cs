using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Banking;

namespace SharpCoreLedgerSheets.Application.Tests.Fakes;

/// <summary>In-memory <see cref="ICounterAccountRuleRepository"/> used by the application tests.</summary>
internal sealed class InMemoryCounterAccountRuleRepository : ICounterAccountRuleRepository
{
    private readonly Dictionary<string, CounterAccountRule> _lines = new(StringComparer.Ordinal);

    /// <summary>How often the rules were read; proves that a batch proposal reads them once.</summary>
    public int CountTimesRead { get; private set; }

    public Task<IReadOnlyList<CounterAccountRule>> AllAsync(CancellationToken ct = default)
    {
        CountTimesRead++;

        return Task.FromResult<IReadOnlyList<CounterAccountRule>>(
            _lines.Values.OrderByDescending(line => line.Confirmations)
                .ThenBy(line => line.Keyword, StringComparer.Ordinal)
                .ToList());
    }

    public Task SaveAsync(CounterAccountRule line, CancellationToken ct = default)
    {
        _lines[line.Keyword] = line;
        return Task.CompletedTask;
    }
}
