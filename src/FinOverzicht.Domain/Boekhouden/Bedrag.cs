using System.Globalization;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Boekhouden;

/// <summary>
/// A monetary amount in a specific currency. Values are always rounded to the currency's
/// number of decimals (banker's-free, half-away-from-zero) so comparisons are deterministic.
/// </summary>
/// <remarks>
/// Mixing currencies in arithmetic is a bug, not a feature: every operator validates that both
/// operands use the same currency and throws <see cref="DomeinException"/> otherwise.
/// </remarks>
public readonly record struct Bedrag
{
    /// <summary>Creates an amount, rounded to the currency's decimals.</summary>
    /// <param name="waarde">The raw value.</param>
    /// <param name="valuta">The currency; must not be <see langword="null"/>.</param>
    public Bedrag(decimal waarde, Valuta valuta)
    {
        ArgumentNullException.ThrowIfNull(valuta);
        Waarde = decimal.Round(waarde, valuta.Decimalen, MidpointRounding.AwayFromZero);
        Valuta = valuta;
    }

    /// <summary>The rounded value.</summary>
    public decimal Waarde { get; }

    /// <summary>The currency of this amount.</summary>
    public Valuta Valuta { get; }

    /// <summary>True when the amount equals zero.</summary>
    public bool IsNul => Waarde == 0m;

    /// <summary>True when the amount is greater than zero.</summary>
    public bool IsPositief => Waarde > 0m;

    /// <summary>True when the amount is less than zero.</summary>
    public bool IsNegatief => Waarde < 0m;

    /// <summary>Returns a zero amount in the given currency.</summary>
    public static Bedrag Nul(Valuta valuta) => new(0m, valuta);

    public static Bedrag operator +(Bedrag links, Bedrag rechts)
    {
        ControleerZelfdeValuta(links, rechts);
        return new Bedrag(links.Waarde + rechts.Waarde, links.Valuta);
    }

    public static Bedrag operator -(Bedrag links, Bedrag rechts)
    {
        ControleerZelfdeValuta(links, rechts);
        return new Bedrag(links.Waarde - rechts.Waarde, links.Valuta);
    }

    public static Bedrag operator -(Bedrag bedrag) => new(-bedrag.Waarde, bedrag.Valuta);

    public static Bedrag operator *(Bedrag bedrag, decimal factor) => new(bedrag.Waarde * factor, bedrag.Valuta);

    public static bool operator >(Bedrag links, Bedrag rechts) => Vergelijk(links, rechts) > 0;

    public static bool operator <(Bedrag links, Bedrag rechts) => Vergelijk(links, rechts) < 0;

    public static bool operator >=(Bedrag links, Bedrag rechts) => Vergelijk(links, rechts) >= 0;

    public static bool operator <=(Bedrag links, Bedrag rechts) => Vergelijk(links, rechts) <= 0;

    /// <inheritdoc />
    public override string ToString() =>
        $"{Waarde.ToString("N", CultureInfo.CurrentCulture)} {Valuta.Code}";

    private static void ControleerZelfdeValuta(Bedrag links, Bedrag rechts)
    {
        if (links.Valuta is null || rechts.Valuta is null)
        {
            throw new DomeinException("Een bedrag zonder valuta kan niet worden gebruikt in een berekening.");
        }

        if (links.Valuta != rechts.Valuta)
        {
            throw new DomeinException(
                $"Valuta's komen niet overeen: {links.Valuta.Code} versus {rechts.Valuta.Code}.");
        }
    }

    private static int Vergelijk(Bedrag links, Bedrag rechts)
    {
        ControleerZelfdeValuta(links, rechts);
        return links.Waarde.CompareTo(rechts.Waarde);
    }
}
