using LedgerSheets.Domain.Banking;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Tests.Banking;

public sealed class CounterAccountRuleTests
{
    [Fact]
    public void Keyword_IsNormalized()
    {
        var line = new CounterAccountRule("  albert   heijn ", "4000");

        Assert.Equal("ALBERT HEIJN", line.Keyword);
    }

    [Fact]
    public void CounterAccountNumber_IsTrimmed()
    {
        var line = new CounterAccountRule("ALBERT HEIJN", "  4000  ");

        Assert.Equal("4000", line.CounterAccountNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ah")]
    public void TooShortKeyword_IsRejected(string keyword)
    {
        var exception = Assert.Throws<DomainException>(() => new CounterAccountRule(keyword, "4000"));

        Assert.Contains("trefwoord", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithoutCounterAccount_IsRejected(string number)
    {
        var exception = Assert.Throws<DomainException>(() => new CounterAccountRule("ALBERT HEIJN", number));

        Assert.Contains("grootboekrekening", exception.Message);
    }

    [Fact]
    public void WithoutConfirmation_IsRejected()
    {
        var exception = Assert.Throws<DomainException>(() => new CounterAccountRule("ALBERT HEIJN", "4000", 0));

        Assert.Contains("bevestigd", exception.Message);
    }

    [Fact]
    public void Confirmed_HoogtTheNumeratorOn()
    {
        var line = new CounterAccountRule("ALBERT HEIJN", "4000");

        var confirmed = line.Confirmed();

        Assert.Equal(1, line.Confirmations);
        Assert.Equal(2, confirmed.Confirmations);
        Assert.Equal(line.Keyword, confirmed.Keyword);
    }

    [Fact]
    public void ForAccount_HoudtKeywordAndConfirmations()
    {
        var line = new CounterAccountRule("ALBERT HEIJN", "4000", 3);

        var moved = line.ForAccount("4100");

        Assert.Equal("4100", moved.CounterAccountNumber);
        Assert.Equal("ALBERT HEIJN", moved.Keyword);
        Assert.Equal(3, moved.Confirmations);
    }
}
