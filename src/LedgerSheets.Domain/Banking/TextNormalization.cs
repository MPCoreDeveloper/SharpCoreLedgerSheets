using System.Text;

namespace LedgerSheets.Domain.Banking;

/// <summary>
/// Normalises the free text of a bank statement so that matching does not depend on casing,
/// accents-in-white-space or the way a bank chose to group an IBAN.
/// </summary>
/// <remarks>
/// Everything is compared with ordinal (culture-independent) semantics: bank data is data, not
/// prose, and a Turkish "I" must not behave differently from a Dutch one.
/// </remarks>
internal static class TextNormalization
{
    /// <summary>Uppercase invariant, single spaces, no leading/trailing white space.</summary>
    internal static string Normalize(params string?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var text = string.Join(' ', parts.Where(divide => !string.IsNullOrWhiteSpace(divide)));
        var builder = new StringBuilder(text.Length);
        var previousWasSpace = false;

        foreach (var sign in text)
        {
            if (char.IsWhiteSpace(sign))
            {
                if (!previousWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                previousWasSpace = true;
                continue;
            }

            builder.Append(char.ToUpperInvariant(sign));
            previousWasSpace = false;
        }

        return builder.ToString().Trim();
    }

    /// <summary>Normalises an IBAN by removing every separator, so grouping cannot break a match.</summary>
    internal static string Iban(string? iban) =>
        iban is null ? string.Empty : Normalize(iban).Replace(" ", string.Empty, StringComparison.Ordinal);

    /// <summary>Builds the import key of a statement line (bank account plus bank reference).</summary>
    internal static string Key(string bankAccountNumber, string reference) =>
        string.Concat(Normalize(bankAccountNumber), "|", Normalize(reference));

    /// <summary>True when the haystack contains the needle; both must already be normalised.</summary>
    internal static bool Contains(string haystack, string needle) =>
        needle.Length > 0 && haystack.Contains(needle, StringComparison.Ordinal);

    /// <summary>The separate words of an already normalised text.</summary>
    internal static string[] Words(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
