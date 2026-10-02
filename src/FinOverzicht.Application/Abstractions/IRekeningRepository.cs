using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Abstractions;

/// <summary>
/// Read/write port for the general-ledger accounts ("grootboekrekeningen").
/// </summary>
/// <remarks>
/// A ledger owns its storage; there is exactly one administration per database, which keeps the
/// contract simple for the target audience (small businesses and individuals).
/// </remarks>
public interface IRekeningRepository
{
    /// <summary>Returns all accounts ordered by number.</summary>
    Task<IReadOnlyList<Rekening>> AlleAsync(CancellationToken ct = default);

    /// <summary>Finds an account by its ledger number (case-insensitive), or <see langword="null"/>.</summary>
    Task<Rekening?> ZoekOpNummerAsync(string nummer, CancellationToken ct = default);

    /// <summary>Persists a new account.</summary>
    Task ToevoegenAsync(Rekening rekening, CancellationToken ct = default);

    /// <summary>Persists changes to an existing account.</summary>
    Task BijwerkenAsync(Rekening rekening, CancellationToken ct = default);
}
