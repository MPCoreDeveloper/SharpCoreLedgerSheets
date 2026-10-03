using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Bankzaken;

/// <summary>
/// A single movement on a bank account, as read from a bank statement file.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Mutatie"/> uses the account holder's point of view: a positive amount is money
/// received (the bank credits the account), a negative amount is money paid.
/// </para>
/// <para>
/// <see cref="Referentie"/> is the bank's own identifier for the movement and together with
/// <see cref="BankRekeningNummer"/> it forms <see cref="Sleutel"/>. That key is what makes an
/// import idempotent: importing the same statement twice must never duplicate a movement.
/// </para>
/// </remarks>
public sealed record BankTransactie
{
    /// <summary>Creates an imported bank transaction.</summary>
    /// <exception cref="DomeinException">Thrown when the movement cannot be used as a bank movement.</exception>
    public BankTransactie(
        Guid id,
        string bankRekeningNummer,
        DateOnly datum,
        Bedrag mutatie,
        string referentie,
        string? tegenpartijNaam = null,
        string? tegenpartijIban = null,
        string? omschrijving = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomeinException("Een banktransactie moet een uniek id hebben.");
        }

        if (string.IsNullOrWhiteSpace(bankRekeningNummer))
        {
            throw new DomeinException("Een banktransactie moet horen bij een grootboekrekening van de bank.");
        }

        if (string.IsNullOrWhiteSpace(referentie))
        {
            throw new DomeinException("Een banktransactie moet de referentie van de bank hebben.");
        }

        if (mutatie.IsNul)
        {
            throw new DomeinException("Een banktransactie zonder bedrag kan niet worden geboekt.");
        }

        Id = id;
        BankRekeningNummer = bankRekeningNummer.Trim();
        Datum = datum;
        Mutatie = mutatie;
        Referentie = referentie.Trim();
        TegenpartijNaam = Schoon(tegenpartijNaam);
        TegenpartijIban = Schoon(tegenpartijIban);
        Omschrijving = Schoon(omschrijving);
        Status = BankTransactieStatus.Open;
    }

    /// <summary>Unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>The ledger number of the bank account this movement belongs to.</summary>
    public string BankRekeningNummer { get; }

    /// <summary>The (value) date of the movement.</summary>
    public DateOnly Datum { get; }

    /// <summary>The signed movement: positive is received, negative is paid.</summary>
    public Bedrag Mutatie { get; }

    /// <summary>The bank's identifier for the movement.</summary>
    public string Referentie { get; }

    /// <summary>The name of the counterparty, when the statement provides one.</summary>
    public string? TegenpartijNaam { get; }

    /// <summary>The IBAN of the counterparty, when the statement provides one.</summary>
    public string? TegenpartijIban { get; }

    /// <summary>The free-text description or remittance information.</summary>
    public string? Omschrijving { get; }

    /// <summary>True when money was received on the account.</summary>
    public bool IsBijschrijving => Mutatie.IsPositief;

    /// <summary>True when money was paid from the account.</summary>
    public bool IsAfschrijving => Mutatie.IsNegatief;

    /// <summary>The absolute amount, used for the journal entry and for display.</summary>
    public Bedrag BedragZonderTeken => new(Math.Abs(Mutatie.Waarde), Mutatie.Valuta);

    /// <summary>The import key: the bank account plus the bank's reference.</summary>
    public string Sleutel => TekstNormalisatie.Sleutel(BankRekeningNummer, Referentie);

    /// <summary>The normalised text the matching assistant looks for keywords in.</summary>
    public string Zoektekst => TekstNormalisatie.Normaliseer(TegenpartijNaam, TegenpartijIban, Omschrijving);

    /// <summary>What has happened with this transaction so far.</summary>
    public BankTransactieStatus Status { get; private init; }

    /// <summary>The journal entry that was created from this transaction, when it is booked.</summary>
    public Guid? JournaalpostId { get; private init; }

    /// <summary>Returns this transaction marked as booked by the given journal entry.</summary>
    /// <exception cref="DomeinException">Thrown when the journal entry identifier is missing.</exception>
    public BankTransactie MarkeerGeboekt(Guid journaalpostId)
    {
        if (journaalpostId == Guid.Empty)
        {
            throw new DomeinException("Een geboekte banktransactie moet naar een journaalpost verwijzen.");
        }

        return this with { Status = BankTransactieStatus.Geboekt, JournaalpostId = journaalpostId };
    }

    /// <summary>Returns this transaction marked as deliberately ignored.</summary>
    public BankTransactie Negeer() =>
        this with { Status = BankTransactieStatus.Genegeerd, JournaalpostId = null };

    private static string? Schoon(string? waarde) =>
        string.IsNullOrWhiteSpace(waarde) ? null : waarde.Trim();
}
