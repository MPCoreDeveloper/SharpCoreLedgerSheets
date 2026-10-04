using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Banking;

/// <summary>
/// The matching assistant's suggestion for an imported bank transaction: which counterparty account
/// the movement most likely belongs on, how sure the assistant is, and why.
/// </summary>
public sealed record PostingProposal
{
    /// <summary>The certainty (0-100) from which the assistant considers a proposal reliable.</summary>
    public const int ConfidenceThreshold = 90;

    /// <summary>Creates a proposal.</summary>
    /// <exception cref="DomainException">Thrown when the proposal is incomplete.</exception>
    public PostingProposal(
        Guid transactionId,
        string counterAccountNumber,
        string counterAccountName,
        int confidence,
        string reason)
    {
        if (transactionId == Guid.Empty)
        {
            throw new DomainException("Een boekingsvoorstel hoort bij een banktransactie.");
        }

        if (string.IsNullOrWhiteSpace(counterAccountNumber))
        {
            throw new DomainException("Een boekingsvoorstel moet een tegenrekening hebben.");
        }

        if (string.IsNullOrWhiteSpace(counterAccountName))
        {
            throw new DomainException("Een boekingsvoorstel moet de naam van de tegenrekening hebben.");
        }

        if (confidence is < 0 or > 100)
        {
            throw new DomainException("De zekerheid van een boekingsvoorstel ligt tussen 0 en 100.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Een boekingsvoorstel moet uitleggen waarom het wordt gedaan.");
        }

        TransactionId = transactionId;
        CounterAccountNumber = counterAccountNumber.Trim();
        CounterAccountName = counterAccountName.Trim();
        Confidence = confidence;
        Reason = reason.Trim();
    }

    /// <summary>The transaction this proposal is about.</summary>
    public Guid TransactionId { get; }

    /// <summary>The ledger number of the proposed counterparty account.</summary>
    public string CounterAccountNumber { get; }

    /// <summary>The name of the proposed counterparty account.</summary>
    public string CounterAccountName { get; }

    /// <summary>How sure the assistant is, from 0 (pure guess) to 100 (learned rule on the IBAN).</summary>
    public int Confidence { get; }

    /// <summary>The reason, in the user's language, so the suggestion can be judged.</summary>
    public string Reason { get; }

    /// <summary>True when the assistant is sure enough for the proposal to be pre-selected.</summary>
    public bool IsReliable => Confidence >= ConfidenceThreshold;
}
