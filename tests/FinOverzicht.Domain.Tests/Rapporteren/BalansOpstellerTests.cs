using FinOverzicht.Domain.Services;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Rapporteren;

public sealed class BalansOpstellerTests
{
    private readonly BalansOpsteller _opsteller = new();
    private readonly Domain.Boekhouden.Grootboek _grootboek = TestGrootboek.MetScenario();

    [Fact]
    public void StelOp_TotalsMatchScenario()
    {
        var balans = _opsteller.StelOp(_grootboek, TestGrootboek.Datum);

        balans.TotaalActiva.Should().Be(18_300m);
        balans.TotaalPassiva.Should().Be(6_000m);
        balans.TotaalEigenVermogen.Should().Be(10_000m);
        balans.Resultaat.Should().Be(2_300m);
    }

    [Fact]
    public void StelOp_Balances()
    {
        var balans = _opsteller.StelOp(_grootboek, TestGrootboek.Datum);

        balans.IsInBalans.Should().BeTrue();
        balans.Verschil.Should().Be(0m);
        balans.TotaalActiva.Should().Be(balans.TotaalCreditZijde);
    }

    [Fact]
    public void StelOp_PresentsCreditAccountsAsPositiveAmounts()
    {
        var balans = _opsteller.StelOp(_grootboek, TestGrootboek.Datum);

        balans.Passiva.Single(r => r.Nummer == TestGrootboek.Crediteuren).Bedrag.Should().Be(2_000m);
        balans.Passiva.Single(r => r.Nummer == TestGrootboek.Banklening).Bedrag.Should().Be(4_000m);
        balans.EigenVermogen.Single().Bedrag.Should().Be(10_000m);
    }

    [Fact]
    public void StelOp_CopiesClassificationFlags()
    {
        var balans = _opsteller.StelOp(_grootboek, TestGrootboek.Datum);

        balans.Activa.Single(r => r.Nummer == TestGrootboek.Kas).LiquideMiddel.Should().BeTrue();
        balans.Activa.Single(r => r.Nummer == TestGrootboek.Voorraad).Voorraad.Should().BeTrue();
        balans.Passiva.Single(r => r.Nummer == TestGrootboek.Crediteuren).Vlottend.Should().BeTrue();
        balans.Activa.Single(r => r.Nummer == TestGrootboek.Machines).Vlottend.Should().BeFalse();
    }

    [Fact]
    public void StelOp_OmitsAccountsWithoutBalance()
    {
        var grootboek = new Domain.Boekhouden.Grootboek();
        var kas = grootboek.VoegRekeningToe("1000", "Kas", Domain.Boekhouden.RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var bank = grootboek.VoegRekeningToe("1100", "Bank", Domain.Boekhouden.RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var omzet = grootboek.VoegRekeningToe("8000", "Omzet", Domain.Boekhouden.RekeningSoort.Opbrengsten);
        grootboek.Boek(FinOverzicht.Domain.Boekhouden.Journaalpost.Nieuw(new DateOnly(2026, 5, 1), "Verkoop")
            .Debet(kas.Id, 500m).Credit(omzet.Id, 500m));

        var balans = _opsteller.StelOp(grootboek, TestGrootboek.Datum);

        balans.Activa.Should().ContainSingle(r => r.Nummer == kas.Nummer);
        balans.Activa.Should().NotContain(r => r.Nummer == bank.Nummer);
    }

    [Fact]
    public void StelOp_RespectsAsOfDate()
    {
        var grootboek = new Domain.Boekhouden.Grootboek();
        var kas = grootboek.VoegRekeningToe("1000", "Kas", Domain.Boekhouden.RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var omzet = grootboek.VoegRekeningToe("8000", "Omzet", Domain.Boekhouden.RekeningSoort.Opbrengsten);
        grootboek.Boek(FinOverzicht.Domain.Boekhouden.Journaalpost.Nieuw(new DateOnly(2026, 5, 1), "Verkoop")
            .Debet(kas.Id, 1_000m).Credit(omzet.Id, 1_000m));

        var voor = _opsteller.StelOp(grootboek, new DateOnly(2026, 4, 30));
        var na = _opsteller.StelOp(grootboek, new DateOnly(2026, 5, 1));

        voor.TotaalActiva.Should().Be(0m);
        na.TotaalActiva.Should().Be(1_000m);
    }

    [Fact]
    public void StelOp_ThrowsForNullLedger()
    {
        var act = () => _opsteller.StelOp(null!, TestGrootboek.Datum);

        act.Should().Throw<ArgumentNullException>();
    }
}
