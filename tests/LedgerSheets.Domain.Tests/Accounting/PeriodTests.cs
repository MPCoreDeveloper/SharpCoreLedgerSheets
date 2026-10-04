using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Tests.Accounting;

public sealed class PeriodTests
{
    [Fact]
    public void Constructor_ThrowsWhenEndPrecedesStart()
    {
        Assert.Throws<DomainException>(() => new Period(new DateOnly(2026, 6, 1), new DateOnly(2026, 5, 31)));
    }

    [Fact]
    public void Constructor_AllowsSingleDayPeriod()
    {
        var date = new DateOnly(2026, 6, 1);
        var period = new Period(date, date);

        Assert.Equal(1, period.CountDays);
    }

    [Fact]
    public void Contains_IsInclusiveOnBothEnds()
    {
        var period = Period.ForYear(2026);

        Assert.True(period.Contains(new DateOnly(2026, 1, 1)));
        Assert.True(period.Contains(new DateOnly(2026, 12, 31)));
        Assert.False(period.Contains(new DateOnly(2025, 12, 31)));
        Assert.False(period.Contains(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void ForYear_Covers365DaysInNonLeapYear()
    {
        Assert.Equal(365, Period.ForYear(2026).CountDays);
    }

    [Fact]
    public void ForYear_Covers366DaysInLeapYear()
    {
        Assert.Equal(366, Period.ForYear(2024).CountDays);
    }

    [Fact]
    public void Year_ReturnsStartYear()
    {
        Assert.Equal(2026, Period.ForYear(2026).Year);
    }
}
