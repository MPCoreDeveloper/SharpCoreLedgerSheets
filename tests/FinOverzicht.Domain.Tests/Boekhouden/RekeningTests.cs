using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class RekeningTests
{
    [Fact]
    public void Constructor_ThrowsForEmptyId()
    {
        var act = () => new Rekening(Guid.Empty, "1000", "Kas", RekeningSoort.Activa);

        act.Should().Throw<DomeinException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsForBlankNumber(string nummer)
    {
        var act = () => new Rekening(Guid.NewGuid(), nummer, "Kas", RekeningSoort.Activa);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void Constructor_ThrowsForBlankName()
    {
        var act = () => new Rekening(Guid.NewGuid(), "1000", " ", RekeningSoort.Activa);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void Constructor_ThrowsWhenCashFlagOnNonAsset()
    {
        var act = () => new Rekening(Guid.NewGuid(), "2000", "Crediteuren", RekeningSoort.Passiva, isLiquideMiddel: true);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void Constructor_ThrowsWhenInventoryFlagOnNonAsset()
    {
        var act = () => new Rekening(Guid.NewGuid(), "4000", "Kosten", RekeningSoort.Kosten, isVoorraad: true);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void Constructor_TrimsNumberAndName()
    {
        var rekening = new Rekening(Guid.NewGuid(), " 1000 ", " Kas ", RekeningSoort.Activa);

        rekening.Nummer.Should().Be("1000");
        rekening.Naam.Should().Be("Kas");
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

        rekening.HeeftDebetSaldo.Should().Be(verwacht);
    }

    [Fact]
    public void DeactiveerAndActiveer_ToggleIsActief()
    {
        var rekening = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa);

        rekening.Deactiveer();
        rekening.IsActief.Should().BeFalse();

        rekening.Activeer();
        rekening.IsActief.Should().BeTrue();
    }

    [Fact]
    public void Hernoem_UpdatesName()
    {
        var rekening = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa);

        rekening.Hernoem("Kas en bank");

        rekening.Naam.Should().Be("Kas en bank");
    }

    [Fact]
    public void Hernoem_ThrowsForBlankName()
    {
        var rekening = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa);

        var act = () => rekening.Hernoem("  ");

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void ZetLiquideMiddel_ThrowsForNonAsset()
    {
        var rekening = new Rekening(Guid.NewGuid(), "2000", "Crediteuren", RekeningSoort.Passiva);

        var act = () => rekening.ZetLiquideMiddel(true);

        act.Should().Throw<DomeinException>();
    }
}
