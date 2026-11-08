using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Domain.Banking;

/// <summary>
/// A single movement on a bank account, as read from a bank statement file.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Movement"/> uses the account holder's point of view: a positive amount is money
/// received (the bank credits the account), a negative amount is money paid.
/// </para>
/// <para>
/// <see cref="Reference"/> is the bank's own identifier for the movement and together with
/// <see cref="BankAccountNumber"/> it forms <see cref="Key"/>. That key is what makes an
/// import idempotent: importing the same statement twice must never duplicate a movement.
/// </para>
/// </remarks>
public sealed record BankTransaction
{
    /// <summary>Creates an imported bank transaction.</summary>
    /// <exception cref="DomainException">Thrown when the movement cannot be used as a bank movement.</exception>
    public BankTransaction(
        Guid id,
        string bankAccountNumber,
        DateOnly date,
        Amount movement,
        string reference,
        string? counterpartyName = null,
        string? counterpartyIban = null,
        string? description = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException(MessageCode.TransactionIdRequired, "Een banktransactie moet een uniek id hebben.");
        }

        if (string.IsNullOrWhiteSpace(bankAccountNumber))
        {
            throw new DomainException(MessageCode.TransactionBankAccountRequired, "Een banktransactie moet horen bij een grootboekrekening van de bank.");
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException(MessageCode.TransactionReferenceRequired, "Een banktransactie moet de referentie van de bank hebben.");
        }

        if (movement.IsNul)
        {
            throw new DomainException(MessageCode.TransactionAmountRequired, "Een banktransactie zonder bedrag kan niet worden geboekt.");
        }

        Id = id;
        BankAccountNumber = bankAccountNumber.Trim();
        Date = date;
        Movement = movement;
        Reference = reference.Trim();
        CounterpartyName = Clean(counterpartyName);
        CounterpartyIban = Clean(counterpartyIban);
        Description = Clean(description);
        Status = BankTransactionStatus.Open;
    }

    /// <summary>Unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>The ledger number of the bank account this movement belongs to.</summary>
    public string BankAccountNumber { get; }

    /// <summary>The (value) date of the movement.</summary>
    public DateOnly Date { get; }

    /// <summary>The signed movement: positive is received, negative is paid.</summary>
    public Amount Movement { get; }

    /// <summary>The bank's identifier for the movement.</summary>
    public string Reference { get; }

    /// <summary>The name of the counterparty, when the statement provides one.</summary>
    public string? CounterpartyName { get; }

    /// <summary>The IBAN of the counterparty, when the statement provides one.</summary>
    public string? CounterpartyIban { get; }

    /// <summary>The free-text description or remittance information.</summary>
    public string? Description { get; }

    /// <summary>True when money was received on the account.</summary>
    public bool IsCredit => Movement.IsPositive;

    /// <summary>True when money was paid from the account.</summary>
    public bool IsDepreciation => Movement.IsNegative;

    /// <summary>The absolute amount, used for the journal entry and for display.</summary>
    public Amount AmountWithoutSign => new(Math.Abs(Movement.Value), Movement.Currency);

    /// <summary>The import key: the bank account plus the bank's reference.</summary>
    public string Key => TextNormalization.Key(BankAccountNumber, Reference);

    /// <summary>The normalised text the matching assistant looks for keywords in.</summary>
    public string SearchText => TextNormalization.Normalize(CounterpartyName, CounterpartyIban, Description);

    /// <summary>What has happened with this transaction so far.</summary>
    public BankTransactionStatus Status { get; private init; }

    /// <summary>The journal entry that was created from this transaction, when it is booked.</summary>
    public Guid? JournalEntryId { get; private init; }

    /// <summary>Returns this transaction marked as booked by the given journal entry.</summary>
    /// <exception cref="DomainException">Thrown when the journal entry identifier is missing.</exception>
    public BankTransaction MarkPosted(Guid journalEntryId)
    {
        if (journalEntryId == Guid.Empty)
        {
            throw new DomainException(MessageCode.TransactionEntryRequired, "Een geboekte banktransactie moet naar een journaalpost verwijzen.");
        }

        return this with { Status = BankTransactionStatus.Posted, JournalEntryId = journalEntryId };
    }

    /// <summary>Returns this transaction marked as deliberately ignored.</summary>
    public BankTransaction Ignore() =>
        this with { Status = BankTransactionStatus.Ignored, JournalEntryId = null };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
