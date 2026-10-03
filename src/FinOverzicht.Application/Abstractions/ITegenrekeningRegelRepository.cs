using FinOverzicht.Domain.Bankzaken;

namespace FinOverzicht.Application.Abstractions;

/// <summary>
/// Read/write port for the matching rules that were learned from confirmed bookings.
/// </summary>
public interface ITegenrekeningRegelRepository
{
    /// <summary>Returns all rules, most confirmed first.</summary>
    Task<IReadOnlyList<TegenrekeningRegel>> AlleAsync(CancellationToken ct = default);

    /// <summary>
    /// Stores a rule: adds it when the keyword is new, replaces the rule with the same keyword
    /// otherwise.
    /// </summary>
    Task BewaarAsync(TegenrekeningRegel regel, CancellationToken ct = default);
}
