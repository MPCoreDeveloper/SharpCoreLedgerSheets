using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Infrastructure.Tests;

public sealed class SharpCoreDbJournaalpostRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Journaalpost MaakPost(Guid kas, Guid omzet, DateOnly datum, decimal bedrag, string omschrijving, string? referentie = null) =>
        Journaalpost.Nieuw(datum, omschrijving, referentie)
            .Debet(kas, bedrag)
            .Credit(omzet, bedrag);

    [Fact]
    public async Task ToevoegenAsync_And_AlleAsync_RoundTripEntryWithLines()
    {
        await using var context = new SharpCoreDbTestContext();
        var kas = Guid.NewGuid();
        var omzet = Guid.NewGuid();
        var post = MaakPost(kas, omzet, new DateOnly(2026, 3, 1), 1234.56m, "Contante verkoop", "F-2026-0001");

        await context.Posten.ToevoegenAsync(post, Ct);
        var posten = await context.Posten.AlleAsync(null, Ct);

        var gelezen = Assert.Single(posten);
        Assert.Equal(post.Id, gelezen.Id);
        Assert.Equal(new DateOnly(2026, 3, 1), gelezen.Datum);
        Assert.Equal("Contante verkoop", gelezen.Omschrijving);
        Assert.Equal("F-2026-0001", gelezen.Referentie);
        Assert.Equal(2, gelezen.Regels.Count);
        Assert.Equal(1234.56m, gelezen.TotaalDebet);
        Assert.Equal(1234.56m, gelezen.TotaalCredit);
        Assert.Equal(kas, gelezen.Regels[0].RekeningId);
        Assert.Equal(1234.56m, gelezen.Regels[0].Debet);
        Assert.Equal(omzet, gelezen.Regels[1].RekeningId);
        Assert.Equal(1234.56m, gelezen.Regels[1].Credit);
    }

    [Fact]
    public async Task ToevoegenAsync_StoresBlankReferenceAsNull()
    {
        await using var context = new SharpCoreDbTestContext();
        var post = MaakPost(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 1), 10m, "Zonder referentie");

        await context.Posten.ToevoegenAsync(post, Ct);
        var posten = await context.Posten.AlleAsync(null, Ct);

        Assert.Null(posten[0].Referentie);
    }

    [Fact]
    public async Task AlleAsync_FiltersByPeriod()
    {
        await using var context = new SharpCoreDbTestContext();
        var kas = Guid.NewGuid();
        var omzet = Guid.NewGuid();
        await context.Posten.ToevoegenAsync(MaakPost(kas, omzet, new DateOnly(2025, 12, 31), 100m, "Vorig jaar"), Ct);
        await context.Posten.ToevoegenAsync(MaakPost(kas, omzet, new DateOnly(2026, 6, 15), 200m, "Dit jaar"), Ct);

        var posten = await context.Posten.AlleAsync(Periode.VoorJaar(2026), Ct);

        Assert.Single(posten, p => p.Omschrijving == "Dit jaar");
    }

    [Fact]
    public async Task AlleAsync_OrdersByDate()
    {
        await using var context = new SharpCoreDbTestContext();
        var kas = Guid.NewGuid();
        var omzet = Guid.NewGuid();
        await context.Posten.ToevoegenAsync(MaakPost(kas, omzet, new DateOnly(2026, 8, 1), 1m, "Augustus"), Ct);
        await context.Posten.ToevoegenAsync(MaakPost(kas, omzet, new DateOnly(2026, 2, 1), 2m, "Februari"), Ct);

        var posten = await context.Posten.AlleAsync(null, Ct);

        Assert.Collection(
            posten,
            p => Assert.Equal("Februari", p.Omschrijving),
            p => Assert.Equal("Augustus", p.Omschrijving));
    }

    [Fact]
    public async Task VerwijderenAsync_RemovesEntryAndItsLines()
    {
        await using var context = new SharpCoreDbTestContext();
        var post = MaakPost(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 1), 100m, "Te verwijderen");
        await context.Posten.ToevoegenAsync(post, Ct);

        await context.Posten.VerwijderenAsync(post.Id, Ct);

        Assert.Empty(await context.Posten.AlleAsync(null, Ct));
    }

    [Fact]
    public async Task Data_SurvivesReopen()
    {
        await using var context = new SharpCoreDbTestContext();
        var kas = Guid.NewGuid();
        var omzet = Guid.NewGuid();
        await context.Posten.ToevoegenAsync(MaakPost(kas, omzet, new DateOnly(2026, 3, 1), 250m, "Verkoop"), Ct);

        await context.HeropenAsync();

        var posten = await context.Posten.AlleAsync(null, Ct);
        Assert.Single(posten);
        Assert.Equal(250m, posten[0].TotaalDebet);
    }

    [Fact]
    public async Task AlleAsync_ReturnsEmptyForEmptyDatabase()
    {
        await using var context = new SharpCoreDbTestContext();

        var posten = await context.Posten.AlleAsync(null, Ct);

        Assert.Empty(posten);
    }
}
