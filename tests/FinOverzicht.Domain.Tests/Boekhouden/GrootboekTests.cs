using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class GrootboekTests
{
    [Fact]
    public void DefaultsToEuro()
    {
        new Grootboek().Valuta.Should().Be(Valuta.Eur);
    }

    [Fact]
    public void VoegRekeningToe_AddsAccountWithGeneratedId()
    {
        var grootboek = new Grootboek();

        var rekening = grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa);

        rekening.Id.Should().NotBe(Guid.Empty);
        grootboek.Rekeningen.Should().ContainSingle();
    }

    [Fact]
    public void VoegRekeningToe_ThrowsForDuplicateNumberCaseInsensitive()
    {
        var grootboek = new Grootboek();
        grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa);

        var act = () => grootboek.VoegRekeningToe("1000", "Kas 2", RekeningSoort.Activa);

        act.Should().Throw<DomeinException>().WithMessage("*bestaat al*");
    }

    [Fact]
    public void ZoekRekening_FindsByNumberCaseInsensitive()
    {
        var grootboek = new Grootboek();
        var kas = grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa);

        grootboek.ZoekRekening("1000").Should().BeSameAs(kas);
        grootboek.ZoekRekening("1000 ").Should().BeNull();
    }

    [Fact]
    public void Boek_AddsPostAndComputesSignedBalance()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        grootboek.Boek(Journaalpost.Nieuw(TestGrootboek.Datum, "Test").Debet(debet.Id, 250m).Credit(credit.Id, 250m));

        grootboek.Saldo(debet.Id).Should().Be(250m);
        grootboek.Saldo(credit.Id).Should().Be(-250m);
    }

    [Fact]
    public void Boek_ThrowsForUnknownAccount()
    {
        var grootboek = new Grootboek();
        var post = Journaalpost.Nieuw(TestGrootboek.Datum, "Test")
            .Debet(Guid.NewGuid(), 10m)
            .Credit(Guid.NewGuid(), 10m);

        var act = () => grootboek.Boek(post);

        act.Should().Throw<DomeinException>().WithMessage("*onbekende rekening*");
    }

    [Fact]
    public void Boek_ThrowsForInactiveAccount()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        debet.Deactiveer();
        var post = Journaalpost.Nieuw(TestGrootboek.Datum, "Test").Debet(debet.Id, 10m).Credit(credit.Id, 10m);

        var act = () => grootboek.Boek(post);

        act.Should().Throw<DomeinException>().WithMessage("*niet actief*");
    }

    [Fact]
    public void Boek_ThrowsWhenPostingSameEntryTwice()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        var post = Journaalpost.Nieuw(TestGrootboek.Datum, "Test").Debet(debet.Id, 10m).Credit(credit.Id, 10m);
        grootboek.Boek(post);

        var act = () => grootboek.Boek(post);

        act.Should().Throw<DomeinException>().WithMessage("*al geboekt*");
    }

    [Fact]
    public void Boek_ThrowsForUnbalancedEntry()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        var post = Journaalpost.Nieuw(TestGrootboek.Datum, "Test").Debet(debet.Id, 10m).Credit(credit.Id, 5m);

        var act = () => grootboek.Boek(post);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void Saldo_RespectsUpperDateBoundary()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 1, 31), "Januari").Debet(debet.Id, 100m).Credit(credit.Id, 100m));
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 2, 28), "Februari").Debet(debet.Id, 50m).Credit(credit.Id, 50m));

        grootboek.Saldo(debet.Id, new DateOnly(2026, 1, 31)).Should().Be(100m);
        grootboek.Saldo(debet.Id, new DateOnly(2026, 2, 28)).Should().Be(150m);
    }

    [Fact]
    public void MutatieInPeriode_OnlyCountsEntriesInsidePeriod()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2025, 12, 31), "Vorig jaar").Debet(debet.Id, 100m).Credit(credit.Id, 100m));
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 3, 1), "Dit jaar").Debet(debet.Id, 40m).Credit(credit.Id, 40m));

        grootboek.MutatieInPeriode(debet.Id, Periode.VoorJaar(2026)).Should().Be(40m);
    }

    [Fact]
    public void ControleTotaal_IsZeroForBalancedLedger()
    {
        TestGrootboek.MetScenario().ControleTotaal().Should().Be(0m);
    }

    [Fact]
    public void Posten_AreReadOnly()
    {
        var grootboek = TestGrootboek.MetScenario();

        grootboek.Posten.Should().HaveCount(8);
        grootboek.Posten.Should().BeAssignableTo<IReadOnlyList<Journaalpost>>();
    }
}
