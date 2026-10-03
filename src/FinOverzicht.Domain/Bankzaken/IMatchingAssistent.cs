using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Domain.Bankzaken;

/// <summary>
/// Proposes a counterparty account for an imported bank transaction.
/// </summary>
/// <remarks>
/// This is the "matching assistant" of the bank import: a pure, deterministic calculation over the
/// movement, the accounts and the rules learned from earlier bookings. It is a domain service
/// because it encodes bookkeeping judgement, not plumbing.
/// </remarks>
public interface IMatchingAssistent
{
    /// <summary>
    /// Returns the best proposal for the transaction, or <see langword="null"/> when nothing in the
    /// ledger points at a plausible counterparty account.
    /// </summary>
    /// <param name="transactie">The imported movement.</param>
    /// <param name="rekeningen">All accounts of the administration.</param>
    /// <param name="regels">The rules learned from earlier confirmed bookings.</param>
    /// <param name="bankRekeningNummer">
    /// The ledger number of the bank account the movement belongs to; it is never proposed as the
    /// counterparty. When omitted, <see cref="BankTransactie.BankRekeningNummer"/> is used.
    /// </param>
    Boekingsvoorstel? StelVoor(
        BankTransactie transactie,
        IReadOnlyList<Rekening> rekeningen,
        IReadOnlyList<TegenrekeningRegel> regels,
        string? bankRekeningNummer = null);
}
