using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Tests.Bankzaken;

public sealed class BoekingsvoorstelTests
{
    [Fact]
    public void Voorstel_BevatAlleOnderdelen()
    {
        var transactieId = Guid.NewGuid();

        var voorstel = new Boekingsvoorstel(transactieId, "4000", "Bedrijfskosten", 70, "De omschrijving noemt 'X'.");

        Assert.Equal(transactieId, voorstel.TransactieId);
        Assert.Equal("4000", voorstel.TegenrekeningNummer);
        Assert.Equal("Bedrijfskosten", voorstel.TegenrekeningNaam);
        Assert.Equal(70, voorstel.Zekerheid);
        Assert.False(voorstel.IsBetrouwbaar);
    }

    [Theory]
    [InlineData(90, true)]
    [InlineData(100, true)]
    [InlineData(89, false)]
    public void IsBetrouwbaar_VanafNegentig(int zekerheid, bool verwacht)
    {
        var voorstel = new Boekingsvoorstel(Guid.NewGuid(), "4000", "Bedrijfskosten", zekerheid, "Omdat.");

        Assert.Equal(verwacht, voorstel.IsBetrouwbaar);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ZekerheidBuitenBereik_WordtGeweigerd(int zekerheid)
    {
        var exceptie = Assert.Throws<DomeinException>(
            () => new Boekingsvoorstel(Guid.NewGuid(), "4000", "Bedrijfskosten", zekerheid, "Omdat."));

        Assert.Contains("tussen 0 en 100", exceptie.Message);
    }

    [Fact]
    public void ZonderTransactie_WordtGeweigerd()
    {
        var exceptie = Assert.Throws<DomeinException>(
            () => new Boekingsvoorstel(Guid.Empty, "4000", "Bedrijfskosten", 70, "Omdat."));

        Assert.Contains("banktransactie", exceptie.Message);
    }

    [Fact]
    public void ZonderTegenrekening_WordtGeweigerd()
    {
        var exceptie = Assert.Throws<DomeinException>(
            () => new Boekingsvoorstel(Guid.NewGuid(), "  ", "Bedrijfskosten", 70, "Omdat."));

        Assert.Contains("tegenrekening", exceptie.Message);
    }

    [Fact]
    public void ZonderReden_WordtGeweigerd()
    {
        var exceptie = Assert.Throws<DomeinException>(
            () => new Boekingsvoorstel(Guid.NewGuid(), "4000", "Bedrijfskosten", 70, "   "));

        Assert.Contains("waarom", exceptie.Message);
    }
}
