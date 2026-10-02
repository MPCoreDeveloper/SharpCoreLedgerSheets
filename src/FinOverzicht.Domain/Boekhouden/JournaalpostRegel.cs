using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Boekhouden;

/// <summary>
/// A single debit or credit line of a <see cref="Journaalpost"/>.
/// </summary>
/// <remarks>
/// A line is either a debit or a credit, never both and never neither. The signed
/// <see cref="Mutatie"/> is the value the line adds to the account balance
/// (positive = debit, negative = credit).
/// </remarks>
public sealed record JournaalpostRegel
{
    /// <summary>Creates a journal entry line.</summary>
    /// <exception cref="DomeinException">Thrown when the line is invalid.</exception>
    public JournaalpostRegel(Guid rekeningId, decimal debet, decimal credit, string? omschrijving = null)
    {
        if (rekeningId == Guid.Empty)
        {
            throw new DomeinException("Een boekingsregel moet naar een rekening verwijzen.");
        }

        if (debet < 0m || credit < 0m)
        {
            throw new DomeinException("Debet en credit mogen niet negatief zijn.");
        }

        if (debet > 0m && credit > 0m)
        {
            throw new DomeinException("Een boekingsregel mag niet zowel debet als credit bevatten.");
        }

        if (debet == 0m && credit == 0m)
        {
            throw new DomeinException("Een boekingsregel moet een debet- of creditbedrag hebben.");
        }

        RekeningId = rekeningId;
        Debet = debet;
        Credit = credit;
        Omschrijving = string.IsNullOrWhiteSpace(omschrijving) ? null : omschrijving.Trim();
    }

    /// <summary>The account this line posts to.</summary>
    public Guid RekeningId { get; }

    /// <summary>The debit amount (0 when this is a credit line).</summary>
    public decimal Debet { get; }

    /// <summary>The credit amount (0 when this is a debit line).</summary>
    public decimal Credit { get; }

    /// <summary>Optional line description.</summary>
    public string? Omschrijving { get; }

    /// <summary>The signed movement added to the account balance (positive = debit).</summary>
    public decimal Mutatie => Debet - Credit;
}
