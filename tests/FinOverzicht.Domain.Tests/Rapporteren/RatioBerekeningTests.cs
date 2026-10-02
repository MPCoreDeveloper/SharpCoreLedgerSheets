using FinOverzicht.Domain.Services;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Rapporteren;

public sealed class RatioBerekeningTests
{
    private readonly BalansOpsteller _balansOpsteller = new();
    private readonly WinstVerliesOpsteller _winstVerliesOpsteller = new();
    private readonly RatioBerekening _berekening = new();

    [Fact]
    public void Bereken_ComputesAllRatiosForScenario()
    {
        var grootboek = TestGrootboek.MetScenario();
        var balans = _balansOpsteller.StelOp(grootboek, TestGrootboek.Datum);
        var winstVerlies = _winstVerliesOpsteller.StelOp(grootboek, TestGrootboek.Jaar2026);

        var ratios = _berekening.Bereken(balans, winstVerlies);

        ratios.CurrentRatio.Should().Be(7.65m);
        ratios.QuickRatio.Should().Be(7.25m);
        ratios.Solvabiliteit.Should().Be(0.6721m);
        ratios.NettoWinstmarge.Should().Be(0.575m);
        ratios.RentabiliteitEigenVermogen.Should().Be(0.1870m);
    }

    [Fact]
    public void Bereken_ReturnsNullWhenDenominatorIsZero()
    {
        var grootboek = new Domain.Boekhouden.Grootboek();
        grootboek.VoegRekeningToe("1000", "Kas", Domain.Boekhouden.RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);

        var balans = _balansOpsteller.StelOp(grootboek, TestGrootboek.Datum);
        var winstVerlies = _winstVerliesOpsteller.StelOp(grootboek, TestGrootboek.Jaar2026);

        var ratios = _berekening.Bereken(balans, winstVerlies);

        ratios.CurrentRatio.Should().BeNull();
        ratios.Solvabiliteit.Should().BeNull();
        ratios.NettoWinstmarge.Should().BeNull();
        ratios.RentabiliteitEigenVermogen.Should().BeNull();
    }

    [Fact]
    public void Bereken_QuickRatioIsLowerWhenInventoryPresent()
    {
        var grootboek = TestGrootboek.MetScenario();
        var balans = _balansOpsteller.StelOp(grootboek, TestGrootboek.Datum);
        var winstVerlies = _winstVerliesOpsteller.StelOp(grootboek, TestGrootboek.Jaar2026);

        var ratios = _berekening.Bereken(balans, winstVerlies);

        ratios.QuickRatio.Should().BeLessThan(ratios.CurrentRatio!.Value);
    }

    [Fact]
    public void Leeg_HasNoValues()
    {
        var leeg = Domain.Rapporteren.RatioOverzicht.Leeg;

        leeg.CurrentRatio.Should().BeNull();
        leeg.QuickRatio.Should().BeNull();
        leeg.Solvabiliteit.Should().BeNull();
        leeg.NettoWinstmarge.Should().BeNull();
        leeg.RentabiliteitEigenVermogen.Should().BeNull();
    }

    [Fact]
    public void Bereken_ThrowsForNullArguments()
    {
        var balans = _balansOpsteller.StelOp(TestGrootboek.MetScenario(), TestGrootboek.Datum);
        var winstVerlies = _winstVerliesOpsteller.StelOp(TestGrootboek.MetScenario(), TestGrootboek.Jaar2026);

        var act1 = () => _berekening.Bereken(null!, winstVerlies);
        var act2 = () => _berekening.Bereken(balans, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }
}
