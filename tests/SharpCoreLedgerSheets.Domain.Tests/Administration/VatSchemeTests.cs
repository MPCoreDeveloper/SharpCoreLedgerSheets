using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Domain.Tests.Administration;

public sealed class VatSchemeTests
{
    [Fact]
    public void Dutch_OffersTheStandardAndReducedRatePlusZero()
    {
        var codes = VatScheme.Dutch.Rates.Select(rate => rate.Code).ToList();

        Assert.Equal(["21", "9", "0"], codes);
        Assert.False(VatScheme.Dutch.IsEmpty);
    }

    [Fact]
    public void Dutch_BooksTheStandardRateOnItsOwnAccounts()
    {
        var rate = VatScheme.Dutch.Require("21");

        Assert.Equal(21m, rate.Percentage);
        Assert.True(rate.BooksVat);
        Assert.Equal("2100", rate.OutputAccountNumber);
        Assert.Equal("1110", rate.InputAccountNumber);
    }

    [Fact]
    public void Dutch_ZeroRate_HasNoVatLeg()
    {
        var rate = VatScheme.Dutch.Require("0");

        Assert.False(rate.BooksVat);
        Assert.Null(rate.OutputAccountNumber);
        Assert.Null(rate.InputAccountNumber);
    }

    [Fact]
    public void Find_IgnoresCasing()
    {
        Assert.NotNull(VatScheme.Dutch.Find("21"));
        Assert.NotNull(VatScheme.Dutch.Find("9"));
        Assert.Null(VatScheme.Dutch.Find("6"));
    }

    [Fact]
    public void Require_ExplainsWhichRatesExist()
    {
        var exception = Assert.Throws<DomainException>(() => VatScheme.Dutch.Require("6"));

        Assert.Equal(MessageCode.VatRateUnknown, exception.Code);
        Assert.Contains("21", exception.Message);
    }

    [Fact]
    public void Constructor_RejectsDuplicateCodes()
    {
        var exception = Assert.Throws<DomainException>(() => new VatScheme(
        [
            new VatRate("21", "21%", 21m, "2100", "1110"),
            new VatRate("21", "eenentwintig", 21m, "2199", "1199"),
        ]));

        Assert.Equal(MessageCode.VatRateDuplicate, exception.Code);
    }

    [Fact]
    public void None_IsEmpty()
    {
        Assert.True(VatScheme.None.IsEmpty);
        Assert.Empty(VatScheme.None.Rates);
    }
}
