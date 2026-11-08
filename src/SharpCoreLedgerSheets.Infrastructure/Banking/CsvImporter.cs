using System.Globalization;
using System.Text;
using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Infrastructure.Banking;

/// <summary>
/// Reads delimited text exports (CSV) of bank statements.
/// </summary>
/// <remarks>
/// <para>
/// Bank exports differ per bank, so the reader recognises columns by their header names instead of
/// by their position. The header may use <c>Date</c>, <c>Date</c> or <c>Boekdatum</c>,
/// <c>Amount</c>, <c>Amount (EUR)</c> or <c>Amount</c>, and so on; case, spaces and punctuation are
/// ignored. The delimiter (<c>;</c>, <c>,</c> or a tab) is detected from the header line, so both
/// Dutch (semicolon plus comma decimals) and English exports are read.
/// </para>
/// <para>
/// A column with the direction (<c>Af/On</c>) decides the sign when it is present; otherwise the
/// sign of the amount itself is used. Rows without a readable date are not movements (a total or
/// footer line) and are skipped, but a row with a date and an unreadable amount stops the import
/// with a message that names the line. Quoted fields and doubled quotes are supported; a quoted
/// value that spans several lines is not.
/// </para>
/// </remarks>
public sealed class CsvImporter : IBankStatementImporter
{
    private const string FieldDate = "datum";
    private const string FieldAmount = "bedrag";
    private const string FieldReference = "referentie";
    private const string FieldCounterparty = "tegenpartij";
    private const string FieldIban = "iban";
    private const string FieldDescription = "omschrijving";
    private const string FieldDirection = "richting";

    /// <summary>Header names per field, normalised to letters and digits in lower case.</summary>
    private static readonly (string Field, string[] Headers)[] Columns =
    [
        (FieldDate,
            ["datum", "date", "boekdatum", "boekingsdatum", "transactiedatum", "valutadatum", "rentedatum"]),
        (FieldAmount,
            ["bedrag", "bedrageur", "bedragineur", "amount", "amounteur", "mutatie", "transactiebedrag",
                "bedragafbij", "bedragbijaf"]),
        (FieldReference,
            ["referentie", "reference", "transactiereferentie", "transactieid", "volgnummer", "kenmerk"]),
        (FieldCounterparty,
            ["tegenpartij", "tegenpartijnaam", "naamtegenpartij", "naam", "counterparty", "tegenrekening",
                "begunstigde", "opdrachtgever"]),
        (FieldIban,
            ["iban", "tegenrekeningiban", "tegenpartijiban", "tegenrekeningnummer", "rekeningnummer"]),
        (FieldDescription,
            ["omschrijving", "description", "mededelingen", "mededeling", "betalingskenmerk", "details"]),
        (FieldDirection,
            ["afbij", "bijaf", "debitcredit", "creditdebit", "richting"]),
    ];

    private static readonly string[] DateFormaten =
    [
        "yyyy-MM-dd", "yyyy/MM/dd", "dd-MM-yyyy", "d-M-yyyy",
        "dd/MM/yyyy", "d/M/yyyy", "yyyyMMdd", "ddMMyyyy",
    ];

    /// <inheritdoc />
    public BankStatementFormat Format => BankStatementFormat.Csv;

    /// <inheritdoc />
    public bool CanProcess(string fileName, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Lines(content).Exists(line => line.Length > 0);
    }

    /// <inheritdoc />
    public IReadOnlyList<BankTransaction> Read(
        string content,
        string bankAccountNumber,
        Currency? defaultCurrency = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankAccountNumber);

        var currency = defaultCurrency ?? Currency.Eur;
        var lines = Lines(content);

        // The header is the first line with content, so blank lines above it are allowed.
        var header = lines.FindIndex(line => line.Length > 0);

        if (header < 0 || header == lines.Count - 1)
        {
            throw new DomainException(MessageCode.CsvHasNoHeader, "Het CSV-bestand heeft geen kopregel met bankmutaties.");
        }

        var separator = DetermineSeparator(lines[header]);
        var headers = LinkHeaders(Split(lines[header], separator));

        var missing = new List<string>();

        if (!headers.ContainsKey(FieldDate))
        {
            missing.Add("Date");
        }

        if (!headers.ContainsKey(FieldAmount))
        {
            missing.Add("Amount");
        }

        if (missing.Count > 0)
        {
            throw new DomainException(
                MessageCode.CsvMissingColumn,
                "Het CSV-bestand heeft geen herkenbare kopregel: de kolom '{0}' ontbreekt.",
                string.Join("' and '", missing));
        }

        var transactions = new List<BankTransaction>();

        // Line numbers are one-based and count from the start of the file, so a message points at the
        // line the user sees in their spreadsheet.
        for (var number = header + 2; number <= lines.Count; number++)
        {
            var fields = Split(lines[number - 1], separator);

            if (!DateOnly.TryParseExact(
                    Field(fields, headers, FieldDate) ?? string.Empty,
                    DateFormaten,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                // A line without a date is not a movement: a total, a header repeat or a footer.
                continue;
            }

            var direction = DirectionSign(Field(fields, headers, FieldDirection));
            var amount = ReadAmount(Field(fields, headers, FieldAmount), currency, number);

            if (direction != 0)
            {
                amount = new Amount(Math.Abs(amount.Value) * direction, currency);
            }

            var counterpartyName = Field(fields, headers, FieldCounterparty);
            var counterpartyIban = Field(fields, headers, FieldIban);
            var description = Field(fields, headers, FieldDescription);

            var reference = Field(fields, headers, FieldReference)
                ?? SyntheticReference.From(date, amount, counterpartyName, counterpartyIban, description);

            transactions.Add(new BankTransaction(
                Guid.NewGuid(),
                bankAccountNumber,
                date,
                amount,
                reference,
                counterpartyName,
                counterpartyIban,
                description));
        }

        if (transactions.Count == 0)
        {
            throw new DomainException(MessageCode.CsvHasNoMovements, "Het CSV-bestand bevat geen bankmutaties.");
        }

        return transactions;
    }

    /// <summary>Reads the amount of a line, accepting both decimal notations and both signs.</summary>
    private static Amount ReadAmount(string? value, Currency currency, int number)
    {
        var text = value ?? string.Empty;
        var digits = new string([.. text.Where(IsAmountSign)]);
        var normalized = digits.Length == 0 ? null : NormalizeNumber(digits);

        if (normalized is null
            || !decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            throw new DomainException(MessageCode.CsvUnreadableAmount, "Regel {0} van het CSV-bestand heeft geen leesbaar bedrag ('{1}').", number, text);
        }

        // A bank export may write a payment as '-12,50' or as '(12,50)'.
        var negative = text.Contains('-') || (text.Contains('(') && text.Contains(')'));

        return new Amount(negative ? -amount : amount, currency);
    }

    private static bool IsAmountSign(char sign) => char.IsDigit(sign) || sign is ',' or '.';

    /// <summary>
    /// Rewrites a number to the invariant notation: the rightmost of <c>,</c> and <c>.</c> is the
    /// decimal separator and the other groups thousands, so both <c>1.234,56</c> and <c>1,234.56</c>
    /// are read as 1234.56.
    /// </summary>
    private static string? NormalizeNumber(string digits)
    {
        var comma = digits.LastIndexOf(',');
        var dot = digits.LastIndexOf('.');

        if (comma >= 0 && dot >= 0)
        {
            var decimalSeparator = comma > dot ? ',' : '.';
            var thousandsSeparator = decimalSeparator == ',' ? "." : ",";

            return digits
                .Replace(thousandsSeparator, string.Empty, StringComparison.Ordinal)
                .Replace(decimalSeparator.ToString(), ".", StringComparison.Ordinal);
        }

        if (comma < 0 && dot < 0)
        {
            return digits;
        }

        // With a single separator, three digits behind it mean thousands ('1.234'), anything else
        // means decimals ('12,5').
        var separator = comma >= 0 ? ',' : '.';
        var count = digits.Count(sign => sign == separator);
        var decimals = digits.Length - digits.LastIndexOf(separator) - 1;

        return count > 1 || decimals == 3
            ? digits.Replace(separator.ToString(), string.Empty, StringComparison.Ordinal)
            : digits.Replace(separator, '.');
    }

    /// <summary>Reads a column that states whether an amount is paid (<c>Af</c>) or received (<c>On</c>).</summary>
    private static int DirectionSign(string? value) => NormalizeHeader(value ?? string.Empty) switch
    {
        "af" or "debit" or "d" => -1,
        "bij" or "credit" or "c" => 1,
        _ => 0,
    };

    /// <summary>Picks the delimiter that occurs most often in the header line.</summary>
    private static char DetermineSeparator(string headerLine)
    {
        var kandidaten = new[] { ';', ',', '\t' };
        var best = ';';
        var bestCount = 0;

        foreach (var candidate in kandidaten)
        {
            var count = headerLine.Count(sign => sign == candidate);

            if (count > bestCount)
            {
                best = candidate;
                bestCount = count;
            }
        }

        return best;
    }

    /// <summary>Maps every field to the column that holds it, using the header names.</summary>
    private static Dictionary<string, int> LinkHeaders(IReadOnlyList<string> headers)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < headers.Count; index++)
        {
            var name = NormalizeHeader(headers[index]);

            if (name.Length == 0)
            {
                continue;
            }

            foreach (var (field, names) in Columns)
            {
                if (!result.ContainsKey(field) && names.Contains(name, StringComparer.Ordinal))
                {
                    result[field] = index;
                }
            }
        }

        return result;
    }

    /// <summary>Reduces a header name to lower-case letters and digits, so 'Amount (EUR)' matches 'bedrageur'.</summary>
    private static string NormalizeHeader(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var sign in text)
        {
            if (char.IsLetterOrDigit(sign))
            {
                builder.Append(char.ToLowerInvariant(sign));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Split the content into trimmed lines. Blank lines are kept so that a line number in an error
    /// message is the line the user sees in their spreadsheet.
    /// </summary>
    private static List<string> Lines(string content)
    {
        var lines = new List<string>();

        foreach (var line in content.Split('\n'))
        {
            lines.Add(line.Trim());
        }

        return lines;
    }

    /// <summary>Split a line into its fields, honouring double quotes and doubled quotes.</summary>
    private static List<string> Split(string line, char separator)
    {
        var fields = new List<string>();
        var builder = new StringBuilder(line.Length);
        var betweenQuotationMarks = false;
        var index = 0;

        while (index < line.Length)
        {
            var sign = line[index];

            if (sign == '"')
            {
                if (betweenQuotationMarks && index + 1 < line.Length && line[index + 1] == '"')
                {
                    // A doubled quote inside a quoted field is one literal quote.
                    builder.Append('"');
                    index += 2;
                    continue;
                }

                betweenQuotationMarks = !betweenQuotationMarks;
                index++;
                continue;
            }

            if (sign == separator && !betweenQuotationMarks)
            {
                fields.Add(builder.ToString().Trim());
                builder.Clear();
                index++;
                continue;
            }

            builder.Append(sign);
            index++;
        }

        fields.Add(builder.ToString().Trim());
        return fields;
    }

    /// <summary>The trimmed value of a field, or <see langword="null"/> when the column or value is absent.</summary>
    private static string? Field(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> headers,
        string name)
    {
        if (!headers.TryGetValue(name, out var index) || index >= fields.Count)
        {
            return null;
        }

        var value = fields[index].Trim();
        return value.Length == 0 ? null : value;
    }
}
