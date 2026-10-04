using System.Globalization;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Accounting;

/// <summary>
/// A monetary amount in a specific currency. Values are always rounded to the currency's
/// number of decimals (banker's-free, half-away-from-zero) so comparisons are deterministic.
/// </summary>
/// <remarks>
/// Mixing currencies in arithmetic is a bug, not a feature: every operator validates that both
/// operands use the same currency and throws <see cref="DomainException"/> otherwise.
/// </remarks>
public readonly record struct Amount
{
    /// <summary>Creates an amount, rounded to the currency's decimals.</summary>
    /// <param name="value">The raw value.</param>
    /// <param name="currency">The currency; must not be <see langword="null"/>.</param>
    public Amount(decimal value, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        Value = decimal.Round(value, currency.Decimals, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    /// <summary>The rounded value.</summary>
    public decimal Value { get; }

    /// <summary>The currency of this amount.</summary>
    public Currency Currency { get; }

    /// <summary>True when the amount equals zero.</summary>
    public bool IsNul => Value == 0m;

    /// <summary>True when the amount is greater than zero.</summary>
    public bool IsPositive => Value > 0m;

    /// <summary>True when the amount is less than zero.</summary>
    public bool IsNegative => Value < 0m;

    /// <summary>Returns a zero amount in the given currency.</summary>
    public static Amount Nul(Currency currency) => new(0m, currency);

    public static Amount operator +(Amount left, Amount right)
    {
        CheckSameCurrency(left, right);
        return new Amount(left.Value + right.Value, left.Currency);
    }

    public static Amount operator -(Amount left, Amount right)
    {
        CheckSameCurrency(left, right);
        return new Amount(left.Value - right.Value, left.Currency);
    }

    public static Amount operator -(Amount amount) => new(-amount.Value, amount.Currency);

    public static Amount operator *(Amount amount, decimal factor) => new(amount.Value * factor, amount.Currency);

    public static bool operator >(Amount left, Amount right) => Compare(left, right) > 0;

    public static bool operator <(Amount left, Amount right) => Compare(left, right) < 0;

    public static bool operator >=(Amount left, Amount right) => Compare(left, right) >= 0;

    public static bool operator <=(Amount left, Amount right) => Compare(left, right) <= 0;

    /// <inheritdoc />
    public override string ToString() =>
        $"{Value.ToString("N", CultureInfo.CurrentCulture)} {Currency.Code}";

    private static void CheckSameCurrency(Amount left, Amount right)
    {
        if (left.Currency is null || right.Currency is null)
        {
            throw new DomainException("Een bedrag zonder valuta kan niet worden gebruikt in een berekening.");
        }

        if (left.Currency != right.Currency)
        {
            throw new DomainException(
                $"Valuta's komen niet overeen: {left.Currency.Code} versus {right.Currency.Code}.");
        }
    }

    private static int Compare(Amount left, Amount right)
    {
        CheckSameCurrency(left, right);
        return left.Value.CompareTo(right.Value);
    }
}
