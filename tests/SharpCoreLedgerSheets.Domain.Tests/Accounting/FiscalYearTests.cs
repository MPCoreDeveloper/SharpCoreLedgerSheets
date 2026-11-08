using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Domain.Tests.Accounting;

/// <summary>
/// A broken book year is normal in the Dutch MKB, so the arithmetic of a shifted year is checked here:
/// the label, both boundaries and the leap-year case.
/// </summary>
public sealed class FiscalYearTests
{
    [Fact]
    public void ForFiscalYear_InJanuary_IsTheCalendarYear()
    {
        var period = Period.ForFiscalYear(2026, 1);

        Assert.Equal(new DateOnly(2026, 1, 1), period.Start);
        Assert.Equal(new DateOnly(2026, 12, 31), period.Closing);
        Assert.Equal(2026, period.Year);
    }

    [Fact]
    public void ForFiscalYear_InFebruary_RunsUntilTheEndOfJanuary()
    {
        var period = Period.ForFiscalYear(2026, 2);

        Assert.Equal(new DateOnly(2026, 2, 1), period.Start);
        Assert.Equal(new DateOnly(2027, 1, 31), period.Closing);
        Assert.Equal(2026, period.Year);
        Assert.Equal(365, period.CountDays);
    }

    [Fact]
    public void ForFiscalYear_InDecember_CrossesIntoTheNextYear()
    {
        var period = Period.ForFiscalYear(2026, 12);

        Assert.Equal(new DateOnly(2026, 12, 1), period.Start);
        Assert.Equal(new DateOnly(2027, 11, 30), period.Closing);
        Assert.Equal(365, period.CountDays);
    }

    [Fact]
    public void ForFiscalYear_StartingInALeapYearMonth_CountsFebruary29()
    {
        // Book year 2027 starts in March 2027 and ends in February 2028, which has 29 days.
        var period = Period.ForFiscalYear(2027, 3);

        Assert.Equal(new DateOnly(2028, 2, 29), period.Closing);
        Assert.Equal(366, period.CountDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void ForFiscalYear_RejectsAMonthOutsideTheYear(int month)
    {
        var exception = Assert.Throws<DomainException>(() => Period.ForFiscalYear(2026, month));

        Assert.Equal(MessageCode.FiscalYearMonthInvalid, exception.Code);
    }

    [Fact]
    public void ForFiscalYear_ContainsOnlyItsOwnDays()
    {
        var period = Period.ForFiscalYear(2026, 2);

        Assert.True(period.Contains(new DateOnly(2026, 2, 1)));
        Assert.True(period.Contains(new DateOnly(2027, 1, 31)));
        Assert.False(period.Contains(new DateOnly(2026, 1, 31)));
        Assert.False(period.Contains(new DateOnly(2027, 2, 1)));
    }
}
