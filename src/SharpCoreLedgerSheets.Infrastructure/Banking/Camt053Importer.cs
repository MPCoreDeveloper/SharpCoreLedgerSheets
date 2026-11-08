using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Infrastructure.Banking;

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
public sealed class Camt053Importer : IBankStatementImporter
{
    private const string PostingStatus = "BOOK";

    private static readonly string[] DateElements = ["BookgDt", "ValDt"];

    // In order of preference: the reference the bank assigned to the account, the entry reference,
    // and finally the references the payment itself carried.
    private static readonly string[] ReferenceElements =
        ["AcctSvcrRef", "NtryRef", "EndToEndId", "TxId", "InstrId"];

    private static readonly string[] CreditorFirst = ["Cdtr", "Dbtr"];
    private static readonly string[] DebtorFirst = ["Dbtr", "Cdtr"];

    /// <inheritdoc />
    public BankStatementFormat Format => BankStatementFormat.Camt053;

    /// <inheritdoc />
    public bool CanProcess(string fileName, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        return content.Contains("<Document", StringComparison.OrdinalIgnoreCase)
            && content.Contains("BkToCstmrStmt", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyList<BankTransaction> Read(
        string content,
        string bankAccountNumber,
        Currency? defaultCurrency = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankAccountNumber);

        var document = LoadDocument(content);
        var currency = defaultCurrency ?? Currency.Eur;

        var transactions = Elements(document, "Ntry")
            .Where(IsPosted)
            .Select(movement => ReadMovement(movement, bankAccountNumber, currency))
            .ToList();

        if (transactions.Count == 0)
        {
            throw new DomainException(MessageCode.StatementHasNoBookedMovements, "Het CAMT.053-bestand bevat geen geboekte bankmutaties.");
        }

        return transactions;
    }

    private static XDocument LoadDocument(string content)
    {
        XDocument document;

        try
        {
            document = XDocument.Parse(content, LoadOptions.None);
        }
        catch (XmlException error)
        {
            throw new DomainException(MessageCode.StatementNotReadableXml, "Het bestand is geen leesbaar XML-bestand (CAMT.053): {0}", error.Message);
        }

        var isCamt = document.Descendants().Any(element => IsKind(element, "BkToCstmrStmt"));

        if (document.Root is null || !IsKind(document.Root, "Document") || !isCamt)
        {
            throw new DomainException(
                MessageCode.StatementNotACamtFile,
                "Het bestand is geen CAMT.053-bankafschrift: het element BkToCstmrStmt ontbreekt.");
        }

        return document;
    }

    /// <summary>True for a movement the bank considers final; a pending one can still change.</summary>
    private static bool IsPosted(XElement movement)
    {
        var status = Text(movement, "Sts");
        return status is null || string.Equals(status, PostingStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static BankTransaction ReadMovement(XElement movement, string bankAccountNumber, Currency defaultCurrency)
    {
        var amount = ReadAmount(movement, defaultCurrency);
        var date = ReadDate(movement);
        var (counterpartyName, counterpartyIban) = ReadCounterparty(movement, amount.IsNegative);
        var description = ReadDescription(movement);

        var reference = ReadReference(movement)
            ?? SyntheticReference.From(date, amount, counterpartyName, counterpartyIban, description);

        return new BankTransaction(
            Guid.NewGuid(),
            bankAccountNumber,
            date,
            amount,
            reference,
            counterpartyName,
            counterpartyIban,
            description);
    }

    private static Amount ReadAmount(XElement movement, Currency defaultCurrency)
    {
        var element = Kind(movement, "Amt");

        // CAMT amounts never carry a thousands separator, so only a decimal point is accepted.
        const NumberStyles style = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        if (element is null
            || !decimal.TryParse(element.Value.Trim(), style, CultureInfo.InvariantCulture, out var value))
        {
            throw new DomainException(MessageCode.StatementUnreadableAmount, "Een mutatie in het CAMT.053-bestand heeft geen leesbaar bedrag (Amt).");
        }

        var currency = ReadCurrency(element, defaultCurrency);
        var direction = Text(movement, "CdtDbtInd");

        if (string.Equals(direction, "DBIT", StringComparison.OrdinalIgnoreCase))
        {
            return new Amount(-Math.Abs(value), currency);
        }

        if (string.Equals(direction, "CRDT", StringComparison.OrdinalIgnoreCase))
        {
            return new Amount(Math.Abs(value), currency);
        }

        // Without a direction, the sign of the amount itself is all the statement gives us.
        return new Amount(value, currency);
    }

    private static Currency ReadCurrency(XElement amount, Currency defaultCurrency)
    {
        var code = amount.Attribute("Ccy")?.Value.Trim();

        if (string.IsNullOrEmpty(code))
        {
            return defaultCurrency;
        }

        var currency = Currency.All.FirstOrDefault(
            known => string.Equals(known.Code, code, StringComparison.OrdinalIgnoreCase));

        return currency
            ?? throw new DomainException(MessageCode.StatementUnsupportedCurrency, "De valuta '{0}' uit het CAMT.053-bestand wordt niet ondersteund.", code);
    }

    private static DateOnly ReadDate(XElement movement)
    {
        foreach (var name in DateElements)
        {
            var element = Kind(movement, name);
            var value = element is null
                ? null
                : Text(element, "Dt") ?? DateFromTimestamp(Text(element, "DtTm"));

            if (value is not null
                && DateOnly.TryParseExact(
                    value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                return date;
            }
        }

        throw new DomainException(MessageCode.StatementUnreadableBookingDate, "Een mutatie in het CAMT.053-bestand heeft geen leesbare boekdatum.");
    }

    private static string? DateFromTimestamp(string? timestamp) => timestamp?.Split('T')[0];

    private static string? ReadReference(XElement movement)
    {
        foreach (var name in ReferenceElements)
        {
            var value = Elements(movement, name)
                .Select(element => element.Value.Trim())
                .FirstOrDefault(text => text.Length > 0 && !IsNoReference(text));

            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Some banks write <c>NOTPROVIDED</c> where the standard expects a real reference.</summary>
    private static bool IsNoReference(string value) =>
        value.Equals("NOTPROVIDED", StringComparison.OrdinalIgnoreCase)
        || value.Equals("NONREF", StringComparison.OrdinalIgnoreCase);

    private static (string? Name, string? Iban) ReadCounterparty(XElement movement, bool isDepreciation)
    {
        var parties = FirstKind(movement, "RltdPties");

        if (parties is null)
        {
            return (null, null);
        }

        foreach (var partyName in isDepreciation ? CreditorFirst : DebtorFirst)
        {
            var party = Kind(parties, partyName);

            if (party is null)
            {
                continue;
            }

            var name = FirstText(party, "Nm");
            var iban = FirstText(Kind(parties, partyName + "Acct"), "IBAN");

            if (name is not null || iban is not null)
            {
                return (name, iban);
            }
        }

        return (null, null);
    }

    private static string? ReadDescription(XElement movement)
    {
        var parts = Elements(movement, "Ustrd")
            .Select(element => element.Value.Trim())
            .Where(text => text.Length > 0)
            .ToList();

        return parts.Count > 0
            ? string.Join(' ', parts)
            : FirstText(movement, "AddtlNtryInf");
    }

    /// <summary>All descendants with the given local name, regardless of the CAMT version or namespace.</summary>
    private static IEnumerable<XElement> Elements(XContainer content, string name) =>
        content.Descendants().Where(element => IsKind(element, name));

    /// <summary>The first descendant (or the element itself) with the given local name.</summary>
    private static XElement? FirstKind(XElement element, string name) =>
        element.DescendantsAndSelf().FirstOrDefault(kind => IsKind(kind, name));

    /// <summary>A direct child with the given local name.</summary>
    private static XElement? Kind(XElement element, string name) =>
        element.Elements().FirstOrDefault(kind => IsKind(kind, name));

    /// <summary>The text of the first descendant with the given local name that is not empty.</summary>
    private static string? FirstText(XElement? element, string name)
    {
        if (element is null)
        {
            return null;
        }

        return element.DescendantsAndSelf()
            .Where(kind => IsKind(kind, name))
            .Select(kind => kind.Value.Trim())
            .FirstOrDefault(text => text.Length > 0);
    }

    /// <summary>The text of a direct child, or <see langword="null"/> when it is missing or empty.</summary>
    private static string? Text(XElement element, string name)
    {
        var value = Kind(element, name)?.Value.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool IsKind(XElement element, string name) =>
        string.Equals(element.Name.LocalName, name, StringComparison.Ordinal);
}

