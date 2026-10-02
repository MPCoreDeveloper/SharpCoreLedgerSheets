using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Services;

/// <summary>
/// Use cases for managing general-ledger accounts.
/// </summary>
public interface IRekeningService
{
    /// <summary>Returns all accounts ordered by number.</summary>
    Task<IReadOnlyList<Rekening>> AlleAsync(CancellationToken ct = default);

    /// <summary>Creates a new account.</summary>
    /// <exception cref="Domain.Common.DomeinException">Thrown when the number already exists.</exception>
    Task<Rekening> VoegToeAsync(RekeningToevoegenCommand command, CancellationToken ct = default);

    /// <summary>Renames an account.</summary>
    Task HernoemAsync(string nummer, string nieuweNaam, CancellationToken ct = default);

    /// <summary>Deactivates an account so it can no longer receive new entries.</summary>
    Task DeactiveerAsync(string nummer, CancellationToken ct = default);
}
