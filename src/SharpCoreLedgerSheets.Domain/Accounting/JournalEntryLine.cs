using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Domain.Accounting;

/// <summary>
/// A single debit or credit line of a <see cref="JournalEntry"/>.
/// </summary>
/// <remarks>
/// A line is either a debit or a credit, never both and never neither. The signed
/// <see cref="Movement"/> is the value the line adds to the account balance
/// (positive = debit, negative = credit).
/// </remarks>
public sealed record JournalEntryLine
{
    /// <summary>Creates a journal entry line.</summary>
    /// <exception cref="DomainException">Thrown when the line is invalid.</exception>
    public JournalEntryLine(Guid accountId, decimal debit, decimal credit, string? description = null)
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainException(MessageCode.LineAccountRequired, "Een boekingsregel moet naar een rekening verwijzen.");
        }

        if (debit < 0m || credit < 0m)
        {
            throw new DomainException(MessageCode.LineNegativeAmount, "Debet en credit mogen niet negatief zijn.");
        }

        if (debit > 0m && credit > 0m)
        {
            throw new DomainException(MessageCode.LineDebitAndCredit, "Een boekingsregel mag niet zowel debet als credit bevatten.");
        }

        if (debit == 0m && credit == 0m)
        {
            throw new DomainException(MessageCode.LineAmountRequired, "Een boekingsregel moet een debet- of creditbedrag hebben.");
        }

        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>The account this line posts to.</summary>
    public Guid AccountId { get; }

    /// <summary>The debit amount (0 when this is a credit line).</summary>
    public decimal Debit { get; }

    /// <summary>The credit amount (0 when this is a debit line).</summary>
    public decimal Credit { get; }

    /// <summary>Optional line description.</summary>
    public string? Description { get; }

    /// <summary>The signed movement added to the account balance (positive = debit).</summary>
    public decimal Movement => Debit - Credit;
}
