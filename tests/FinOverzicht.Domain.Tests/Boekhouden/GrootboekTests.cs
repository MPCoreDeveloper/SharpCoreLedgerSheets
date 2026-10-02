using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class GrootboekTests
{
    [Fact]
    public void DefaultsToEuro()
    {
        Assert.Equal(Valuta.Eur, new Grootboek().Valuta);
    }

    [Fact]
    public void VoegRekeningToe_AddsAccountWithGeneratedId()
    {
        var grootboek = new Grootboek();

        var rekening = grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa);

        Assert.NotEqual(Guid.Empty, rekening.Id);
        Assert.Single(grootboek.Rekeningen);
    }

    [Fact]
    public void VoegRekeningToe_ThrowsForDuplicateNumberCaseInsensitive()
    {
        var grootboek = new Grootboek();
        grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa);

        var exceptie = Assert.Throws<DomeinException>(
            () => grootboek.VoegRekeningToe("1000", "Kas 2", RekeningSoort.Activa));

        Assert.Contains("bestaat al", exceptie.Message);
    }

    [Fact]
    public void ZoekRekening_FindsByNumberCaseInsensitive()
    {
        var grootboek = new Grootboek();
        var kas = grootboek.VoegRekeningToe("1000", "Kas", RekeningSoort.Activa);

        Assert.Same(kas, grootboek.ZoekRekening("1000"));
        Assert.Null(grootboek.ZoekRekening("1000 "));
    }

    [Fact]
    public void Boek_AddsPostAndComputesSignedBalance()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        grootboek.Boek(Journaalpost.Nieuw(TestGrootboek.Datum, "Test").Debet(debet.Id, 250m).Credit(credit.Id, 250m));

        Assert.Equal(250m, grootboek.Saldo(debet.Id));
        Assert.Equal(-250m, grootboek.Saldo(credit.Id));
    }

    [Fact]
    public void Boek_ThrowsForUnknownAccount()
    {
        var grootboek = new Grootboek();
        var post = Journaalpost.Nieuw(TestGrootboek.Datum, "Test")
            .Debet(Guid.NewGuid(), 10m)
            .Credit(Guid.NewGuid(), 10m);

        var exceptie = Assert.Throws<DomeinException>(() => grootboek.Boek(post));

        Assert.Contains("onbekende rekening", exceptie.Message);
    }

    [Fact]
    public void Boek_ThrowsForInactiveAccount()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        debet.Deactiveer();
        var post = Journaalpost.Nieuw(TestGrootboek.Datum, "Test").Debet(debet.Id, 10m).Credit(credit.Id, 10m);

        var exceptie = Assert.Throws<DomeinException>(() => grootboek.Boek(post));

        Assert.Contains("niet actief", exceptie.Message);
    }

    [Fact]
    public void Boek_ThrowsWhenPostingSameEntryTwice()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        var post = Journaalpost.Nieuw(TestGrootboek.Datum, "Test").Debet(debet.Id, 10m).Credit(credit.Id, 10m);
        grootboek.Boek(post);

        var exceptie = Assert.Throws<DomeinException>(() => grootboek.Boek(post));

        Assert.Contains("al geboekt", exceptie.Message);
    }

    [Fact]
    public void Boek_ThrowsForUnbalancedEntry()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        var post = Journaalpost.Nieuw(TestGrootboek.Datum, "Test").Debet(debet.Id, 10m).Credit(credit.Id, 5m);

        Assert.Throws<DomeinException>(() => grootboek.Boek(post));
    }

    [Fact]
    public void Saldo_RespectsUpperDateBoundary()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 1, 31), "Januari").Debet(debet.Id, 100m).Credit(credit.Id, 100m));
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 2, 28), "Februari").Debet(debet.Id, 50m).Credit(credit.Id, 50m));

        Assert.Equal(100m, grootboek.Saldo(debet.Id, new DateOnly(2026, 1, 31)));
        Assert.Equal(150m, grootboek.Saldo(debet.Id, new DateOnly(2026, 2, 28)));
    }

    [Fact]
    public void MutatieInPeriode_OnlyCountsEntriesInsidePeriod()
    {
        var (grootboek, debet, credit) = TestGrootboek.MetTweeRekeningen();
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2025, 12, 31), "Vorig jaar").Debet(debet.Id, 100m).Credit(credit.Id, 100m));
        grootboek.Boek(Journaalpost.Nieuw(new DateOnly(2026, 3, 1), "Dit jaar").Debet(debet.Id, 40m).Credit(credit.Id, 40m));

        Assert.Equal(40m, grootboek.MutatieInPeriode(debet.Id, Periode.VoorJaar(2026)));
    }

    [Fact]
    public void ControleTotaal_IsZeroForBalancedLedger()
    {
        Assert.Equal(0m, TestGrootboek.MetScenario().ControleTotaal());
    }

    [Fact]
    public void Posten_AreReadOnly()
    {
        var grootboek = TestGrootboek.MetScenario();

        Assert.Equal(8, grootboek.Posten.Count);
        Assert.IsAssignableFrom<IReadOnlyList<Journaalpost>>(grootboek.Posten);
    }
}
