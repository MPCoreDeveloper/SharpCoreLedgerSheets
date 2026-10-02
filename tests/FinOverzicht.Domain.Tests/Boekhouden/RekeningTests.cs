using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class RekeningTests
{
    [Fact]
    public void Constructor_ThrowsForEmptyId()
    {
        Assert.Throws<DomeinException>(() => new Rekening(Guid.Empty, "1000", "Kas", RekeningSoort.Activa));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsForBlankNumber(string nummer)
    {
        Assert.Throws<DomeinException>(() => new Rekening(Guid.NewGuid(), nummer, "Kas", RekeningSoort.Activa));
    }

    [Fact]
    public void Constructor_ThrowsForBlankName()
    {
        Assert.Throws<DomeinException>(() => new Rekening(Guid.NewGuid(), "1000", " ", RekeningSoort.Activa));
    }

    [Fact]
    public void Constructor_ThrowsWhenCashFlagOnNonAsset()
    {
        Assert.Throws<DomeinException>(
            () => new Rekening(Guid.NewGuid(), "2000", "Crediteuren", RekeningSoort.Passiva, isLiquideMiddel: true));
    }

    [Fact]
    public void Constructor_ThrowsWhenInventoryFlagOnNonAsset()
    {
        Assert.Throws<DomeinException>(
            () => new Rekening(Guid.NewGuid(), "4000", "Kosten", RekeningSoort.Kosten, isVoorraad: true));
    }

    [Fact]
    public void Constructor_TrimsNumberAndName()
    {
        var rekening = new Rekening(Guid.NewGuid(), " 1000 ", " Kas ", RekeningSoort.Activa);

        Assert.Equal("1000", rekening.Nummer);
        Assert.Equal("Kas", rekening.Naam);
    }

    [Theory]
    [InlineData(RekeningSoort.Activa, true)]
    [InlineData(RekeningSoort.Kosten, true)]
    [InlineData(RekeningSoort.Passiva, false)]
    [InlineData(RekeningSoort.EigenVermogen, false)]
    [InlineData(RekeningSoort.Opbrengsten, false)]
    public void HeeftDebetSaldo_ReflectsNormalBalance(RekeningSoort soort, bool verwacht)
    {
        var rekening = new Rekening(Guid.NewGuid(), "1000", "Test", soort);

        Assert.Equal(verwacht, rekening.HeeftDebetSaldo);
    }

    [Fact]
    public void DeactiveerAndActiveer_ToggleIsActief()
    {
        var rekening = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa);

        rekening.Deactiveer();
        Assert.False(rekening.IsActief);

        rekening.Activeer();
        Assert.True(rekening.IsActief);
    }

    [Fact]
    public void Hernoem_UpdatesName()
    {
        var rekening = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa);

        rekening.Hernoem("Kas en bank");

        Assert.Equal("Kas en bank", rekening.Naam);
    }

    [Fact]
    public void Hernoem_ThrowsForBlankName()
    {
        var rekening = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa);

        Assert.Throws<DomeinException>(() => rekening.Hernoem("  "));
    }

    [Fact]
    public void ZetLiquideMiddel_ThrowsForNonAsset()
    {
        var rekening = new Rekening(Guid.NewGuid(), "2000", "Crediteuren", RekeningSoort.Passiva);

        Assert.Throws<DomeinException>(() => rekening.ZetLiquideMiddel(true));
    }
}
