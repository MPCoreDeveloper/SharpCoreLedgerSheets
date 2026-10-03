using FinOverzicht.Application.Exceptions;
using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Application.Tests.Services;

public sealed class BankstaatServiceTests
{
    private const string Bank = "1100";
    private const string Kosten = "4000";
    private const string Omzet = "8000";

    private const string AlbertHeijn = "2026-04-02;-12.50;REF-0001;Albert Heijn;NL91ABNA0417164300;Boodschappen";
    private const string Pinbetaling = "2026-04-03;250.00;REF-0002;Betaalvereniging;;PINBETALING OMZET";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ImporteerAsync_LeestEnBewaartNieuweMutaties()
    {
        var administratie = await MetAdministratieAsync();

        var resultaat = await ImporteerAsync(administratie, $"{AlbertHeijn}\n{Pinbetaling}");

        Assert.Equal(BankstaatFormaat.Csv, resultaat.Formaat);
        Assert.Equal(2, resultaat.Gelezen);
        Assert.Equal(2, resultaat.Toegevoegd);
        Assert.Equal(0, resultaat.Overgeslagen);
        Assert.False(resultaat.AllesAlBekend);
        Assert.Equal(2, resultaat.Nieuw.Count);
        Assert.Equal(2, (await administratie.BankstaatService.AlleAsync(BankTransactieStatus.Open, Ct)).Count);
    }

    [Fact]
    public async Task ImporteerAsync_SlaatAlBekendeMutatiesOver()
    {
        var administratie = await MetAdministratieAsync();
        await ImporteerAsync(administratie, $"{AlbertHeijn}\n{Pinbetaling}");

        var resultaat = await ImporteerAsync(administratie, $"{AlbertHeijn}\n{Pinbetaling}");

        Assert.Equal(2, resultaat.Gelezen);
        Assert.Equal(0, resultaat.Toegevoegd);
        Assert.Equal(2, resultaat.Overgeslagen);
        Assert.True(resultaat.AllesAlBekend);
        Assert.Empty(resultaat.Nieuw);
    }

    [Fact]
    public async Task ImporteerAsync_SlaatEenDubbeleRegelBinnenEenBestandOver()
    {
        var administratie = await MetAdministratieAsync();

        var resultaat = await ImporteerAsync(administratie, $"{AlbertHeijn}\n{AlbertHeijn}");

        Assert.Equal(2, resultaat.Gelezen);
        Assert.Equal(1, resultaat.Toegevoegd);
        Assert.Equal(1, resultaat.Overgeslagen);
    }

    [Fact]
    public async Task ImporteerAsync_KiestDeLezerOpBestandsnaam()
    {
        var administratie = await MetAdministratieAsync();

        var resultaat = await ImporteerAsync(administratie, AlbertHeijn, "afschrift.fake");

        Assert.Equal(BankstaatFormaat.Csv, resultaat.Formaat);
    }

    [Fact]
    public async Task ImporteerAsync_KiestDeLezerOpHetOpgegevenFormaat()
    {
        var administratie = await MetAdministratieAsync();

        var resultaat = await administratie.BankstaatService.ImporteerAsync(
            new BankstaatImportCommand("afschrift.fake", AlbertHeijn, Bank, BankstaatFormaat.Camt053),
            Ct);

        Assert.Equal(BankstaatFormaat.Camt053, resultaat.Formaat);
    }

    [Fact]
    public async Task ImporteerAsync_ZonderGeschikteLezer_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();

        var exceptie = await Assert.ThrowsAsync<DomeinException>(
            () => ImporteerAsync(administratie, AlbertHeijn, "afschrift.pdf"));

        Assert.Contains("geen ondersteund bankafschrift", exceptie.Message);
    }

    [Fact]
    public async Task ImporteerAsync_OnbekendeBankrekening_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();

        var exceptie = await Assert.ThrowsAsync<NietGevondenException>(
            () => administratie.BankstaatService.ImporteerAsync(
                new BankstaatImportCommand("afschrift.fake", AlbertHeijn, "9999"),
                Ct));

        Assert.Contains("9999", exceptie.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ImporteerAsync_BlancoBestandsnaam_WordtGeweigerd(string bestandsnaam)
    {
        var administratie = await MetAdministratieAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => ImporteerAsync(administratie, AlbertHeijn, bestandsnaam));
    }

    [Fact]
    public async Task ImporteerAsync_KoppeltDeMutatieAanDeBankrekening()
    {
        var administratie = await MetAdministratieAsync();

        await ImporteerAsync(administratie, AlbertHeijn);

        var mutatie = Assert.Single(await administratie.BankstaatService.AlleAsync(null, Ct));
        Assert.Equal(Bank, mutatie.BankRekeningNummer);
        Assert.Equal(BankTransactieStatus.Open, mutatie.Status);
    }

    [Fact]
    public async Task BoekAsync_EenAfschrijving_DebiteertDeTegenrekening()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie);

        var post = await administratie.BankstaatService.BoekAsync(mutatie.Id, Kosten, ct: Ct);

        var bank = await administratie.Rekeningen.ZoekOpNummerAsync(Bank, Ct);
        var kosten = await administratie.Rekeningen.ZoekOpNummerAsync(Kosten, Ct);

        Assert.Equal(new DateOnly(2026, 4, 2), post.Datum);
        Assert.Equal("REF-0001", post.Referentie);
        Assert.True(post.IsInBalans);
        Assert.Equal(12.50m, post.Regels.Single(regel => regel.RekeningId == kosten!.Id).Debet);
        Assert.Equal(12.50m, post.Regels.Single(regel => regel.RekeningId == bank!.Id).Credit);
    }

    [Fact]
    public async Task BoekAsync_EenBijschrijving_DebiteertDeBankrekening()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie, Pinbetaling);

        var post = await administratie.BankstaatService.BoekAsync(mutatie.Id, Omzet, ct: Ct);

        var bank = await administratie.Rekeningen.ZoekOpNummerAsync(Bank, Ct);
        var omzet = await administratie.Rekeningen.ZoekOpNummerAsync(Omzet, Ct);

        Assert.Equal(250.00m, post.Regels.Single(regel => regel.RekeningId == bank!.Id).Debet);
        Assert.Equal(250.00m, post.Regels.Single(regel => regel.RekeningId == omzet!.Id).Credit);
    }

    [Fact]
    public async Task BoekAsync_MarkeertDeMutatieAlsGeboekt()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie);

        var post = await administratie.BankstaatService.BoekAsync(mutatie.Id, Kosten, ct: Ct);

        var geboekt = Assert.Single(
            await administratie.BankstaatService.AlleAsync(BankTransactieStatus.Geboekt, Ct));

        Assert.Equal(post.Id, geboekt.JournaalpostId);
        Assert.Empty(await administratie.BankstaatService.AlleAsync(BankTransactieStatus.Open, Ct));
        Assert.Single(await administratie.Posten.AlleAsync(ct: Ct));
    }

    [Fact]
    public async Task BoekAsync_GebruiktDeOmschrijvingVanDeMutatie()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie);

        var post = await administratie.BankstaatService.BoekAsync(mutatie.Id, Kosten, ct: Ct);

        Assert.Equal("Albert Heijn - Boodschappen", post.Omschrijving);
    }

    [Fact]
    public async Task BoekAsync_TweeKeerDezelfdeMutatie_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie);
        await administratie.BankstaatService.BoekAsync(mutatie.Id, Kosten, ct: Ct);

        var exceptie = await Assert.ThrowsAsync<DomeinException>(
            () => administratie.BankstaatService.BoekAsync(mutatie.Id, Kosten, ct: Ct));

        Assert.Contains("al verwerkt", exceptie.Message);
    }

    [Fact]
    public async Task BoekAsync_OpDeBankrekeningZelf_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie);

        var exceptie = await Assert.ThrowsAsync<DomeinException>(
            () => administratie.BankstaatService.BoekAsync(mutatie.Id, Bank, ct: Ct));

        Assert.Contains("bankrekening zelf", exceptie.Message);
    }

    [Fact]
    public async Task BoekAsync_OnbekendeTegenrekening_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie);

        await Assert.ThrowsAsync<NietGevondenException>(
            () => administratie.BankstaatService.BoekAsync(mutatie.Id, "9999", ct: Ct));
    }

    [Fact]
    public async Task BoekAsync_InactieveTegenrekening_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie);
        await administratie.RekeningService.DeactiveerAsync(Kosten, Ct);

        var exceptie = await Assert.ThrowsAsync<DomeinException>(
            () => administratie.BankstaatService.BoekAsync(mutatie.Id, Kosten, ct: Ct));

        Assert.Contains("niet actief", exceptie.Message);
    }

    [Fact]
    public async Task BoekAsync_OnbekendeMutatie_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();

        await Assert.ThrowsAsync<NietGevondenException>(
            () => administratie.BankstaatService.BoekAsync(Guid.NewGuid(), Kosten, ct: Ct));
    }

    [Fact]
    public async Task BoekAsync_ZonderId_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => administratie.BankstaatService.BoekAsync(Guid.Empty, Kosten, ct: Ct));
    }

    [Fact]
    public async Task NegeerAsync_ZetDeMutatieOpGenegeerd()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie);

        await administratie.BankstaatService.NegeerAsync(mutatie.Id, Ct);

        Assert.Empty(await administratie.BankstaatService.AlleAsync(BankTransactieStatus.Open, Ct));
        Assert.Single(await administratie.BankstaatService.AlleAsync(BankTransactieStatus.Genegeerd, Ct));
        Assert.Empty(await administratie.Posten.AlleAsync(ct: Ct));
    }

    [Fact]
    public async Task NegeerAsync_EenGeboekteMutatie_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie);
        await administratie.BankstaatService.BoekAsync(mutatie.Id, Kosten, ct: Ct);

        var exceptie = await Assert.ThrowsAsync<DomeinException>(
            () => administratie.BankstaatService.NegeerAsync(mutatie.Id, Ct));

        Assert.Contains("niet worden genegeerd", exceptie.Message);
    }

    [Fact]
    public async Task VoorstelAsync_ZonderSignaal_GeeftGeenVoorstel()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(
            administratie,
            "2026-04-04;-31.00;REF-0009;Onbekende Partij;NL99RABO0123456789;Betaling");

        Assert.Null(await administratie.BankstaatService.VoorstelAsync(mutatie.Id, Ct));
    }

    [Fact]
    public async Task VoorstelAsync_LeertVanEenEerdereBoeking()
    {
        var administratie = await MetAdministratieAsync();
        var eerste = await OpenMutatieAsync(administratie);
        await administratie.BankstaatService.BoekAsync(eerste.Id, Kosten, ct: Ct);
        var tweede = await OpenMutatieAsync(
            administratie,
            "2026-04-05;-8.00;REF-0004;Albert Heijn;NL91ABNA0417164300;Boodschappen");

        var voorstel = await administratie.BankstaatService.VoorstelAsync(tweede.Id, Ct);

        Assert.NotNull(voorstel);
        Assert.Equal(Kosten, voorstel!.TegenrekeningNummer);
        Assert.Equal("Bedrijfskosten", voorstel.TegenrekeningNaam);
        Assert.True(voorstel.IsBetrouwbaar);
    }

    [Fact]
    public async Task VoorstellenAsync_DoetEenVoorstelPerMutatie()
    {
        var administratie = await MetAdministratieAsync();
        var geleerd = await OpenMutatieAsync(administratie);
        await administratie.BankstaatService.BoekAsync(geleerd.Id, Kosten, ct: Ct);
        await ImporteerAsync(
            administratie,
            "2026-04-04;-8.00;REF-0004;Albert Heijn;NL91ABNA0417164300;Boodschappen\n" +
            "2026-04-05;-31.00;REF-0009;Onbekende Partij;NL99RABO0123456789;Betaling");

        var open = await administratie.BankstaatService.AlleAsync(BankTransactieStatus.Open, Ct);
        var voorstellen = await administratie.BankstaatService.VoorstellenAsync(open, Ct);

        var voorstel = Assert.Single(voorstellen);
        Assert.Equal(open.Single(mutatie => mutatie.Referentie == "REF-0004").Id, voorstel.TransactieId);
        Assert.Equal(Kosten, voorstel.TegenrekeningNummer);
        Assert.True(voorstel.IsBetrouwbaar);
    }

    [Fact]
    public async Task VoorstellenAsync_LeestDeRekeningenEnDeRegelsEenKeer()
    {
        var administratie = await MetAdministratieAsync();
        var geleerd = await OpenMutatieAsync(administratie);
        await administratie.BankstaatService.BoekAsync(geleerd.Id, Kosten, ct: Ct);
        await ImporteerAsync(
            administratie,
            "2026-04-04;-8.00;REF-0004;Albert Heijn;NL91ABNA0417164300;Boodschappen\n" +
            "2026-04-05;-9.00;REF-0005;Albert Heijn;NL91ABNA0417164300;Boodschappen");
        var open = await administratie.BankstaatService.AlleAsync(BankTransactieStatus.Open, Ct);
        var rekeningenVoor = administratie.Rekeningen.AantalKeerGelezen;
        var regelsVoor = administratie.Regels.AantalKeerGelezen;

        var voorstellen = await administratie.BankstaatService.VoorstellenAsync(open, Ct);

        Assert.Equal(2, open.Count);
        Assert.Equal(2, voorstellen.Count);
        Assert.All(voorstellen, voorstel => Assert.True(voorstel.IsBetrouwbaar));
        Assert.Equal(1, administratie.Rekeningen.AantalKeerGelezen - rekeningenVoor);
        Assert.Equal(1, administratie.Regels.AantalKeerGelezen - regelsVoor);
    }

    [Fact]
    public async Task VoorstellenAsync_ZonderMutaties_LeestNiets()
    {
        var administratie = await MetAdministratieAsync();
        var rekeningenVoor = administratie.Rekeningen.AantalKeerGelezen;
        var regelsVoor = administratie.Regels.AantalKeerGelezen;

        var voorstellen = await administratie.BankstaatService.VoorstellenAsync([], Ct);

        Assert.Empty(voorstellen);
        Assert.Equal(rekeningenVoor, administratie.Rekeningen.AantalKeerGelezen);
        Assert.Equal(regelsVoor, administratie.Regels.AantalKeerGelezen);
    }

    [Fact]
    public async Task VoorstellenAsync_ZonderLijst_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => administratie.BankstaatService.VoorstellenAsync(null!, Ct));
    }

    [Fact]
    public async Task VoorstelAsync_OnbekendeMutatie_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();

        await Assert.ThrowsAsync<NietGevondenException>(
            () => administratie.BankstaatService.VoorstelAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task VoorstelAsync_ZonderId_WordtGeweigerd()
    {
        var administratie = await MetAdministratieAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => administratie.BankstaatService.VoorstelAsync(Guid.Empty, Ct));
    }

    [Fact]
    public async Task BoekAsync_OnthoudtDeRegelEnBevestigtHem()
    {
        var administratie = await MetAdministratieAsync();
        var eerste = await OpenMutatieAsync(administratie);
        await administratie.BankstaatService.BoekAsync(eerste.Id, Kosten, ct: Ct);
        var tweede = await OpenMutatieAsync(
            administratie,
            "2026-04-05;-8.00;REF-0004;Albert Heijn;NL91ABNA0417164300;Boodschappen");

        await administratie.BankstaatService.BoekAsync(tweede.Id, Kosten, ct: Ct);

        var regel = Assert.Single(await administratie.Regels.AlleAsync(Ct));
        Assert.Equal("NL91ABNA0417164300", regel.Trefwoord);
        Assert.Equal(Kosten, regel.TegenrekeningNummer);
        Assert.Equal(2, regel.Bevestigingen);
    }

    [Fact]
    public async Task BoekAsync_ZonderTegenpartij_OnthoudtGeenRegel()
    {
        var administratie = await MetAdministratieAsync();
        var mutatie = await OpenMutatieAsync(administratie, "2026-04-06;-3.50;REF-0005;;;;");

        await administratie.BankstaatService.BoekAsync(mutatie.Id, Kosten, ct: Ct);

        Assert.Empty(await administratie.Regels.AlleAsync(Ct));
    }

    [Fact]
    public async Task AlleAsync_ZonderStatus_ToontElkeMutatie()
    {
        var administratie = await MetAdministratieAsync();
        var eerste = await OpenMutatieAsync(administratie);
        await administratie.BankstaatService.BoekAsync(eerste.Id, Kosten, ct: Ct);
        await OpenMutatieAsync(administratie, Pinbetaling);

        Assert.Equal(2, (await administratie.BankstaatService.AlleAsync(ct: Ct)).Count);
    }

    private static Task<BankstaatImportResultaat> ImporteerAsync(
        TestAdministratie administratie,
        string inhoud,
        string bestandsnaam = "afschrift.fake") =>
        administratie.BankstaatService.ImporteerAsync(
            new BankstaatImportCommand(bestandsnaam, inhoud, Bank),
            Ct);

    private static async Task<TestAdministratie> MetAdministratieAsync()
    {
        var administratie = new TestAdministratie();

        await administratie.RekeningService.VoegToeAsync(
            new RekeningToevoegenCommand(Bank, "Bank", RekeningSoort.Activa, Vlottend: true, LiquideMiddel: true),
            Ct);
        await administratie.RekeningService.VoegToeAsync(
            new RekeningToevoegenCommand(Kosten, "Bedrijfskosten", RekeningSoort.Kosten),
            Ct);
        await administratie.RekeningService.VoegToeAsync(
            new RekeningToevoegenCommand(Omzet, "Omzet", RekeningSoort.Opbrengsten),
            Ct);

        return administratie;
    }

    private static async Task<BankTransactie> OpenMutatieAsync(
        TestAdministratie administratie,
        string inhoud = AlbertHeijn)
    {
        await ImporteerAsync(administratie, inhoud);

        return Assert.Single(
            await administratie.BankstaatService.AlleAsync(BankTransactieStatus.Open, Ct));
    }
}
