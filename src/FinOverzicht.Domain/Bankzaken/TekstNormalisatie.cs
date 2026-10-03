using System.Text;

namespace FinOverzicht.Domain.Bankzaken;

/// <summary>
/// Normalises the free text of a bank statement so that matching does not depend on casing,
/// accents-in-white-space or the way a bank chose to group an IBAN.
/// </summary>
/// <remarks>
/// Everything is compared with ordinal (culture-independent) semantics: bank data is data, not
/// prose, and a Turkish "I" must not behave differently from a Dutch one.
/// </remarks>
internal static class TekstNormalisatie
{
    /// <summary>Uppercase invariant, single spaces, no leading/trailing white space.</summary>
    internal static string Normaliseer(params string?[] delen)
    {
        ArgumentNullException.ThrowIfNull(delen);

        var tekst = string.Join(' ', delen.Where(deel => !string.IsNullOrWhiteSpace(deel)));
        var bouwer = new StringBuilder(tekst.Length);
        var vorigeWasSpatie = false;

        foreach (var teken in tekst)
        {
            if (char.IsWhiteSpace(teken))
            {
                if (!vorigeWasSpatie && bouwer.Length > 0)
                {
                    bouwer.Append(' ');
                }

                vorigeWasSpatie = true;
                continue;
            }

            bouwer.Append(char.ToUpperInvariant(teken));
            vorigeWasSpatie = false;
        }

        return bouwer.ToString().Trim();
    }

    /// <summary>Normalises an IBAN by removing every separator, so grouping cannot break a match.</summary>
    internal static string Iban(string? iban) =>
        iban is null ? string.Empty : Normaliseer(iban).Replace(" ", string.Empty, StringComparison.Ordinal);

    /// <summary>Builds the import key of a statement line (bank account plus bank reference).</summary>
    internal static string Sleutel(string bankRekeningNummer, string referentie) =>
        string.Concat(Normaliseer(bankRekeningNummer), "|", Normaliseer(referentie));

    /// <summary>True when the haystack contains the needle; both must already be normalised.</summary>
    internal static bool Bevat(string hooiberg, string naald) =>
        naald.Length > 0 && hooiberg.Contains(naald, StringComparison.Ordinal);

    /// <summary>The separate words of an already normalised text.</summary>
    internal static string[] Woorden(string tekst) =>
        tekst.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
