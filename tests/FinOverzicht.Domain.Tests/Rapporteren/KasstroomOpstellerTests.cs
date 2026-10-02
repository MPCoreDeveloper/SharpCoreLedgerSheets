using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Services;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Rapporteren;

public sealed class KasstroomOpstellerTests
{
    private readonly KasstroomOpsteller _opsteller = new();
    private readonly Grootboek _grootboek = TestGrootboek.MetScenario();

    [Fact]
    public void StelOp_SplitsCashFlowsIntoThreeCategories()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        overzicht.NettoOperationeel.Should().Be(3_500m);
        overzicht.NettoInvestering.Should().Be(-3_000m);
        overzicht.NettoFinanciering.Should().Be(14_000m);
    }

    [Fact]
    public void StelOp_ReconcilesOpeningAndClosingBalance()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        overzicht.BeginSaldo.Should().Be(0m);
        overzicht.EindSaldo.Should().Be(14_500m);
        overzicht.NettoMutatie.Should().Be(14_500m);
        overzicht.IsInBalans.Should().BeTrue();
    }

    [Fact]
    public void StelOp_ClassifiesExpensePaymentAsOperating()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        overzicht.Operationeel.Should().Contain(r => r.Omschrijving == "Bedrijfskosten betaald" && r.Bedrag == -500m);
        overzicht.Operationeel.Should().Contain(r => r.Omschrijving == "Contante verkoop" && r.Bedrag == 4_000m);
    }

    [Fact]
    public void StelOp_ClassifiesAssetPurchaseAsInvesting()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        overzicht.Investering.Should().ContainSingle(r => r.Bedrag == -3_000m);
    }

    [Fact]
    public void StelOp_ClassifiesCapitalAndLoansAsFinancing()
    {
        var overzicht = _opsteller.StelOp(_grootboek, TestGrootboek.Jaar2026);

        overzicht.Financiering.Should().Contain(r => r.Bedrag == 10_000m);
        overzicht.Financiering.Should().Contain(r => r.Bedrag == 5_000m);
        overzicht.Financiering.Should().Contain(r => r.Bedrag == -1_000m);
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

        overzicht.Operationeel.Should().BeEmpty();
        overzicht.Investering.Should().BeEmpty();
        overzicht.Financiering.Should().BeEmpty();
        overzicht.IsInBalans.Should().BeTrue();
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

        overzicht.BeginSaldo.Should().Be(800m);
        overzicht.EindSaldo.Should().Be(1_000m);
        overzicht.NettoMutatie.Should().Be(200m);
    }

    [Fact]
    public void StelOp_ThrowsForNullLedger()
    {
        var act = () => _opsteller.StelOp(null!, TestGrootboek.Jaar2026);

        act.Should().Throw<ArgumentNullException>();
    }
}
