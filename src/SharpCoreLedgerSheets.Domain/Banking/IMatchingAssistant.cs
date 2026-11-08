using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Domain.Banking;

/// <summary>
/// Proposes a counterparty account for an imported bank transaction.
/// </summary>
/// <remarks>
/// This is the "matching assistant" of the bank import: a pure, deterministic calculation over the
/// movement, the accounts and the rules learned from earlier bookings. It is a domain service
/// because it encodes bookkeeping judgement, not plumbing.
/// </remarks>
public interface IMatchingAssistant
{
    /// <summary>
    /// Returns the best proposal for the transaction, or <see langword="null"/> when nothing in the
    /// ledger points at a plausible counterparty account.
    /// </summary>
    /// <param name="transaction">The imported movement.</param>
    /// <param name="accounts">All accounts of the administration.</param>
    /// <param name="lines">The rules learned from earlier confirmed bookings.</param>
    /// <param name="bankAccountNumber">
    /// The ledger number of the bank account the movement belongs to; it is never proposed as the
    /// counterparty. When omitted, <see cref="BankTransaction.BankAccountNumber"/> is used.
    /// </param>
    PostingProposal? Propose(
        BankTransaction transaction,
        IReadOnlyList<Account> accounts,
        IReadOnlyList<CounterAccountRule> lines,
        string? bankAccountNumber = null);
}
