using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Services;

namespace FinOverzicht.Domain.Tests.Rapporteren;

public sealed class WinstVerliesOpstellerTests
{
    private readonly WinstVerliesOpsteller _opsteller = new();
    private readonly Grootboek _grootboek = TestGrootboek.MetScenario();

    [Fact]
    public void StelOp_ComputesRevenueCostsAndResult()
    {
        var rapport = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        Assert.Equal(4_000m, rapport.TotaalOpbrengsten);
        Assert.Equal(1_700m, rapport.TotaalKosten);
        Assert.Equal(2_300m, rapport.Resultaat);
        Assert.True(rapport.IsWinst);
        Assert.False(rapport.IsVerlies);
    }

    [Fact]
    public void StelOp_PresentsRevenueAndCostsAsPositiveAmounts()
    {
        var rapport = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        Assert.Single(rapport.Opbrengsten, r => r.Nummer == TestGrootboek.Omzet && r.Bedrag == 4_000m);
        Assert.Single(rapport.Kosten, r => r.Nummer == TestGrootboek.Kosten && r.Bedrag == 1_700m);
    }

    [Fact]
    public void StelOp_ExcludesBalanceSheetAccounts()
    {
        var rapport = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        Assert.All(rapport.Opbrengsten, r => Assert.Equal(TestGrootboek.Omzet, r.Nummer));
        Assert.All(rapport.Kosten, r => Assert.Equal(TestGrootboek.Kosten, r.Nummer));
    }

    [Fact]
    public void StelOp_OnlyCountsEntriesInsidePeriod()
    {
        var grootboek = new Grootboek();
        var kas = grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var omzet = grootboek.VoegRekeningToe("8000", "Omzet", RekeningSoort.Opbrengsten);
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2025, 12, 31), "Vorig jaar").Debet(kas.Id, 900m).Credit(omzet.Id, 900m));
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 6, 1), "Dit jaar").Debet(kas.Id, 100m).Credit(omzet.Id, 100m));

        var rapport = _opsteller.StelOp(grootboek, Periode.VoorJaar(2026));

        Assert.Equal(100m, rapport.TotaalOpbrengsten);
    }

    [Fact]
    public void StelOp_ReportsLossWhenCostsExceedRevenue()
    {
        var grootboek = new Grootboek();
        var kas = grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var kosten = grootboek.VoegRekeningToe("4000", "Kosten", RekeningSoort.Kosten);
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 6, 1), "Kosten").Debet(kosten.Id, 500m).Credit(kas.Id, 500m));

        var rapport = _opsteller.StelOp(grootboek, Periode.VoorJaar(2026));

        Assert.Equal(-500m, rapport.Resultaat);
        Assert.True(rapport.IsVerlies);
        Assert.Null(rapport.NettoMarge);
    }

    [Fact]
    public void StelOp_ThrowsForNullLedger()
    {
        Assert.Throws<ArgumentNullException>(() => _opsteller.StelOp(null!, TestGrootboek.Jaar2026));
    }
}
