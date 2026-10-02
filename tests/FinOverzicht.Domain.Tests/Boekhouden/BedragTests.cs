using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class BedragTests
{
    [Fact]
    public void Constructor_RoundsToCurrencyDecimals()
    {
        var bedrag = new Bedrag(10.005m, Valuta.Eur);

        Assert.Equal(10.01m, bedrag.Waarde);
    }

    [Fact]
    public void Constructor_ThrowsWhenCurrencyIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Bedrag(1m, null!));
    }

    [Fact]
    public void Addition_AddsValuesInSameCurrency()
    {
        var resultaat = new Bedrag(10.25m, Valuta.Eur) + new Bedrag(2.75m, Valuta.Eur);

        Assert.Equal(13.00m, resultaat.Waarde);
        Assert.Equal(Valuta.Eur, resultaat.Valuta);
    }

    [Fact]
    public void Subtraction_SubtractsValuesInSameCurrency()
    {
        var resultaat = new Bedrag(10m, Valuta.Eur) - new Bedrag(4.50m, Valuta.Eur);

        Assert.Equal(5.50m, resultaat.Waarde);
    }

    [Fact]
    public void Multiplication_ScalesValue()
    {
        var resultaat = new Bedrag(10m, Valuta.Eur) * 2.5m;

        Assert.Equal(25.00m, resultaat.Waarde);
    }

    [Fact]
    public void Negation_FlipsSign()
    {
        var resultaat = -new Bedrag(10m, Valuta.Eur);

        Assert.Equal(-10m, resultaat.Waarde);
    }

    [Fact]
    public void Operators_ThrowWhenCurrenciesDiffer()
    {
        var exceptie = Assert.Throws<DomeinException>(
            () => new Bedrag(1m, Valuta.Eur) + new Bedrag(1m, Valuta.Usd));

        Assert.Contains("Valuta's komen niet overeen", exceptie.Message);
    }

    [Fact]
    public void Comparison_ComparesValuesInSameCurrency()
    {
        var groot = new Bedrag(10m, Valuta.Eur);
        var klein = new Bedrag(5m, Valuta.Eur);

        Assert.True(groot > klein);
        Assert.True(klein < groot);
        Assert.True(groot >= klein);
        Assert.True(klein <= groot);
    }

    [Fact]
    public void Nul_IsZeroInGivenCurrency()
    {
        var nul = Bedrag.Nul(Valuta.Eur);

        Assert.True(nul.IsNul);
        Assert.False(nul.IsPositief);
        Assert.False(nul.IsNegatief);
    }

    [Fact]
    public void ToString_ContainsCurrencyCode()
    {
        var bedrag = new Bedrag(12.5m, Valuta.Eur);

        Assert.Contains("EUR", bedrag.ToString());
    }
}
