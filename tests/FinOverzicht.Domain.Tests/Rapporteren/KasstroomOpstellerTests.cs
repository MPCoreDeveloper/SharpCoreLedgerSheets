using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Services;

namespace FinOverzicht.Domain.Tests.Rapporteren;

public sealed class KasstroomOpstellerTests
{
    private readonly KasstroomOpsteller _opsteller = new();
    private readonly Grootboek _grootboek = TestGrootboek.MetScenario();

    [Fact]
    public void StelOp_SplitsCashFlowsIntoThreeCategories()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        Assert.Equal(3_500m, overzicht.NettoOperationeel);
        Assert.Equal(-3_000m, overzicht.NettoInvestering);
        Assert.Equal(14_000m, overzicht.NettoFinanciering);
    }

    [Fact]
    public void StelOp_ReconcilesOpeningAndClosingBalance()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        Assert.Equal(0m, overzicht.BeginSaldo);
        Assert.Equal(14_500m, overzicht.EindSaldo);
        Assert.Equal(14_500m, overzicht.NettoMutatie);
        Assert.True(overzicht.IsInBalans);
    }

    [Fact]
    public void StelOp_ClassifiesExpensePaymentAsOperating()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        Assert.Contains(overzicht.Operationeel, r => r.Omschrijving == "Bedrijfskosten betaald" && r.Bedrag == -500m);
        Assert.Contains(overzicht.Operationeel, r => r.Omschrijving == "Contante verkoop" && r.Bedrag == 4_000m);
    }

    [Fact]
    public void StelOp_ClassifiesAssetPurchaseAsInvesting()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        Assert.Single(overzicht.Investering, r => r.Bedrag == -3_000m);
    }

    [Fact]
    public void StelOp_ClassifiesCapitalAndLoansAsFinancing()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        Assert.Contains(overzicht.Financiering, r => r.Bedrag == 10_000m);
        Assert.Contains(overzicht.Financiering, r => r.Bedrag == 5_000m);
        Assert.Contains(overzicht.Financiering, r => r.Bedrag == -1_000m);
    }

    [Fact]
    public void StelOp_IgnoresEntriesWithoutCashMovement()
    {
        var grootboek = new Grootboek();
        var voorraad = grootboek.VoegRekeningToe("1300", "Voorraad", RekeningSoort.Activa, isVlottend: true, isVoorraad: true);
        var crediteuren = grootboek.VoegRekeningToe("2000", "Crediteuren", RekeningSoort.Passiva, isVlottend: true);
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 3, 1), "Inkoop op rekening")
            .Debet(voorraad.Id, 750m).Credit(crediteuren.Id, 750m));

        var overzicht = _opsteller.StelOp(grootboek, Periode.VoorJaar(2026));

        Assert.Empty(overzicht.Operationeel);
        Assert.Empty(overzicht.Investering);
        Assert.Empty(overzicht.Financiering);
        Assert.True(overzicht.IsInBalans);
    }

    [Fact]
    public void StelOp_UsesOpeningBalanceFromPreviousPeriod()
    {
        var grootboek = new Grootboek();
        var kas = grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var omzet = grootboek.VoegRekeningToe("8000", "Omzet", RekeningSoort.Opbrengsten);
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2025, 6, 1), "Verkoop 2025").Debet(kas.Id, 800m).Credit(omzet.Id, 800m));
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 6, 1), "Verkoop 2026").Debet(kas.Id, 200m).Credit(omzet.Id, 200m));

        var overzicht = _opsteller.StelOp(grootboek, Periode.VoorJaar(2026));

        Assert.Equal(800m, overzicht.BeginSaldo);
        Assert.Equal(1_000m, overzicht.EindSaldo);
        Assert.Equal(200m, overzicht.NettoMutatie);
    }

    [Fact]
    public void StelOp_ThrowsForNullLedger()
    {
        Assert.Throws<ArgumentNullException>(() => _opsteller.StelOp(null!, TestGrootboek.Jaar2026));
    }
}
