using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Bankzaken;

/// <summary>
/// The matching assistant's suggestion for an imported bank transaction: which counterparty account
/// the movement most likely belongs on, how sure the assistant is, and why.
/// </summary>
public sealed record Boekingsvoorstel
{
    /// <summary>The certainty (0-100) from which the assistant considers a proposal reliable.</summary>
    public const int Betrouwbaarheidsgrens = 90;

    /// <summary>Creates a proposal.</summary>
    /// <exception cref="DomeinException">Thrown when the proposal is incomplete.</exception>
    public Boekingsvoorstel(
        Guid transactieId,
        string tegenrekeningNummer,
        string tegenrekeningNaam,
        int zekerheid,
        string reden)
    {
        if (transactieId == Guid.Empty)
        {
            throw new DomeinException("Een boekingsvoorstel hoort bij een banktransactie.");
        }

        if (string.IsNullOrWhiteSpace(tegenrekeningNummer))
        {
            throw new DomeinException("Een boekingsvoorstel moet een tegenrekening hebben.");
        }

        if (string.IsNullOrWhiteSpace(tegenrekeningNaam))
        {
            throw new DomeinException("Een boekingsvoorstel moet de naam van de tegenrekening hebben.");
        }

        if (zekerheid is < 0 or > 100)
        {
            throw new DomeinException("De zekerheid van een boekingsvoorstel ligt tussen 0 en 100.");
        }

        if (string.IsNullOrWhiteSpace(reden))
        {
            throw new DomeinException("Een boekingsvoorstel moet uitleggen waarom het wordt gedaan.");
        }

        TransactieId = transactieId;
        TegenrekeningNummer = tegenrekeningNummer.Trim();
        TegenrekeningNaam = tegenrekeningNaam.Trim();
        Zekerheid = zekerheid;
        Reden = reden.Trim();
    }

    /// <summary>The transaction this proposal is about.</summary>
    public Guid TransactieId { get; }

    /// <summary>The ledger number of the proposed counterparty account.</summary>
    public string TegenrekeningNummer { get; }

    /// <summary>The name of the proposed counterparty account.</summary>
    public string TegenrekeningNaam { get; }

    /// <summary>How sure the assistant is, from 0 (pure guess) to 100 (learned rule on the IBAN).</summary>
    public int Zekerheid { get; }

    /// <summary>The reason, in the user's language, so the suggestion can be judged.</summary>
    public string Reden { get; }

    /// <summary>True when the assistant is sure enough for the proposal to be pre-selected.</summary>
    public bool IsBetrouwbaar => Zekerheid >= Betrouwbaarheidsgrens;
}
