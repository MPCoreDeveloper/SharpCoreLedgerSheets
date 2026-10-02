using FinOverzicht.Domain.Services;

namespace FinOverzicht.Domain.Tests.Rapporteren;

public sealed class BalansOpstellerTests
{
    private readonly BalansOpsteller _opsteller = new();
    private readonly Domain.Boekhouden.Grootboek _grootboek = TestGrootboek.MetScenario();

    [Fact]
    public void StelOp_TotalsMatchScenario()
    {
        var balans = _opsteller.StelOp(_grootboek, TestGrootboek.Datum);

        Assert.Equal(18_300m, balans.TotaalActiva);
        Assert.Equal(6_000m, balans.TotaalPassiva);
        Assert.Equal(10_000m, balans.TotaalEigenVermogen);
        Assert.Equal(2_300m, balans.Resultaat);
    }

    [Fact]
    public void StelOp_Balances()
    {
        var balans = _opsteller.StelOp(_grootboek, TestGrootboek.Datum);

        Assert.True(balans.IsInBalans);
        Assert.Equal(0m, balans.Verschil);
        Assert.Equal(balans.TotaalCreditZijde, balans.TotaalActiva);
    }

    [Fact]
    public void StelOp_PresentsCreditAccountsAsPositiveAmounts()
    {
        var balans = _opsteller.StelOp(_grootboek, TestGrootboek.Datum);

        Assert.Equal(2_000m, balans.Passiva.Single(r => r.Nummer == TestGrootboek.Crediteuren).Bedrag);
        Assert.Equal(4_000m, balans.Passiva.Single(r => r.Nummer == TestGrootboek.Banklening).Bedrag);
        Assert.Equal(10_000m, balans.EigenVermogen.Single().Bedrag);
    }

    [Fact]
    public void StelOp_CopiesClassificationFlags()
    {
        var balans = _opsteller.StelOp(_grootboek, TestGrootboek.Datum);

        Assert.True(balans.Activa.Single(r => r.Nummer == TestGrootboek.Kas).LiquideMiddel);
        Assert.True(balans.Activa.Single(r => r.Nummer == TestGrootboek.Voorraad).Voorraad);
        Assert.True(balans.Passiva.Single(r => r.Nummer == TestGrootboek.Crediteuren).Vlottend);
        Assert.False(balans.Activa.Single(r => r.Nummer == TestGrootboek.Machines).Vlottend);
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

        Assert.Single(balans.Activa, r => r.Nummer == kas.Nummer);
        Assert.DoesNotContain(balans.Activa, r => r.Nummer == bank.Nummer);
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

        Assert.Equal(0m, voor.TotaalActiva);
        Assert.Equal(1_000m, na.TotaalActiva);
    }

    [Fact]
    public void StelOp_ThrowsForNullLedger()
    {
        Assert.Throws<ArgumentNullException>(() => _opsteller.StelOp(null!, TestGrootboek.Datum));
    }
}
