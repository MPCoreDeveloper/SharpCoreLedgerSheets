using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class PeriodeTests
{
    [Fact]
    public void Constructor_ThrowsWhenEndPrecedesStart()
    {
        Assert.Throws<DomeinException>(() => new Periode(new DateOnly(2026, 6, 1), new DateOnly(2026, 5, 31)));
    }

    [Fact]
    public void Constructor_AllowsSingleDayPeriod()
    {
        var datum = new DateOnly(2026, 6, 1);
        var periode = new Periode(datum, datum);

        Assert.Equal(1, periode.AantalDagen);
    }

    [Fact]
    public void Bevat_IsInclusiveOnBothEnds()
    {
        var periode = Periode.VoorJaar(2026);

        Assert.True(periode.Bevat(new DateOnly(2026, 1, 1)));
        Assert.True(periode.Bevat(new DateOnly(2026, 12, 31)));
        Assert.False(periode.Bevat(new DateOnly(2025, 12, 31)));
        Assert.False(periode.Bevat(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void VoorJaar_Covers365DaysInNonLeapYear()
    {
        Assert.Equal(365, Periode.VoorJaar(2026).AantalDagen);
    }

    [Fact]
    public void VoorJaar_Covers366DaysInLeapYear()
    {
        Assert.Equal(366, Periode.VoorJaar(2024).AantalDagen);
    }

    [Fact]
    public void Jaar_ReturnsStartYear()
    {
        Assert.Equal(2026, Periode.VoorJaar(2026).Jaar);
    }
}
