using FinOverzicht.Domain.Boekhouden;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class ValutaTests
{
    [Theory]
    [InlineData("EUR", "EUR")]
    [InlineData("eur", "EUR")]
    [InlineData("usd", "USD")]
    [InlineData("GBP", "GBP")]
    [InlineData("chf", "CHF")]
    public void VanCode_ResolvesKnownCurrenciesCaseInsensitive(string code, string verwacht)
    {
        Valuta.VanCode(code).Code.Should().Be(verwacht);
    }

    [Fact]
    public void VanCode_ThrowsForUnknownCurrency()
    {
        var act = () => Valuta.VanCode("XYZ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VanCode_ThrowsForBlankCode()
    {
        var act = () => Valuta.VanCode("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Alle_ContainsAtLeastTheDefaultCurrency()
    {
        Valuta.Alle.Should().Contain(Valuta.Eur);
    }
}
