using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Tests.Accounting;

public sealed class AmountTests
{
    [Fact]
    public void Constructor_RoundsToCurrencyDecimals()
    {
        var amount = new Amount(10.005m, Currency.Eur);

        Assert.Equal(10.01m, amount.Value);
    }

    [Fact]
    public void Constructor_ThrowsWhenCurrencyIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Amount(1m, null!));
    }

    [Fact]
    public void Addition_AddsValuesInSameCurrency()
    {
        var result = new Amount(10.25m, Currency.Eur) + new Amount(2.75m, Currency.Eur);

        Assert.Equal(13.00m, result.Value);
        Assert.Equal(Currency.Eur, result.Currency);
    }

    [Fact]
    public void Subtraction_SubtractsValuesInSameCurrency()
    {
        var result = new Amount(10m, Currency.Eur) - new Amount(4.50m, Currency.Eur);

        Assert.Equal(5.50m, result.Value);
    }

    [Fact]
    public void Multiplication_ScalesValue()
    {
        var result = new Amount(10m, Currency.Eur) * 2.5m;

        Assert.Equal(25.00m, result.Value);
    }

    [Fact]
    public void Negation_FlipsSign()
    {
        var result = -new Amount(10m, Currency.Eur);

        Assert.Equal(-10m, result.Value);
    }

    [Fact]
    public void Operators_ThrowWhenCurrenciesDiffer()
    {
        var exception = Assert.Throws<DomainException>(
            () => new Amount(1m, Currency.Eur) + new Amount(1m, Currency.Usd));

        Assert.Contains("Valuta's komen niet overeen", exception.Message);
    }

    [Fact]
    public void Comparison_ComparesValuesInSameCurrency()
    {
        var big = new Amount(10m, Currency.Eur);
        var small = new Amount(5m, Currency.Eur);

        Assert.True(big > small);
        Assert.True(small < big);
        Assert.True(big >= small);
        Assert.True(small <= big);
    }

    [Fact]
    public void Nul_IsZeroInGivenCurrency()
    {
        var nul = Amount.Nul(Currency.Eur);

        Assert.True(nul.IsNul);
        Assert.False(nul.IsPositive);
        Assert.False(nul.IsNegative);
    }

    [Fact]
    public void ToString_ContainsCurrencyCode()
    {
        var amount = new Amount(12.5m, Currency.Eur);

        Assert.Contains("EUR", amount.ToString());
    }
}
