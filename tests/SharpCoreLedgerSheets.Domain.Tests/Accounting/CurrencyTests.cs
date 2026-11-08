using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Domain.Tests.Accounting;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("EUR", "EUR")]
    [InlineData("eur", "EUR")]
    [InlineData("usd", "USD")]
    [InlineData("GBP", "GBP")]
    [InlineData("chf", "CHF")]
    public void FromCode_ResolvesKnownCurrenciesCaseInsensitive(string code, string expected)
    {
        Assert.Equal(expected, Currency.FromCode(code).Code);
    }

    [Fact]
    public void FromCode_ThrowsForUnknownCurrency()
    {
        Assert.Throws<ArgumentException>(() => Currency.FromCode("XYZ"));
    }

    [Fact]
    public void FromCode_ThrowsForBlankCode()
    {
        Assert.Throws<ArgumentException>(() => Currency.FromCode("  "));
    }

    [Fact]
    public void All_ContainsAtLeastTheDefaultCurrency()
    {
        Assert.Contains(Currency.Eur, Currency.All);
    }
}
