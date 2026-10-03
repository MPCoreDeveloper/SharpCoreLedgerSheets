using FinOverzicht.Domain.Bankzaken;

namespace FinOverzicht.Infrastructure.Tests;

/// <summary>
/// Tests for the SharpCoreDB matching rule store: a keyword identifies a rule, so storing a rule for
/// a keyword that already exists replaces it instead of leaving two conflicting rules behind.
/// </summary>
public sealed class SharpCoreDbTegenrekeningRegelRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BewaarAsync_And_AlleAsync_RoundTripAllProperties()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.Matchregels.BewaarAsync(new TegenrekeningRegel("albert heijn", "4700", 3), Ct);

        var regel = Assert.Single(await context.Matchregels.AlleAsync(Ct));
        Assert.Equal("ALBERT HEIJN", regel.Trefwoord);
        Assert.Equal("4700", regel.TegenrekeningNummer);
        Assert.Equal(3, regel.Bevestigingen);
    }

    [Fact]
    public async Task AlleAsync_OrdersByConfirmations()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.Matchregels.BewaarAsync(new TegenrekeningRegel("energie", "4300", 1), Ct);
        await context.Matchregels.BewaarAsync(new TegenrekeningRegel("albert heijn", "4700", 7), Ct);

        var regels = await context.Matchregels.AlleAsync(Ct);

        Assert.Collection(
            regels,
            regel => Assert.Equal("ALBERT HEIJN", regel.Trefwoord),
            regel => Assert.Equal("ENERGIE", regel.Trefwoord));
    }

    [Fact]
    public async Task BewaarAsync_ReplacesTheRuleForTheSameKeyword()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.Matchregels.BewaarAsync(new TegenrekeningRegel("albert heijn", "4700", 1), Ct);

        await context.Matchregels.BewaarAsync(new TegenrekeningRegel("albert heijn", "4710", 2), Ct);

        var regel = Assert.Single(await context.Matchregels.AlleAsync(Ct));
        Assert.Equal("4710", regel.TegenrekeningNummer);
        Assert.Equal(2, regel.Bevestigingen);
    }

    [Fact]
    public async Task Rules_SurviveReopen()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.Matchregels.BewaarAsync(new TegenrekeningRegel("albert heijn", "4700", 4), Ct);

        await context.HeropenAsync();

        var regel = Assert.Single(await context.Matchregels.AlleAsync(Ct));
        Assert.Equal("4700", regel.TegenrekeningNummer);
        Assert.Equal(4, regel.Bevestigingen);
    }

    [Fact]
    public async Task BewaarAsync_ThrowsForNull()
    {
        await using var context = new SharpCoreDbTestContext();

        await Assert.ThrowsAsync<ArgumentNullException>(() => context.Matchregels.BewaarAsync(null!, Ct));
    }
}
