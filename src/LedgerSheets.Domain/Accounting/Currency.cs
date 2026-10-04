namespace LedgerSheets.Domain.Accounting;

/// <summary>
/// An ISO-4217 currency used for monetary values.
/// </summary>
/// <param name="Code">The ISO-4217 code, for example <c>EUR</c>.</param>
/// <param name="Symbol">The display symbol, for example <c>€</c>.</param>
/// <param name="Decimals">The number of decimal places the currency uses.</param>
public sealed record Currency(string Code, string Symbol, int Decimals)
{
    /// <summary>Euro (default currency for LedgerSheets).</summary>
    public static readonly Currency Eur = new("EUR", "\u20AC", 2);

    /// <summary>US dollar.</summary>
    public static readonly Currency Usd = new("USD", "$", 2);

    /// <summary>British pound sterling.</summary>
    public static readonly Currency Gbp = new("GBP", "\u00A3", 2);

    /// <summary>Swiss franc.</summary>
    public static readonly Currency Chf = new("CHF", "CHF", 2);

    /// <summary>All currencies known to the engine.</summary>
    public static IReadOnlyList<Currency> All { get; } = [Eur, Usd, Gbp, Chf];

    /// <summary>
    /// Resolves a currency from its ISO-4217 code (case-insensitive).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the code is not a known currency.</exception>
    public static Currency FromCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        foreach (var currency in All)
        {
            if (string.Equals(currency.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return currency;
            }
        }

        throw new ArgumentException($"Onbekende valuta: '{code}'.", nameof(code));
    }
}
