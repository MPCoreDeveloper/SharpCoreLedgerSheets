using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class PeriodeTests
{
    [Fact]
    public void Constructor_ThrowsWhenEndPrecedesStart()
    {
        var act = () => new Periode(new DateOnly(2026, 6, 1), new DateOnly(2026, 5, 31));

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void Constructor_AllowsSingleDayPeriod()
    {
        var datum = new DateOnly(2026, 6, 1);
        var periode = new Periode(datum, datum);

        periode.AantalDagen.Should().Be(1);
    }

    [Fact]
    public void Bevat_IsInclusiveOnBothEnds()
    {
        var periode = Periode.VoorJaar(2026);

        periode.Bevat(new DateOnly(2026, 1, 1)).Should().BeTrue();
        periode.Bevat(new DateOnly(2026, 12, 31)).Should().BeTrue();
        periode.Bevat(new DateOnly(2025, 12, 31)).Should().BeFalse();
        periode.Bevat(new DateOnly(2027, 1, 1)).Should().BeFalse();
    }

    [Fact]
    public void VoorJaar_Covers365DaysInNonLeapYear()
    {
        Periode.VoorJaar(2026).AantalDagen.Should().Be(365);
    }

    [Fact]
    public void VoorJaar_Covers366DaysInLeapYear()
    {
        Periode.VoorJaar(2024).AantalDagen.Should().Be(366);
    }

    [Fact]
    public void Jaar_ReturnsStartYear()
    {
        Periode.VoorJaar(2026).Jaar.Should().Be(2026);
    }
}
