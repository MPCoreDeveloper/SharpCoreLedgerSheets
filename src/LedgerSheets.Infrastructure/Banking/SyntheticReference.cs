using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Infrastructure.Banking;

/// <summary>
/// Derives the reference of a bank statement line when the bank did not supply one.
/// </summary>
/// <remarks>
/// The reference is the identity of a movement, so it has to be stable: importing the same statement
/// twice may never produce a second movement. Deriving it from the content of the movement keeps the
/// import idempotent, at the price that a statement without references cannot tell two movements
/// apart that share their date, amount, currency and text - those count as one.
/// </remarks>
internal static class SyntheticReference
{
    /// <summary>The prefix that marks a reference as derived rather than supplied by the bank.</summary>
    internal const string Prefix = "SYN-";

    private const int CountHexCharacters = 12;

    /// <summary>Derives a stable reference from the date, amount and text of a movement.</summary>
    internal static string From(
        DateOnly date,
        Amount amount,
        string? counterparty,
        string? iban,
        string? description)
    {
        var content = string.Join(
            '|',
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            amount.Value.ToString("0.00", CultureInfo.InvariantCulture),
            amount.Currency.Code,
            Normalize(counterparty),
            Normalize(iban),
            Normalize(description));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var builder = new StringBuilder(Prefix, Prefix.Length + CountHexCharacters);

        for (var index = 0; index < CountHexCharacters / 2; index++)
        {
            builder.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    /// <summary>Uppercases the text and collapses it to single spaces, so casing cannot split a reference.</summary>
    private static string Normalize(string? text) =>
        text is null
            ? string.Empty
            : string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToUpperInvariant();
}
