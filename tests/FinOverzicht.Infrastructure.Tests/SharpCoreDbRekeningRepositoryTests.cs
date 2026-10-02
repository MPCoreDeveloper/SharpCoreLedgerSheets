using FinOverzicht.Domain.Boekhouden;

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

        var rekening = Assert.Single(rekeningen);
        Assert.Equal("1300", rekening.Nummer);
        Assert.Equal("Voorraad", rekening.Naam);
        Assert.Equal(RekeningSoort.Activa, rekening.Soort);
        Assert.True(rekening.IsActief);
        Assert.True(rekening.IsVlottend);
        Assert.False(rekening.IsLiquideMiddel);
        Assert.True(rekening.IsVoorraad);
    }

    [Fact]
    public async Task AlleAsync_OrdersByNumber()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.Rekeningen.ToevoegenAsync(new Rekening(Guid.NewGuid(), "8000", "Omzet", RekeningSoort.Opbrengsten), Ct);
        await context.Rekeningen.ToevoegenAsync(new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa), Ct);

        var rekeningen = await context.Rekeningen.AlleAsync(Ct);

        Assert.Collection(
            rekeningen,
            r => Assert.Equal("1000", r.Nummer),
            r => Assert.Equal("8000", r.Nummer));
    }

    [Fact]
    public async Task ZoekOpNummerAsync_FindsAccount()
    {
        await using var context = new SharpCoreDbTestContext();
        var kas = new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa, isLiquideMiddel: true);
        await context.Rekeningen.ToevoegenAsync(kas, Ct);

        var gevonden = await context.Rekeningen.ZoekOpNummerAsync("1000", Ct);

        Assert.NotNull(gevonden);
        Assert.Equal(kas.Id, gevonden.Id);
        Assert.True(gevonden.IsLiquideMiddel);
    }

    [Fact]
    public async Task ZoekOpNummerAsync_ReturnsNullWhenMissing()
    {
        await using var context = new SharpCoreDbTestContext();

        var gevonden = await context.Rekeningen.ZoekOpNummerAsync("9999", Ct);

        Assert.Null(gevonden);
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

        Assert.NotNull(gevonden);
        Assert.Equal("Kas en bank", gevonden.Naam);
        Assert.False(gevonden.IsActief);
    }

    [Fact]
    public async Task Data_SurvivesReopen()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.Rekeningen.ToevoegenAsync(
            new Rekening(Guid.NewGuid(), "1000", "Kas", RekeningSoort.Activa, isLiquideMiddel: true), Ct);

        await context.HeropenAsync();

        var rekeningen = await context.Rekeningen.AlleAsync(Ct);
        Assert.Single(rekeningen, r => r.Nummer == "1000" && r.IsLiquideMiddel);
    }

    [Fact]
    public async Task ZoekOpNummerAsync_ThrowsForBlankNumber()
    {
        await using var context = new SharpCoreDbTestContext();

        await Assert.ThrowsAsync<ArgumentException>(() => context.Rekeningen.ZoekOpNummerAsync("  ", Ct));
    }
}
