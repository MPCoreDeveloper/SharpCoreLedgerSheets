using FinOverzicht.Domain.Boekhouden;

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
        Assert.Equal(verwacht, Valuta.VanCode(code).Code);
    }

    [Fact]
    public void VanCode_ThrowsForUnknownCurrency()
    {
        Assert.Throws<ArgumentException>(() => Valuta.VanCode("XYZ"));
    }

    [Fact]
    public void VanCode_ThrowsForBlankCode()
    {
        Assert.Throws<ArgumentException>(() => Valuta.VanCode("  "));
    }

    [Fact]
    public void Alle_ContainsAtLeastTheDefaultCurrency()
    {
        Assert.Contains(Valuta.Eur, Valuta.Alle);
    }
}
