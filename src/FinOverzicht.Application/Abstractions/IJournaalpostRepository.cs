using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Abstractions;

/// <summary>
/// Read/write port for journal entries ("journaalposten").
/// </summary>
public interface IJournaalpostRepository
{
    /// <summary>
    /// Returns all posted entries ordered by date, optionally restricted to a period.
    /// </summary>
    Task<IReadOnlyList<Journaalpost>> AlleAsync(Periode? periode = null, CancellationToken ct = default);

    /// <summary>Persists a new journal entry (including its lines).</summary>
    Task ToevoegenAsync(Journaalpost post, CancellationToken ct = default);

    /// <summary>Removes a journal entry and its lines.</summary>
    Task VerwijderenAsync(Guid id, CancellationToken ct = default);
}
