using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Infrastructure.Bankzaken;

/// <summary>
/// Reads ISO 20022 CAMT.053 bank statements (the XML statement most banks offer for download).
/// </summary>
/// <remarks>
/// <para>
/// Elements are matched on their local name, so every CAMT.053 version
/// (camt.053.001.02 up to the latest) and every namespace a bank happens to use is accepted.
/// Optional elements are treated as optional here as well.
/// </para>
/// <para>
/// Only booked movements (<c>&lt;Sts&gt;BOOK&lt;/Sts&gt;</c>) are read: a pending movement
/// (<c>PDNG</c>) can still change or disappear, so booking it would create an entry the statement
/// may later contradict. The direction comes from <c>CdtDbtInd</c>, which states the movement from
/// the account holder's point of view: <c>DBIT</c> is money paid, <c>CRDT</c> is money received.
/// </para>
/// </remarks>
public sealed class Camt053Importeur : IBankstaatImporteur
{
    private const string Boekingsstatus = "BOOK";

    private static readonly string[] DatumElementen = ["BookgDt", "ValDt"];

    // In order of preference: the reference the bank assigned to the account, the entry reference,
    // and finally the references the payment itself carried.
    private static readonly string[] ReferentieElementen =
        ["AcctSvcrRef", "NtryRef", "EndToEndId", "TxId", "InstrId"];

    private static readonly string[] CrediteurEerst = ["Cdtr", "Dbtr"];
    private static readonly string[] DebiteurEerst = ["Dbtr", "Cdtr"];

    /// <inheritdoc />
    public BankstaatFormaat Formaat => BankstaatFormaat.Camt053;

    /// <inheritdoc />
    public bool KanVerwerken(string bestandsnaam, string inhoud)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bestandsnaam);
        ArgumentNullException.ThrowIfNull(inhoud);

        return inhoud.Contains("<Document", StringComparison.OrdinalIgnoreCase)
            && inhoud.Contains("BkToCstmrStmt", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyList<BankTransactie> Lees(
        string inhoud,
        string bankRekeningNummer,
        Valuta? standaardValuta = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inhoud);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankRekeningNummer);

        var document = LaadDocument(inhoud);
        var valuta = standaardValuta ?? Valuta.Eur;

        var transacties = Elementen(document, "Ntry")
            .Where(IsGeboekt)
            .Select(mutatie => LeesMutatie(mutatie, bankRekeningNummer, valuta))
            .ToList();

        if (transacties.Count == 0)
        {
            throw new DomeinException("Het CAMT.053-bestand bevat geen geboekte bankmutaties.");
        }

        return transacties;
    }

    private static XDocument LaadDocument(string inhoud)
    {
        XDocument document;

        try
        {
            document = XDocument.Parse(inhoud, LoadOptions.None);
        }
        catch (XmlException fout)
        {
            throw new DomeinException($"Het bestand is geen leesbaar XML-bestand (CAMT.053): {fout.Message}");
        }

        var isCamt = document.Descendants().Any(element => IsKind(element, "BkToCstmrStmt"));

        if (document.Root is null || !IsKind(document.Root, "Document") || !isCamt)
        {
            throw new DomeinException(
                "Het bestand is geen CAMT.053-bankafschrift: het element BkToCstmrStmt ontbreekt.");
        }

        return document;
    }

    /// <summary>True for a movement the bank considers final; a pending one can still change.</summary>
    private static bool IsGeboekt(XElement mutatie)
    {
        var status = Tekst(mutatie, "Sts");
        return status is null || string.Equals(status, Boekingsstatus, StringComparison.OrdinalIgnoreCase);
    }

    private static BankTransactie LeesMutatie(XElement mutatie, string bankRekeningNummer, Valuta standaardValuta)
    {
        var bedrag = LeesBedrag(mutatie, standaardValuta);
        var datum = LeesDatum(mutatie);
        var (tegenpartijNaam, tegenpartijIban) = LeesTegenpartij(mutatie, bedrag.IsNegatief);
        var omschrijving = LeesOmschrijving(mutatie);

        var referentie = LeesReferentie(mutatie)
            ?? SynthetischeReferentie.Van(datum, bedrag, tegenpartijNaam, tegenpartijIban, omschrijving);

        return new BankTransactie(
            Guid.NewGuid(),
            bankRekeningNummer,
            datum,
            bedrag,
            referentie,
            tegenpartijNaam,
            tegenpartijIban,
            omschrijving);
    }

    private static Bedrag LeesBedrag(XElement mutatie, Valuta standaardValuta)
    {
        var element = Kind(mutatie, "Amt");

        // CAMT amounts never carry a thousands separator, so only a decimal point is accepted.
        const NumberStyles stijl = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        if (element is null
            || !decimal.TryParse(element.Value.Trim(), stijl, CultureInfo.InvariantCulture, out var waarde))
        {
            throw new DomeinException("Een mutatie in het CAMT.053-bestand heeft geen leesbaar bedrag (Amt).");
        }

        var valuta = LeesValuta(element, standaardValuta);
        var richting = Tekst(mutatie, "CdtDbtInd");

        if (string.Equals(richting, "DBIT", StringComparison.OrdinalIgnoreCase))
        {
            return new Bedrag(-Math.Abs(waarde), valuta);
        }

        if (string.Equals(richting, "CRDT", StringComparison.OrdinalIgnoreCase))
        {
            return new Bedrag(Math.Abs(waarde), valuta);
        }

        // Without a direction, the sign of the amount itself is all the statement gives us.
        return new Bedrag(waarde, valuta);
    }

    private static Valuta LeesValuta(XElement bedrag, Valuta standaardValuta)
    {
        var code = bedrag.Attribute("Ccy")?.Value.Trim();

        if (string.IsNullOrEmpty(code))
        {
            return standaardValuta;
        }

        var valuta = Valuta.Alle.FirstOrDefault(
            bekend => string.Equals(bekend.Code, code, StringComparison.OrdinalIgnoreCase));

        return valuta
            ?? throw new DomeinException($"De valuta '{code}' uit het CAMT.053-bestand wordt niet ondersteund.");
    }

    private static DateOnly LeesDatum(XElement mutatie)
    {
        foreach (var naam in DatumElementen)
        {
            var element = Kind(mutatie, naam);
            var waarde = element is null
                ? null
                : Tekst(element, "Dt") ?? DatumVanTijdstip(Tekst(element, "DtTm"));

            if (waarde is not null
                && DateOnly.TryParseExact(
                    waarde,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var datum))
            {
                return datum;
            }
        }

        throw new DomeinException("Een mutatie in het CAMT.053-bestand heeft geen leesbare boekdatum.");
    }

    private static string? DatumVanTijdstip(string? tijdstip) => tijdstip?.Split('T')[0];

    private static string? LeesReferentie(XElement mutatie)
    {
        foreach (var naam in ReferentieElementen)
        {
            var waarde = Elementen(mutatie, naam)
                .Select(element => element.Value.Trim())
                .FirstOrDefault(tekst => tekst.Length > 0 && !IsGeenReferentie(tekst));

            if (waarde is not null)
            {
                return waarde;
            }
        }

        return null;
    }

    /// <summary>Some banks write <c>NOTPROVIDED</c> where the standard expects a real reference.</summary>
    private static bool IsGeenReferentie(string waarde) =>
        waarde.Equals("NOTPROVIDED", StringComparison.OrdinalIgnoreCase)
        || waarde.Equals("NONREF", StringComparison.OrdinalIgnoreCase);

    private static (string? Naam, string? Iban) LeesTegenpartij(XElement mutatie, bool isAfschrijving)
    {
        var partijen = EersteKind(mutatie, "RltdPties");

        if (partijen is null)
        {
            return (null, null);
        }

        foreach (var partijNaam in isAfschrijving ? CrediteurEerst : DebiteurEerst)
        {
            var partij = Kind(partijen, partijNaam);

            if (partij is null)
            {
                continue;
            }

            var naam = EersteTekst(partij, "Nm");
            var iban = EersteTekst(Kind(partijen, partijNaam + "Acct"), "IBAN");

            if (naam is not null || iban is not null)
            {
                return (naam, iban);
            }
        }

        return (null, null);
    }

    private static string? LeesOmschrijving(XElement mutatie)
    {
        var delen = Elementen(mutatie, "Ustrd")
            .Select(element => element.Value.Trim())
            .Where(tekst => tekst.Length > 0)
            .ToList();

        return delen.Count > 0
            ? string.Join(' ', delen)
            : EersteTekst(mutatie, "AddtlNtryInf");
    }

    /// <summary>All descendants with the given local name, regardless of the CAMT version or namespace.</summary>
    private static IEnumerable<XElement> Elementen(XContainer inhoud, string naam) =>
        inhoud.Descendants().Where(element => IsKind(element, naam));

    /// <summary>The first descendant (or the element itself) with the given local name.</summary>
    private static XElement? EersteKind(XElement element, string naam) =>
        element.DescendantsAndSelf().FirstOrDefault(kind => IsKind(kind, naam));

    /// <summary>A direct child with the given local name.</summary>
    private static XElement? Kind(XElement element, string naam) =>
        element.Elements().FirstOrDefault(kind => IsKind(kind, naam));

    /// <summary>The text of the first descendant with the given local name that is not empty.</summary>
    private static string? EersteTekst(XElement? element, string naam)
    {
        if (element is null)
        {
            return null;
        }

        return element.DescendantsAndSelf()
            .Where(kind => IsKind(kind, naam))
            .Select(kind => kind.Value.Trim())
            .FirstOrDefault(tekst => tekst.Length > 0);
    }

    /// <summary>The text of a direct child, or <see langword="null"/> when it is missing or empty.</summary>
    private static string? Tekst(XElement element, string naam)
    {
        var waarde = Kind(element, naam)?.Value.Trim();
        return string.IsNullOrEmpty(waarde) ? null : waarde;
    }

    private static bool IsKind(XElement element, string naam) =>
        string.Equals(element.Name.LocalName, naam, StringComparison.Ordinal);
}

