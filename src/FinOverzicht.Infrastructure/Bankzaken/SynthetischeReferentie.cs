using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Infrastructure.Bankzaken;

/// <summary>
/// Derives the reference of a bank statement line when the bank did not supply one.
/// </summary>
/// <remarks>
/// The reference is the identity of a movement, so it has to be stable: importing the same statement
/// twice may never produce a second movement. Deriving it from the content of the movement keeps the
/// import idempotent, at the price that a statement without references cannot tell two movements
/// apart that share their date, amount, currency and text - those count as one.
/// </remarks>
internal static class SynthetischeReferentie
{
    /// <summary>The prefix that marks a reference as derived rather than supplied by the bank.</summary>
    internal const string Voorvoegsel = "SYN-";

    private const int AantalHexTekens = 12;

    /// <summary>Derives a stable reference from the date, amount and text of a movement.</summary>
    internal static string Van(
        DateOnly datum,
        Bedrag bedrag,
        string? tegenpartij,
        string? iban,
        string? omschrijving)
    {
        var inhoud = string.Join(
            '|',
            datum.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            bedrag.Waarde.ToString("0.00", CultureInfo.InvariantCulture),
            bedrag.Valuta.Code,
            Normaliseer(tegenpartij),
            Normaliseer(iban),
            Normaliseer(omschrijving));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(inhoud));
        var bouwer = new StringBuilder(Voorvoegsel, Voorvoegsel.Length + AantalHexTekens);

        for (var index = 0; index < AantalHexTekens / 2; index++)
        {
            bouwer.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
        }

        return bouwer.ToString();
    }

    /// <summary>Uppercases the text and collapses it to single spaces, so casing cannot split a reference.</summary>
    private static string Normaliseer(string? tekst) =>
        tekst is null
            ? string.Empty
            : string.Join(' ', tekst.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToUpperInvariant();
}
