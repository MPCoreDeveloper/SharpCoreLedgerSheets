using FinOverzicht.Domain.Bankzaken;

namespace FinOverzicht.Application.Abstractions;

/// <summary>
/// Read/write port for imported bank transactions.
/// </summary>
/// <remarks>
/// Importing the same statement twice must never duplicate a movement, which is why
/// <see cref="BestaandeSleutelsAsync"/> exists: the import compares the keys of the file with the
/// keys that are already stored.
/// </remarks>
public interface IBankstaatRepository
{
    /// <summary>Returns the transactions ordered by date, optionally restricted to one status.</summary>
    Task<IReadOnlyList<BankTransactie>> AlleAsync(
        BankTransactieStatus? status = null,
        CancellationToken ct = default);

    /// <summary>Finds a transaction by its identifier, or <see langword="null"/>.</summary>
    Task<BankTransactie?> ZoekAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the import keys (<see cref="BankTransactie.Sleutel"/>) of everything already stored.</summary>
    Task<IReadOnlyList<string>> BestaandeSleutelsAsync(CancellationToken ct = default);

    /// <summary>Persists new transactions.</summary>
    Task ToevoegenAsync(IEnumerable<BankTransactie> transacties, CancellationToken ct = default);

    /// <summary>Persists a changed transaction, for example after booking or ignoring it.</summary>
    Task BijwerkenAsync(BankTransactie transactie, CancellationToken ct = default);
}
