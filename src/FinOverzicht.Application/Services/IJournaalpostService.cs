using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Services;

/// <summary>
/// Use cases for posting and querying journal entries.
/// </summary>
public interface IJournaalpostService
{
    /// <summary>Returns all entries ordered by date, optionally restricted to a period.</summary>
    Task<IReadOnlyList<Journaalpost>> AlleAsync(Periode? periode = null, CancellationToken ct = default);

    /// <summary>Posts a new journal entry after resolving and validating the account numbers.</summary>
    Task<Journaalpost> BoekAsync(JournaalpostBoekenCommand command, CancellationToken ct = default);

    /// <summary>Removes a journal entry.</summary>
    Task VerwijderAsync(Guid id, CancellationToken ct = default);
}
