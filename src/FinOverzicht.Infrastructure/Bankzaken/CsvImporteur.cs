using System.Globalization;
using System.Text;
using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Infrastructure.Bankzaken;

/// <summary>
/// Reads delimited text exports (CSV) of bank statements.
/// </summary>
/// <remarks>
/// <para>
/// Bank exports differ per bank, so the reader recognises columns by their header names instead of
/// by their position. The header may use <c>Datum</c>, <c>Date</c> or <c>Boekdatum</c>,
/// <c>Bedrag</c>, <c>Bedrag (EUR)</c> or <c>Amount</c>, and so on; case, spaces and punctuation are
/// ignored. The delimiter (<c>;</c>, <c>,</c> or a tab) is detected from the header line, so both
/// Dutch (semicolon plus comma decimals) and English exports are read.
/// </para>
/// <para>
/// A column with the direction (<c>Af/Bij</c>) decides the sign when it is present; otherwise the
/// sign of the amount itself is used. Rows without a readable date are not movements (a total or
/// footer line) and are skipped, but a row with a date and an unreadable amount stops the import
/// with a message that names the line. Quoted fields and doubled quotes are supported; a quoted
/// value that spans several lines is not.
/// </para>
/// </remarks>
public sealed class CsvImporteur : IBankstaatImporteur
{
    private const string VeldDatum = "datum";
    private const string VeldBedrag = "bedrag";
    private const string VeldReferentie = "referentie";
    private const string VeldTegenpartij = "tegenpartij";
    private const string VeldIban = "iban";
    private const string VeldOmschrijving = "omschrijving";
    private const string VeldRichting = "richting";

    /// <summary>Header names per field, normalised to letters and digits in lower case.</summary>
    private static readonly (string Veld, string[] Koppen)[] Kolommen =
    [
        (VeldDatum,
            ["datum", "date", "boekdatum", "boekingsdatum", "transactiedatum", "valutadatum", "rentedatum"]),
        (VeldBedrag,
            ["bedrag", "bedrageur", "bedragineur", "amount", "amounteur", "mutatie", "transactiebedrag",
                "bedragafbij", "bedragbijaf"]),
        (VeldReferentie,
            ["referentie", "reference", "transactiereferentie", "transactieid", "volgnummer", "kenmerk"]),
        (VeldTegenpartij,
            ["tegenpartij", "tegenpartijnaam", "naamtegenpartij", "naam", "counterparty", "tegenrekening",
                "begunstigde", "opdrachtgever"]),
        (VeldIban,
            ["iban", "tegenrekeningiban", "tegenpartijiban", "tegenrekeningnummer", "rekeningnummer"]),
        (VeldOmschrijving,
            ["omschrijving", "description", "mededelingen", "mededeling", "betalingskenmerk", "details"]),
        (VeldRichting,
            ["afbij", "bijaf", "debitcredit", "creditdebit", "richting"]),
    ];

    private static readonly string[] DatumFormaten =
    [
        "yyyy-MM-dd", "yyyy/MM/dd", "dd-MM-yyyy", "d-M-yyyy",
        "dd/MM/yyyy", "d/M/yyyy", "yyyyMMdd", "ddMMyyyy",
    ];

    /// <inheritdoc />
    public BankstaatFormaat Formaat => BankstaatFormaat.Csv;

    /// <inheritdoc />
    public bool KanVerwerken(string bestandsnaam, string inhoud)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bestandsnaam);
        ArgumentNullException.ThrowIfNull(inhoud);

        if (!bestandsnaam.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            && !bestandsnaam.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Regels(inhoud).Exists(regel => regel.Length > 0);
    }

    /// <inheritdoc />
    public IReadOnlyList<BankTransactie> Lees(
        string inhoud,
        string bankRekeningNummer,
        Valuta? standaardValuta = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inhoud);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankRekeningNummer);

        var valuta = standaardValuta ?? Valuta.Eur;
        var regels = Regels(inhoud);

        // The header is the first line with content, so blank lines above it are allowed.
        var kop = regels.FindIndex(regel => regel.Length > 0);

        if (kop < 0 || kop == regels.Count - 1)
        {
            throw new DomeinException("Het CSV-bestand heeft geen kopregel met bankmutaties.");
        }

        var scheidingsteken = BepaalScheidingsteken(regels[kop]);
        var koppen = KoppelKoppen(Splits(regels[kop], scheidingsteken));

        var ontbreekt = new List<string>();

        if (!koppen.ContainsKey(VeldDatum))
        {
            ontbreekt.Add("Datum");
        }

        if (!koppen.ContainsKey(VeldBedrag))
        {
            ontbreekt.Add("Bedrag");
        }

        if (ontbreekt.Count > 0)
        {
            throw new DomeinException(
                $"Het CSV-bestand heeft geen herkenbare kopregel: de kolom '{string.Join("' en '", ontbreekt)}' ontbreekt.");
        }

        var transacties = new List<BankTransactie>();

        // Line numbers are one-based and count from the start of the file, so a message points at the
        // line the user sees in their spreadsheet.
        for (var nummer = kop + 2; nummer <= regels.Count; nummer++)
        {
            var velden = Splits(regels[nummer - 1], scheidingsteken);

            if (!DateOnly.TryParseExact(
                    Veld(velden, koppen, VeldDatum) ?? string.Empty,
                    DatumFormaten,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var datum))
            {
                // A line without a date is not a movement: a total, a header repeat or a footer.
                continue;
            }

            var richting = RichtingTeken(Veld(velden, koppen, VeldRichting));
            var bedrag = LeesBedrag(Veld(velden, koppen, VeldBedrag), valuta, nummer);

            if (richting != 0)
            {
                bedrag = new Bedrag(Math.Abs(bedrag.Waarde) * richting, valuta);
            }

            var tegenpartijNaam = Veld(velden, koppen, VeldTegenpartij);
            var tegenpartijIban = Veld(velden, koppen, VeldIban);
            var omschrijving = Veld(velden, koppen, VeldOmschrijving);

            var referentie = Veld(velden, koppen, VeldReferentie)
                ?? SynthetischeReferentie.Van(datum, bedrag, tegenpartijNaam, tegenpartijIban, omschrijving);

            transacties.Add(new BankTransactie(
                Guid.NewGuid(),
                bankRekeningNummer,
                datum,
                bedrag,
                referentie,
                tegenpartijNaam,
                tegenpartijIban,
                omschrijving));
        }

        if (transacties.Count == 0)
        {
            throw new DomeinException("Het CSV-bestand bevat geen bankmutaties.");
        }

        return transacties;
    }

    /// <summary>Reads the amount of a line, accepting both decimal notations and both signs.</summary>
    private static Bedrag LeesBedrag(string? waarde, Valuta valuta, int nummer)
    {
        var tekst = waarde ?? string.Empty;
        var cijfers = new string([.. tekst.Where(IsBedragTeken)]);
        var genormaliseerd = cijfers.Length == 0 ? null : NormaliseerGetal(cijfers);

        if (genormaliseerd is null
            || !decimal.TryParse(genormaliseerd, NumberStyles.Number, CultureInfo.InvariantCulture, out var bedrag))
        {
            throw new DomeinException($"Regel {nummer} van het CSV-bestand heeft geen leesbaar bedrag ('{tekst}').");
        }

        // A bank export may write a payment as '-12,50' or as '(12,50)'.
        var negatief = tekst.Contains('-') || (tekst.Contains('(') && tekst.Contains(')'));

        return new Bedrag(negatief ? -bedrag : bedrag, valuta);
    }

    private static bool IsBedragTeken(char teken) => char.IsDigit(teken) || teken is ',' or '.';

    /// <summary>
    /// Rewrites a number to the invariant notation: the rightmost of <c>,</c> and <c>.</c> is the
    /// decimal separator and the other groups thousands, so both <c>1.234,56</c> and <c>1,234.56</c>
    /// are read as 1234.56.
    /// </summary>
    private static string? NormaliseerGetal(string cijfers)
    {
        var komma = cijfers.LastIndexOf(',');
        var punt = cijfers.LastIndexOf('.');

        if (komma >= 0 && punt >= 0)
        {
            var decimaal = komma > punt ? ',' : '.';
            var duizendtal = decimaal == ',' ? "." : ",";

            return cijfers
                .Replace(duizendtal, string.Empty, StringComparison.Ordinal)
                .Replace(decimaal.ToString(), ".", StringComparison.Ordinal);
        }

        if (komma < 0 && punt < 0)
        {
            return cijfers;
        }

        // With a single separator, three digits behind it mean thousands ('1.234'), anything else
        // means decimals ('12,5').
        var scheidingsteken = komma >= 0 ? ',' : '.';
        var aantal = cijfers.Count(teken => teken == scheidingsteken);
        var decimalen = cijfers.Length - cijfers.LastIndexOf(scheidingsteken) - 1;

        return aantal > 1 || decimalen == 3
            ? cijfers.Replace(scheidingsteken.ToString(), string.Empty, StringComparison.Ordinal)
            : cijfers.Replace(scheidingsteken, '.');
    }

    /// <summary>Reads a column that states whether an amount is paid (<c>Af</c>) or received (<c>Bij</c>).</summary>
    private static int RichtingTeken(string? waarde) => NormaliseerKop(waarde ?? string.Empty) switch
    {
        "af" or "debit" or "d" => -1,
        "bij" or "credit" or "c" => 1,
        _ => 0,
    };

    /// <summary>Picks the delimiter that occurs most often in the header line.</summary>
    private static char BepaalScheidingsteken(string kopregel)
    {
        var kandidaten = new[] { ';', ',', '\t' };
        var beste = ';';
        var besteAantal = 0;

        foreach (var kandidaat in kandidaten)
        {
            var aantal = kopregel.Count(teken => teken == kandidaat);

            if (aantal > besteAantal)
            {
                beste = kandidaat;
                besteAantal = aantal;
            }
        }

        return beste;
    }

    /// <summary>Maps every field to the column that holds it, using the header names.</summary>
    private static Dictionary<string, int> KoppelKoppen(IReadOnlyList<string> koppen)
    {
        var resultaat = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < koppen.Count; index++)
        {
            var naam = NormaliseerKop(koppen[index]);

            if (naam.Length == 0)
            {
                continue;
            }

            foreach (var (veld, namen) in Kolommen)
            {
                if (!resultaat.ContainsKey(veld) && namen.Contains(naam, StringComparer.Ordinal))
                {
                    resultaat[veld] = index;
                }
            }
        }

        return resultaat;
    }

    /// <summary>Reduces a header name to lower-case letters and digits, so 'Bedrag (EUR)' matches 'bedrageur'.</summary>
    private static string NormaliseerKop(string tekst)
    {
        var bouwer = new StringBuilder(tekst.Length);

        foreach (var teken in tekst)
        {
            if (char.IsLetterOrDigit(teken))
            {
                bouwer.Append(char.ToLowerInvariant(teken));
            }
        }

        return bouwer.ToString();
    }

    /// <summary>
    /// Splits the content into trimmed lines. Blank lines are kept so that a line number in an error
    /// message is the line the user sees in their spreadsheet.
    /// </summary>
    private static List<string> Regels(string inhoud)
    {
        var regels = new List<string>();

        foreach (var regel in inhoud.Split('\n'))
        {
            regels.Add(regel.Trim());
        }

        return regels;
    }

    /// <summary>Splits a line into its fields, honouring double quotes and doubled quotes.</summary>
    private static List<string> Splits(string regel, char scheidingsteken)
    {
        var velden = new List<string>();
        var bouwer = new StringBuilder(regel.Length);
        var tussenAanhalingstekens = false;
        var index = 0;

        while (index < regel.Length)
        {
            var teken = regel[index];

            if (teken == '"')
            {
                if (tussenAanhalingstekens && index + 1 < regel.Length && regel[index + 1] == '"')
                {
                    // A doubled quote inside a quoted field is one literal quote.
                    bouwer.Append('"');
                    index += 2;
                    continue;
                }

                tussenAanhalingstekens = !tussenAanhalingstekens;
                index++;
                continue;
            }

            if (teken == scheidingsteken && !tussenAanhalingstekens)
            {
                velden.Add(bouwer.ToString().Trim());
                bouwer.Clear();
                index++;
                continue;
            }

            bouwer.Append(teken);
            index++;
        }

        velden.Add(bouwer.ToString().Trim());
        return velden;
    }

    /// <summary>The trimmed value of a field, or <see langword="null"/> when the column or value is absent.</summary>
    private static string? Veld(
        IReadOnlyList<string> velden,
        IReadOnlyDictionary<string, int> koppen,
        string naam)
    {
        if (!koppen.TryGetValue(naam, out var index) || index >= velden.Count)
        {
            return null;
        }

        var waarde = velden[index].Trim();
        return waarde.Length == 0 ? null : waarde;
    }
}
