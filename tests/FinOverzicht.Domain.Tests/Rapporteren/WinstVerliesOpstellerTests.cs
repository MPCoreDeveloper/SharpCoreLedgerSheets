using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Services;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Rapporteren;

public sealed class WinstVerliesOpstellerTests
{
    private readonly WinstVerliesOpsteller _opsteller = new();
    private readonly Grootboek _grootboek = TestGrootboek.MetScenario();

    [Fact]
    public void StelOp_ComputesRevenueCostsAndResult()
    {
        var rapport = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        rapport.TotaalOpbrengsten.Should().Be(4_000m);
        rapport.TotaalKosten.Should().Be(1_700m);
        rapport.Resultaat.Should().Be(2_300m);
        rapport.IsWinst.Should().BeTrue();
        rapport.IsVerlies.Should().BeFalse();
    }

    [Fact]
    public void StelOp_PresentsRevenueAndCostsAsPositiveAmounts()
    {
        var rapport = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        rapport.Opbrengsten.Should().ContainSingle(r => r.Nummer == TestGrootboek.Omzet && r.Bedrag == 4_000m);
        rapport.Kosten.Should().ContainSingle(r => r.Nummer == TestGrootboek.Kosten && r.Bedrag == 1_700m);
    }

    [Fact]
    public void StelOp_ExcludesBalanceSheetAccounts()
    {
        var rapport = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        rapport.Opbrengsten.Should().OnlyContain(r => r.Nummer == TestGrootboek.Omzet);
        rapport.Kosten.Should().OnlyContain(r => r.Nummer == TestGrootboek.Kosten);
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

        rapport.TotaalOpbrengsten.Should().Be(100m);
    }

    [Fact]
    public void StelOp_ReportsLossWhenCostsExceedRevenue()
    {
        var grootboek = new Grootboek();
        var kas = grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var kosten = grootboek.VoegRekeningToe("4000", "Kosten", RekeningSoort.Kosten);
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 6, 1), "Kosten").Debet(kosten.Id, 500m).Credit(kas.Id, 500m));

        var rapport = _opsteller.StelOp(grootboek, Periode.VoorJaar(2026));

        rapport.Resultaat.Should().Be(-500m);
        rapport.IsVerlies.Should().BeTrue();
        rapport.NettoMarge.Should().BeNull();
    }

    [Fact]
    public void StelOp_ThrowsForNullLedger()
    {
        var act = () => _opsteller.StelOp(null!, TestGrootboek.Jaar2026);

        act.Should().Throw<ArgumentNullException>();
    }
}
