using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Domain.Tests.Bankzaken;

public sealed class MatchingAssistentTests
{
    private const string Banknummer = "1100";
    private static readonly DateOnly Datum = new(2026, 4, 2);

    private readonly MatchingAssistent _assistent = new();

    [Fact]
    public void ZonderSignaal_GeeftGeenVoorstel()
    {
        var voorstel = _assistent.StelVoor(
            Transactie(naam: "Onbekende Partij", omschrijving: "Betaling zonder trefwoord"),
            [Bank(), Kosten(), Omzet()],
            []);

        Assert.Null(voorstel);
    }

    [Fact]
    public void IbanRegel_GeeftVolledigeZekerheid()
    {
        var voorstel = Voorstel(
            Transactie(naam: "Albert Heijn", iban: "NL91ABNA0417164300", omschrijving: "Boodschappen"),
            [Bank(), Kosten(), Omzet()],
            [new TegenrekeningRegel("NL91ABNA0417164300", "4000", 4)]);

        Assert.Equal("4000", voorstel.TegenrekeningNummer);
        Assert.Equal("Bedrijfskosten", voorstel.TegenrekeningNaam);
        Assert.Equal(100, voorstel.Zekerheid);
        Assert.True(voorstel.IsBetrouwbaar);
        Assert.Contains("NL91ABNA0417164300", voorstel.Reden);
    }

    [Fact]
    public void IbanRegel_NegeertAndereGroeperingVanHetIban()
    {
        var voorstel = Voorstel(
            Transactie(naam: "Albert Heijn", iban: "NL91 ABNA 0417 1643 00"),
            [Bank(), Kosten()],
            [new TegenrekeningRegel("nl91abna0417164300", "4000")]);

        Assert.Equal("4000", voorstel.TegenrekeningNummer);
        Assert.Equal(100, voorstel.Zekerheid);
    }

    [Fact]
    public void TrefwoordRegel_InDeOmschrijving_SteltVoorMetNegentig()
    {
        var voorstel = Voorstel(
            Transactie(naam: "Albert Heijn", omschrijving: "Boodschappen winkel 42"),
            [Bank(), Kosten(), Omzet()],
            [new TegenrekeningRegel("albert heijn", "4000")]);

        Assert.Equal("4000", voorstel.TegenrekeningNummer);
        Assert.Equal(90, voorstel.Zekerheid);
        Assert.Contains("ALBERT HEIJN", voorstel.Reden);
    }

    [Fact]
    public void TrefwoordRegel_InDeTegenpartijnaam_SteltVoor()
    {
        var voorstel = Voorstel(
            Transactie(naam: "Gemeente Amsterdam", omschrijving: "Leges"),
            [Bank(), Kosten()],
            [new TegenrekeningRegel("GEMEENTE", "4000")]);

        Assert.Equal("4000", voorstel.TegenrekeningNummer);
        Assert.Equal(90, voorstel.Zekerheid);
    }

    [Fact]
    public void IbanRegel_WintVanTrefwoordregel()
    {
        var voorstel = Voorstel(
            Transactie(naam: "Albert Heijn", iban: "NL91ABNA0417164300", omschrijving: "Betaling"),
            [Bank(), Kosten(), Omzet()],
            [new TegenrekeningRegel("BETALING", "8000"), new TegenrekeningRegel("NL91ABNA0417164300", "4000")]);

        Assert.Equal("4000", voorstel.TegenrekeningNummer);
        Assert.Equal(100, voorstel.Zekerheid);
    }

    [Fact]
    public void MeesteBevestigingen_Wint()
    {
        var voorstel = Voorstel(
            Transactie(naam: "Albert Heijn", omschrijving: "Boodschappen"),
            [Bank(), Kosten(), Omzet()],
            [new TegenrekeningRegel("ALBERT HEIJN", "4000", 1), new TegenrekeningRegel("ALBERT HEIJN", "8000", 5)]);

        Assert.Equal("8000", voorstel.TegenrekeningNummer);
    }

    [Fact]
    public void LangsteTrefwoord_WintBijGelijkeBevestigingen()
    {
        var voorstel = Voorstel(
            Transactie(naam: "Albert Heijn", omschrijving: "Boodschappen"),
            [Bank(), Kosten(), Omzet()],
            [new TegenrekeningRegel("ALBERT", "4000"), new TegenrekeningRegel("ALBERT HEIJN", "8000")]);

        Assert.Equal("Omzet", voorstel.TegenrekeningNaam);
    }

    [Fact]
    public void RekeningnaamInDeOmschrijving_SteltVoorMetZeventig()
    {
        var voorstel = Voorstel(
            Transactie(naam: "Betaalvereniging", omschrijving: "PINBETALING OMZET augustus"),
            [Bank(), Kosten(), Omzet()],
            []);

        Assert.Equal("8000", voorstel.TegenrekeningNummer);
        Assert.Equal(70, voorstel.Zekerheid);
        Assert.False(voorstel.IsBetrouwbaar);
        Assert.Contains("Omzet", voorstel.Reden);
    }

    [Fact]
    public void RekeningnummerAlsLosWoord_SteltVoor()
    {
        var voorstel = Voorstel(
            Transactie(naam: "Onbekende Partij", omschrijving: "Betaling 8000 ten behoeve van derden"),
            [Bank(), Kosten(), Omzet()],
            []);

        Assert.Equal("8000", voorstel.TegenrekeningNummer);
        Assert.Equal(70, voorstel.Zekerheid);
    }

    [Fact]
    public void RekeningnummerBinnenEenGroterGetal_SteltNietVoor()
    {
        var voorstel = _assistent.StelVoor(
            Transactie(naam: "Onbekende Partij", omschrijving: "Factuur 128000"),
            [Bank(), Kosten(), Omzet()],
            []);

        Assert.Null(voorstel);
    }

    [Fact]
    public void InactieveRekening_WordtOvergeslagenWaarnaEenZwakkerSignaalVolgt()
    {
        var verouderd = new Rekening(Guid.NewGuid(), "4100", "Oude kostenrekening", RekeningSoort.Kosten, isActief: false);

        var voorstel = Voorstel(
            Transactie(naam: "Albert Heijn", omschrijving: "PINBETALING OMZET"),
            [Bank(), Kosten(), Omzet(), verouderd],
            [new TegenrekeningRegel("ALBERT HEIJN", "4100", 9)]);

        Assert.Equal("8000", voorstel.TegenrekeningNummer);
        Assert.Equal(70, voorstel.Zekerheid);
    }

    [Fact]
    public void RegelNaarEenOnbekendeRekening_GeeftGeenVoorstel()
    {
        var voorstel = _assistent.StelVoor(
            Transactie(naam: "Albert Heijn", omschrijving: "Boodschappen"),
            [Bank(), Kosten(), Omzet()],
            [new TegenrekeningRegel("ALBERT HEIJN", "9999")]);

        Assert.Null(voorstel);
    }

    [Fact]
    public void DeBankrekeningZelf_WordtNooitVoorgesteld()
    {
        var kas = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);

        var voorstel = _assistent.StelVoor(
            Transactie(naam: null, omschrijving: "Geldopname automaat"),
            [Bank(), Kosten(), kas],
            [new TegenrekeningRegel("GELDOPNAME", Banknummer)]);

        Assert.Null(voorstel);
    }

    [Fact]
    public void EigenRekeningParameter_BepaaltWelkeRekeningWordtUitgesloten()
    {
        var kas = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);

        var voorstel = Voorstel(
            Transactie(naam: null, omschrijving: "Geldopname automaat", bankrekening: "1200"),
            [Bank(), Kosten(), kas],
            [new TegenrekeningRegel("GELDOPNAME", "4000")],
            bankRekeningNummer: "1200");

        Assert.Equal("4000", voorstel.TegenrekeningNummer);
        Assert.Equal(90, voorstel.Zekerheid);
    }

    [Fact]
    public void ZonderActieveRekeningen_GeeftGeenVoorstel()
    {
        var voorstel = _assistent.StelVoor(
            Transactie(naam: "Albert Heijn", omschrijving: "Boodschappen"),
            [Bank(actief: false), Kosten(actief: false)],
            [new TegenrekeningRegel("ALBERT HEIJN", "4000")]);

        Assert.Null(voorstel);
    }

    [Fact]
    public void NullArgumenten_WordenGeweigerd()
    {
        var transactie = Transactie();

        Assert.Throws<ArgumentNullException>(() => _assistent.StelVoor(null!, [Bank()], []));
        Assert.Throws<ArgumentNullException>(() => _assistent.StelVoor(transactie, null!, []));
        Assert.Throws<ArgumentNullException>(() => _assistent.StelVoor(transactie, [Bank()], null!));
    }

    private static BankTransactie Transactie(
        string? naam = null,
        string? iban = null,
        string? omschrijving = null,
        string bankrekening = Banknummer) =>
        new(
            Guid.NewGuid(),
            bankrekening,
            Datum,
            new Bedrag(-25m, Valuta.Eur),
            Guid.NewGuid().ToString("N"),
            naam,
            iban,
            omschrijving);

    private static Rekening Bank(bool actief = true) =>
        new(
            Guid.NewGuid(),
            Banknummer,
            "Bank",
            RekeningSoort.Activa,
            isActief: actief,
            isVlottend: true,
            isLiquideMiddel: true);

    private static Rekening Kosten(bool actief = true) =>
        new(Guid.NewGuid(), "4000", "Bedrijfskosten", RekeningSoort.Kosten, isActief: actief);

    private static Rekening Omzet(bool actief = true) =>
        new(Guid.NewGuid(), "8000", "Omzet", RekeningSoort.Opbrengsten, isActief: actief);

    private Boekingsvoorstel Voorstel(
        BankTransactie transactie,
        IReadOnlyList<Rekening> rekeningen,
        IReadOnlyList<TegenrekeningRegel> regels,
        string? bankRekeningNummer = null) =>
        _assistent.StelVoor(transactie, rekeningen, regels, bankRekeningNummer)
        ?? throw new InvalidOperationException("De matching-assistent had een voorstel moeten doen.");
}
