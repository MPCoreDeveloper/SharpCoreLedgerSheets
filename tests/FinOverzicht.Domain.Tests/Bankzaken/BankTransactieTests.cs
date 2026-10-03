using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Tests.Bankzaken;

public sealed class BankTransactieTests
{
    private static readonly DateOnly Datum = new(2026, 4, 2);

    [Fact]
    public void Nieuw_MaaktEenOpenTransactie()
    {
        var transactie = Maak();

        Assert.Equal(BankTransactieStatus.Open, transactie.Status);
        Assert.Null(transactie.JournaalpostId);
        Assert.True(transactie.IsAfschrijving);
        Assert.False(transactie.IsBijschrijving);
        Assert.Equal(12.50m, transactie.BedragZonderTeken.Waarde);
    }

    [Fact]
    public void Bijschrijving_IsPositief()
    {
        var transactie = Maak(bedrag: 250m);

        Assert.True(transactie.IsBijschrijving);
        Assert.False(transactie.IsAfschrijving);
        Assert.Equal(250m, transactie.BedragZonderTeken.Waarde);
        Assert.Equal(Valuta.Eur, transactie.BedragZonderTeken.Valuta);
    }

    [Fact]
    public void ZonderId_WordtGeweigerd()
    {
        var exceptie = Assert.Throws<DomeinException>(() => Maak(id: Guid.Empty));

        Assert.Contains("uniek id", exceptie.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ZonderBankrekening_WordtGeweigerd(string bankrekening)
    {
        var exceptie = Assert.Throws<DomeinException>(() => Maak(bankrekening: bankrekening));

        Assert.Contains("grootboekrekening van de bank", exceptie.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ZonderReferentie_WordtGeweigerd(string referentie)
    {
        var exceptie = Assert.Throws<DomeinException>(() => Maak(referentie: referentie));

        Assert.Contains("referentie", exceptie.Message);
    }

    [Fact]
    public void ZonderBedrag_WordtGeweigerd()
    {
        var exceptie = Assert.Throws<DomeinException>(() => Maak(bedrag: 0m));

        Assert.Contains("zonder bedrag", exceptie.Message);
    }

    [Fact]
    public void Sleutel_IsHoofdletterongevoelig()
    {
        var eerste = Maak(bankrekening: "1100", referentie: "abc-123");
        var tweede = Maak(bankrekening: "1100", referentie: "ABC-123");

        Assert.Equal(eerste.Sleutel, tweede.Sleutel);
    }

    [Fact]
    public void Sleutel_VerschiltPerBankrekening()
    {
        var eerste = Maak(bankrekening: "1100");
        var tweede = Maak(bankrekening: "1200");

        Assert.NotEqual(eerste.Sleutel, tweede.Sleutel);
    }

    [Fact]
    public void Zoektekst_NormaliseertTegenpartijIbanEnOmschrijving()
    {
        var transactie = Maak();

        Assert.Equal("ALBERT HEIJN NL91ABNA0417164300 BOODSCHAPPEN", transactie.Zoektekst);
    }

    [Fact]
    public void TegenpartijEnOmschrijving_WordenGetrimd()
    {
        var transactie = Maak(naam: "  Albert Heijn  ", omschrijving: "   ");

        Assert.Equal("Albert Heijn", transactie.TegenpartijNaam);
        Assert.Null(transactie.Omschrijving);
    }

    [Fact]
    public void MarkeerGeboekt_ZetStatusEnJournaalpost()
    {
        var post = Guid.NewGuid();

        var geboekt = Maak().MarkeerGeboekt(post);

        Assert.Equal(BankTransactieStatus.Geboekt, geboekt.Status);
        Assert.Equal(post, geboekt.JournaalpostId);
    }

    [Fact]
    public void MarkeerGeboekt_LaatHetOrigineelOngewijzigd()
    {
        var transactie = Maak();

        _ = transactie.MarkeerGeboekt(Guid.NewGuid());

        Assert.Equal(BankTransactieStatus.Open, transactie.Status);
        Assert.Null(transactie.JournaalpostId);
    }

    [Fact]
    public void MarkeerGeboekt_ZonderJournaalpost_WordtGeweigerd()
    {
        var exceptie = Assert.Throws<DomeinException>(() => Maak().MarkeerGeboekt(Guid.Empty));

        Assert.Contains("journaalpost", exceptie.Message);
    }

    [Fact]
    public void Negeer_ZetStatusEnWistDeJournaalpost()
    {
        var genegeerd = Maak().MarkeerGeboekt(Guid.NewGuid()).Negeer();

        Assert.Equal(BankTransactieStatus.Genegeerd, genegeerd.Status);
        Assert.Null(genegeerd.JournaalpostId);
    }

    private static BankTransactie Maak(
        Guid? id = null,
        string bankrekening = "1100",
        decimal bedrag = -12.50m,
        string referentie = "BANK-2026-0001",
        string? naam = "Albert Heijn",
        string? iban = "NL91ABNA0417164300",
        string? omschrijving = "Boodschappen") =>
        new(
            id ?? Guid.NewGuid(),
            bankrekening,
            Datum,
            new Bedrag(bedrag, Valuta.Eur),
            referentie,
            naam,
            iban,
            omschrijving);
}
