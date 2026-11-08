using SharpCoreLedgerSheets.Domain.Banking;

namespace SharpCoreLedgerSheets.Application.Abstractions;

/// <summary>
/// Read/write port for the matching rules that were learned from confirmed bookings.
/// </summary>
public interface ICounterAccountRuleRepository
{
    /// <summary>Returns all rules, most confirmed first.</summary>
    Task<IReadOnlyList<CounterAccountRule>> AllAsync(CancellationToken ct = default);

    /// <summary>
    /// Stores a rule: adds it when the keyword is new, replaces the rule with the same keyword
    /// otherwise.
    /// </summary>
    Task SaveAsync(CounterAccountRule line, CancellationToken ct = default);
}
