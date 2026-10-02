using FinOverzicht.Domain.Services;

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

        Assert.Equal(7.65m, ratios.CurrentRatio);
        Assert.Equal(7.25m, ratios.QuickRatio);
        Assert.Equal(0.6721m, ratios.Solvabiliteit);
        Assert.Equal(0.575m, ratios.NettoWinstmarge);
        Assert.Equal(0.1870m, ratios.RentabiliteitEigenVermogen);
    }

    [Fact]
    public void Bereken_ReturnsNullWhenDenominatorIsZero()
    {
        var grootboek = new Domain.Boekhouden.Grootboek();
        grootboek.VoegRekeningToe("1000", "Kas", Domain.Boekhouden.RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);

        var balans = _balansOpsteller.StelOp(grootboek, TestGrootboek.Datum);
        var winstVerlies = _winstVerliesOpsteller.StelOp(grootboek, TestGrootboek.Jaar2026);

        var ratios = _berekening.Bereken(balans, winstVerlies);

        Assert.Null(ratios.CurrentRatio);
        Assert.Null(ratios.Solvabiliteit);
        Assert.Null(ratios.NettoWinstmarge);
        Assert.Null(ratios.RentabiliteitEigenVermogen);
    }

    [Fact]
    public void Bereken_QuickRatioIsLowerWhenInventoryPresent()
    {
        var grootboek = TestGrootboek.MetScenario();
        var balans = _balansOpsteller.StelOp(grootboek, TestGrootboek.Datum);
        var winstVerlies = _winstVerliesOpsteller.StelOp(grootboek, TestGrootboek.Jaar2026);

        var ratios = _berekening.Bereken(balans, winstVerlies);

        Assert.True(ratios.QuickRatio < ratios.CurrentRatio!.Value);
    }

    [Fact]
    public void Leeg_HasNoValues()
    {
        var leeg = Domain.Rapporteren.RatioOverzicht.Leeg;

        Assert.Null(leeg.CurrentRatio);
        Assert.Null(leeg.QuickRatio);
        Assert.Null(leeg.Solvabiliteit);
        Assert.Null(leeg.NettoWinstmarge);
        Assert.Null(leeg.RentabiliteitEigenVermogen);
    }

    [Fact]
    public void Bereken_ThrowsForNullArguments()
    {
        var balans = _balansOpsteller.StelOp(TestGrootboek.MetScenario(), TestGrootboek.Datum);
        var winstVerlies = _winstVerliesOpsteller.StelOp(TestGrootboek.MetScenario(), TestGrootboek.Jaar2026);

        Assert.Throws<ArgumentNullException>(() => _berekening.Bereken(null!, winstVerlies));
        Assert.Throws<ArgumentNullException>(() => _berekening.Bereken(balans, null!));
    }
}
