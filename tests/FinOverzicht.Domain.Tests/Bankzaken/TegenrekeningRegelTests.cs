using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Tests.Bankzaken;

public sealed class TegenrekeningRegelTests
{
    [Fact]
    public void Trefwoord_WordtGenormaliseerd()
    {
        var regel = new TegenrekeningRegel("  albert   heijn ", "4000");

        Assert.Equal("ALBERT HEIJN", regel.Trefwoord);
    }

    [Fact]
    public void TegenrekeningNummer_WordtGetrimd()
    {
        var regel = new TegenrekeningRegel("ALBERT HEIJN", "  4000  ");

        Assert.Equal("4000", regel.TegenrekeningNummer);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ah")]
    public void TeKortTrefwoord_WordtGeweigerd(string trefwoord)
    {
        var exceptie = Assert.Throws<DomeinException>(() => new TegenrekeningRegel(trefwoord, "4000"));

        Assert.Contains("trefwoord", exceptie.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ZonderTegenrekening_WordtGeweigerd(string nummer)
    {
        var exceptie = Assert.Throws<DomeinException>(() => new TegenrekeningRegel("ALBERT HEIJN", nummer));

        Assert.Contains("grootboekrekening", exceptie.Message);
    }

    [Fact]
    public void ZonderBevestiging_WordtGeweigerd()
    {
        var exceptie = Assert.Throws<DomeinException>(() => new TegenrekeningRegel("ALBERT HEIJN", "4000", 0));

        Assert.Contains("bevestigd", exceptie.Message);
    }

    [Fact]
    public void Bevestigd_HoogtDeTellerOp()
    {
        var regel = new TegenrekeningRegel("ALBERT HEIJN", "4000");

        var bevestigd = regel.Bevestigd();

        Assert.Equal(1, regel.Bevestigingen);
        Assert.Equal(2, bevestigd.Bevestigingen);
        Assert.Equal(regel.Trefwoord, bevestigd.Trefwoord);
    }

    [Fact]
    public void VoorRekening_HoudtTrefwoordEnBevestigingen()
    {
        var regel = new TegenrekeningRegel("ALBERT HEIJN", "4000", 3);

        var verplaatst = regel.VoorRekening("4100");

        Assert.Equal("4100", verplaatst.TegenrekeningNummer);
        Assert.Equal("ALBERT HEIJN", verplaatst.Trefwoord);
        Assert.Equal(3, verplaatst.Bevestigingen);
    }
}
