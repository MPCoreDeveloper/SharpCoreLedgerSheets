using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Domain.Tests.Administration;

public sealed class AdministrationSettingsTests
{
    [Fact]
    public void Default_IsACalendarYearEuroAdministration()
    {
        var settings = AdministrationSettings.Default;

        Assert.Equal(AdministrationSettings.DefaultCompanyName, settings.CompanyName);
        Assert.Equal(1, settings.FiscalYearStartMonth);
        Assert.True(settings.UsesCalendarYear);
        Assert.Equal(Currency.Eur, settings.Currency);
        Assert.False(settings.Vat.IsEmpty);
    }

    [Fact]
    public void Constructor_TrimsTheCompanyName()
    {
        var settings = new AdministrationSettings("  Sanne Consulting  ");

        Assert.Equal("Sanne Consulting", settings.CompanyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsABlankCompanyName(string name)
    {
        var exception = Assert.Throws<DomainException>(() => new AdministrationSettings(name));

        Assert.Equal(MessageCode.CompanyNameRequired, exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Constructor_RejectsAnImpossibleStartMonth(int month)
    {
        var exception = Assert.Throws<DomainException>(() => new AdministrationSettings("Sanne", month));

        Assert.Equal(MessageCode.FiscalYearMonthInvalid, exception.Code);
    }

    [Fact]
    public void PeriodFor_FollowsTheConfiguredStartMonth()
    {
        var settings = new AdministrationSettings("Bram Retail", fiscalYearStartMonth: 4);

        var period = settings.PeriodFor(2026);

        Assert.Equal(new DateOnly(2026, 4, 1), period.Start);
        Assert.Equal(new DateOnly(2027, 3, 31), period.Closing);
        Assert.False(settings.UsesCalendarYear);
    }

    [Fact]
    public void WithCompanyName_KeepsTheOtherSettings()
    {
        var settings = new AdministrationSettings("Oud", fiscalYearStartMonth: 7);

        var renamed = settings.WithCompanyName("Nieuw");

        Assert.Equal("Nieuw", renamed.CompanyName);
        Assert.Equal(7, renamed.FiscalYearStartMonth);
        Assert.Equal(settings.Vat, renamed.Vat);
    }

    [Fact]
    public void WithFiscalYearStartMonth_KeepsTheOtherSettings()
    {
        var settings = new AdministrationSettings("Sanne", fiscalYearStartMonth: 1);

        var shifted = settings.WithFiscalYearStartMonth(9);

        Assert.Equal("Sanne", shifted.CompanyName);
        Assert.Equal(9, shifted.FiscalYearStartMonth);
    }

    [Fact]
    public void WithVatScheme_KeepsTheOtherSettings()
    {
        var settings = AdministrationSettings.Default;

        var withoutVat = settings.WithVatScheme(VatScheme.None);

        Assert.True(withoutVat.Vat.IsEmpty);
        Assert.Equal(settings.CompanyName, withoutVat.CompanyName);
    }
}
