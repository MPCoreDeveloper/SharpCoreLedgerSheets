using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Accounting;

/// <summary>
/// A journal entry ("journalEntry"): a dated collection of balanced debit/credit lines.
/// </summary>
/// <remarks>
/// Entries are mutable while being composed (via <see cref="AddLine"/>) and validated by
/// <see cref="Ledger.Post"/>. Use <see cref="IsBalanced"/> and <see cref="Validate"/> when
/// you need to check an entry outside of the ledger.
/// </remarks>
public sealed class JournalEntry
{
    private readonly List<JournalEntryLine> _lines = [];

    /// <summary>Creates a new, empty journal entry.</summary>
    /// <exception cref="DomainException">Thrown when the id or description is missing.</exception>
    public JournalEntry(Guid id, DateOnly date, string description, string? reference = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Een journaalpost moet een uniek id hebben.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Een journaalpost moet een omschrijving hebben.");
        }

        Id = id;
        Date = date;
        Description = description.Trim();
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }

    /// <summary>Creates a new journal entry with a freshly generated identifier.</summary>
    public static JournalEntry New(DateOnly date, string description, string? reference = null) =>
        new(Guid.NewGuid(), date, description, reference);

    /// <summary>Unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>The booking date.</summary>
    public DateOnly Date { get; }

    /// <summary>The description of the entry.</summary>
    public string Description { get; }

    /// <summary>Optional external reference (invoice number, bank statement line, ...).</summary>
    public string? Reference { get; }

    /// <summary>The lines of this entry.</summary>
    public IReadOnlyList<JournalEntryLine> Lines => _lines.AsReadOnly();

    /// <summary>The total of all debit lines.</summary>
    public decimal TotalDebit => _lines.Sum(r => r.Debit);

    /// <summary>The total of all credit lines.</summary>
    public decimal TotalCredit => _lines.Sum(r => r.Credit);

    /// <summary>True when total debit equals total credit.</summary>
    public bool IsBalanced => TotalDebit == TotalCredit;

    /// <summary>Adds a debit or credit line and returns this entry for fluent composition.</summary>
    public JournalEntry AddLine(Guid accountId, decimal debit, decimal credit, string? description = null)
    {
        _lines.Add(new JournalEntryLine(accountId, debit, credit, description));
        return this;
    }

    /// <summary>Convenience overload: adds a debit line.</summary>
    public JournalEntry Debit(Guid accountId, decimal amount, string? description = null) =>
        AddLine(accountId, amount, 0m, description);

    /// <summary>Convenience overload: adds a credit line.</summary>
    public JournalEntry Credit(Guid accountId, decimal amount, string? description = null) =>
        AddLine(accountId, 0m, amount, description);

    /// <summary>
    /// Validates that the entry has at least two lines and is in balance.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the entry is not a valid journal entry.</exception>
    public void Validate()
    {
        if (_lines.Count < 2)
        {
            throw new DomainException($"Journaalpost '{Description}' moet minimaal twee regels hebben.");
        }

        if (!IsBalanced)
        {
            throw new DomainException(
                $"Journaalpost '{Description}' is niet in balans: debet {TotalDebit} versus credit {TotalCredit}.");
        }
    }
}
