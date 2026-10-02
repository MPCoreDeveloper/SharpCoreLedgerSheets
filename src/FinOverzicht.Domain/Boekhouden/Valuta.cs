namespace FinOverzicht.Domain.Boekhouden;

/// <summary>
/// An ISO-4217 currency used for monetary values.
/// </summary>
/// <param name="Code">The ISO-4217 code, for example <c>EUR</c>.</param>
/// <param name="Symbool">The display symbol, for example <c>€</c>.</param>
/// <param name="Decimalen">The number of decimal places the currency uses.</param>
public sealed record Valuta(string Code, string Symbool, int Decimalen)
{
    /// <summary>Euro (default currency for FinOverzicht).</summary>
    public static readonly Valuta Eur = new("EUR", "\u20AC", 2);

    /// <summary>US dollar.</summary>
    public static readonly Valuta Usd = new("USD", "$", 2);

    /// <summary>British pound sterling.</summary>
    public static readonly Valuta Gbp = new("GBP", "\u00A3", 2);

    /// <summary>Swiss franc.</summary>
    public static readonly Valuta Chf = new("CHF", "CHF", 2);

    /// <summary>All currencies known to the engine.</summary>
    public static IReadOnlyList<Valuta> Alle { get; } = [Eur, Usd, Gbp, Chf];

    /// <summary>
    /// Resolves a currency from its ISO-4217 code (case-insensitive).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the code is not a known currency.</exception>
    public static Valuta VanCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        foreach (var valuta in Alle)
        {
            if (string.Equals(valuta.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return valuta;
            }
        }

        throw new ArgumentException($"Onbekende valuta: '{code}'.", nameof(code));
    }
}
