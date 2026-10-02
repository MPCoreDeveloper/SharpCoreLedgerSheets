using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Boekhouden;

/// <summary>
/// A journal entry ("journaalpost"): a dated collection of balanced debit/credit lines.
/// </summary>
/// <remarks>
/// Entries are mutable while being composed (via <see cref="VoegRegelToe"/>) and validated by
/// <see cref="Grootboek.Boek"/>. Use <see cref="IsInBalans"/> and <see cref="Valideer"/> when
/// you need to check an entry outside of the ledger.
/// </remarks>
public sealed class Journaalpost
{
    private readonly List<JournaalpostRegel> _regels = [];

    /// <summary>Creates a new, empty journal entry.</summary>
    /// <exception cref="DomeinException">Thrown when the id or description is missing.</exception>
    public Journaalpost(Guid id, DateOnly datum, string omschrijving, string? referentie = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomeinException("Een journaalpost moet een uniek id hebben.");
        }

        if (string.IsNullOrWhiteSpace(omschrijving))
        {
            throw new DomeinException("Een journaalpost moet een omschrijving hebben.");
        }

        Id = id;
        Datum = datum;
        Omschrijving = omschrijving.Trim();
        Referentie = string.IsNullOrWhiteSpace(referentie) ? null : referentie.Trim();
    }

    /// <summary>Creates a new journal entry with a freshly generated identifier.</summary>
    public static Journaalpost Nieuw(DateOnly datum, string omschrijving, string? referentie = null) =>
        new(Guid.NewGuid(), datum, omschrijving, referentie);

    /// <summary>Unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>The booking date.</summary>
    public DateOnly Datum { get; }

    /// <summary>The description of the entry.</summary>
    public string Omschrijving { get; }

    /// <summary>Optional external reference (invoice number, bank statement line, ...).</summary>
    public string? Referentie { get; }

    /// <summary>The lines of this entry.</summary>
    public IReadOnlyList<JournaalpostRegel> Regels => _regels.AsReadOnly();

    /// <summary>The total of all debit lines.</summary>
    public decimal TotaalDebet => _regels.Sum(r => r.Debet);

    /// <summary>The total of all credit lines.</summary>
    public decimal TotaalCredit => _regels.Sum(r => r.Credit);

    /// <summary>True when total debit equals total credit.</summary>
    public bool IsInBalans => TotaalDebet == TotaalCredit;

    /// <summary>Adds a debit or credit line and returns this entry for fluent composition.</summary>
    public Journaalpost VoegRegelToe(Guid rekeningId, decimal debet, decimal credit, string? omschrijving = null)
    {
        _regels.Add(new JournaalpostRegel(rekeningId, debet, credit, omschrijving));
        return this;
    }

    /// <summary>Convenience overload: adds a debit line.</summary>
    public Journaalpost Debet(Guid rekeningId, decimal bedrag, string? omschrijving = null) =>
        VoegRegelToe(rekeningId, bedrag, 0m, omschrijving);

    /// <summary>Convenience overload: adds a credit line.</summary>
    public Journaalpost Credit(Guid rekeningId, decimal bedrag, string? omschrijving = null) =>
        VoegRegelToe(rekeningId, 0m, bedrag, omschrijving);

    /// <summary>
    /// Validates that the entry has at least two lines and is in balance.
    /// </summary>
    /// <exception cref="DomeinException">Thrown when the entry is not a valid journal entry.</exception>
    public void Valideer()
    {
        if (_regels.Count < 2)
        {
            throw new DomeinException($"Journaalpost '{Omschrijving}' moet minimaal twee regels hebben.");
        }

        if (!IsInBalans)
        {
            throw new DomeinException(
                $"Journaalpost '{Omschrijving}' is niet in balans: debet {TotaalDebet} versus credit {TotaalCredit}.");
        }
    }
}
