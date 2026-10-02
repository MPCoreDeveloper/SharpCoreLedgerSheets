using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class BedragTests
{
    [Fact]
    public void Constructor_RoundsToCurrencyDecimals()
    {
        var bedrag = new Bedrag(10.005m, Valuta.Eur);

        bedrag.Waarde.Should().Be(10.01m);
    }

    [Fact]
    public void Constructor_ThrowsWhenCurrencyIsNull()
    {
        var act = () => new Bedrag(1m, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Addition_AddsValuesInSameCurrency()
    {
        var resultaat = new Bedrag(10.25m, Valuta.Eur) + new Bedrag(2.75m, Valuta.Eur);

        resultaat.Waarde.Should().Be(13.00m);
        resultaat.Valuta.Should().Be(Valuta.Eur);
    }

    [Fact]
    public void Subtraction_SubtractsValuesInSameCurrency()
    {
        var resultaat = new Bedrag(10m, Valuta.Eur) - new Bedrag(4.50m, Valuta.Eur);

        resultaat.Waarde.Should().Be(5.50m);
    }

    [Fact]
    public void Multiplication_ScalesValue()
    {
        var resultaat = new Bedrag(10m, Valuta.Eur) * 2.5m;

        resultaat.Waarde.Should().Be(25.00m);
    }

    [Fact]
    public void Negation_FlipsSign()
    {
        var resultaat = -new Bedrag(10m, Valuta.Eur);

        resultaat.Waarde.Should().Be(-10m);
    }

    [Fact]
    public void Operators_ThrowWhenCurrenciesDiffer()
    {
        var act = () => new Bedrag(1m, Valuta.Eur) + new Bedrag(1m, Valuta.Usd);

        act.Should().Throw<DomeinException>().WithMessage("*Valuta's komen niet overeen*");
    }

    [Fact]
    public void Comparison_ComparesValuesInSameCurrency()
    {
        var groot = new Bedrag(10m, Valuta.Eur);
        var klein = new Bedrag(5m, Valuta.Eur);

        (groot > klein).Should().BeTrue();
        (klein < groot).Should().BeTrue();
        (groot >= klein).Should().BeTrue();
        (klein <= groot).Should().BeTrue();
    }

    [Fact]
    public void Nul_IsZeroInGivenCurrency()
    {
        var nul = Bedrag.Nul(Valuta.Eur);

        nul.IsNul.Should().BeTrue();
        nul.IsPositief.Should().BeFalse();
        nul.IsNegatief.Should().BeFalse();
    }

    [Fact]
    public void ToString_ContainsCurrencyCode()
    {
        var bedrag = new Bedrag(12.5m, Valuta.Eur);

        bedrag.ToString().Should().Contain("EUR");
    }
}
