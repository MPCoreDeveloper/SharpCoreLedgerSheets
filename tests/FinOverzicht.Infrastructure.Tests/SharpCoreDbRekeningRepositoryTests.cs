using FinOverzicht.Domain.Boekhouden;
using FluentAssertions;

namespace FinOverzicht.Infrastructure.Tests;

public sealed class SharpCoreDbRekeningRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ToevoegenAsync_And_AlleAsync_RoundTripAllProperties()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.Rekeningen.ToevoegenAsync(
            new Rekening(Guid.NewGuid(), "1300", "Voorraad", RekeningSoort.Activa,
                isActief: true, isVlottend: true, isLiquideMiddel: false, isVoorraad: true), Ct);

        var rekeningen = await context.Rekeningen.AlleAsync(Ct);

        var rekening = rekeningen.Should().ContainSingle().Subject;
        rekening.Nummer.Should().Be("1300");
        rekening.Naam.Should().Be("Voorraad");
        rekening.Soort.Should().Be(RekeningSoort.Activa);
        rekening.IsActief.Should().BeTrue();
        rekening.IsVlottend.Should().BeTrue();
        rekening.IsLiquideMiddel.Should().BeFalse();
        rekening.IsVoorraad.Should().BeTrue();
    }

    [Fact]
    public async Task AlleAsync_OrdersByNumber()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.Rekeningen.ToevoegenAsync(new Rekening(Guid.NewGuid(), "8000", "Omzet", RekeningSoort.Opbrengsten), Ct);
        await context.Rekeningen.ToevoegenAsync(new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa), Ct);

        var rekeningen = await context.Rekeningen.AlleAsync(Ct);

        rekeningen.Select(r => r.Nummer).Should().ContainInOrder("1000", "8000");
    }

    [Fact]
    public async Task ZoekOpNummerAsync_FindsAccount()
    {
        await using var context = new SharpCoreDbTestContext();
        var kas = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa, isLiquideMiddel: true);
        await context.Rekeningen.ToevoegenAsync(kas, Ct);

        var gevonden = await context.Rekeningen.ZoekOpNummerAsync("1000", Ct);

        gevonden.Should().NotBeNull();
        gevonden!.Id.Should().Be(kas.Id);
        gevonden.IsLiquideMiddel.Should().BeTrue();
    }

    [Fact]
    public async Task ZoekOpNummerAsync_ReturnsNullWhenMissing()
    {
        await using var context = new SharpCoreDbTestContext();

        var gevonden = await context.Rekeningen.ZoekOpNummerAsync("9999", Ct);

        gevonden.Should().BeNull();
    }

    [Fact]
    public async Task BijwerkenAsync_PersistsNameAndFlags()
    {
        await using var context = new SharpCoreDbTestContext();
        var rekening = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa, isLiquideMiddel: true);
        await context.Rekeningen.ToevoegenAsync(rekening, Ct);

        rekening.Hernoem("Kas en bank");
        rekening.Deactiveer();
        await context.Rekeningen.BijwerkenAsync(rekening, Ct);

        var gevonden = await context.Rekeningen.ZoekOpNummerAsync("1000", Ct);

        gevonden!.Naam.Should().Be("Kas en bank");
        gevonden.IsActief.Should().BeFalse();
    }

    [Fact]
    public async Task Data_SurvivesReopen()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.Rekeningen.ToevoegenAsync(
            new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa, isLiquideMiddel: true), Ct);

        await context.HeropenAsync();

        var rekeningen = await context.Rekeningen.AlleAsync(Ct);
        rekeningen.Should().ContainSingle(r => r.Nummer == "1000" && r.IsLiquideMiddel);
    }

    [Fact]
    public async Task ZoekOpNummerAsync_ThrowsForBlankNumber()
    {
        await using var context = new SharpCoreDbTestContext();

        var act = () => context.Rekeningen.ZoekOpNummerAsync("  ", Ct);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
