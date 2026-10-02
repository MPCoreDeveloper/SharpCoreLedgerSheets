using FinOverzicht.Application.Tests.Fakes;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Tests.Services;

public sealed class GrootboekLaderTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LaadAsync_RehydratesAccountsAndEntriesWithOriginalIds()
    {
        var administratie = new TestAdministratie();
        await administratie.SeedAsync(Ct);

        var grootboek = await administratie.Lader.LaadAsync(Ct);

        Assert.Equal(9, grootboek.Rekeningen.Count);
        Assert.Equal(8, grootboek.Posten.Count);
        Assert.Equal(0m, grootboek.ControleTotaal());
        Assert.All(grootboek.Rekeningen, r => Assert.NotEqual(Guid.Empty, r.Id));
    }

    [Fact]
    public async Task LaadAsync_PreservesBalancesAcrossRehydration()
    {
        var administratie = new TestAdministratie();
        await administratie.SeedAsync(Ct);

        var grootboek = await administratie.Lader.LaadAsync(Ct);

        var kas = grootboek.ZoekRekening("1000");
        Assert.NotNull(kas);
        Assert.Equal(14_000m, grootboek.Saldo(kas!.Id));
    }

    [Fact]
    public async Task LaadAsync_AllowsInactiveAccountsInHistoricalEntries()
    {
        var rekeningen = new InMemoryRekeningRepository();
        var posten = new InMemoryJournaalpostRepository();
        var rekeningService = new Application.Services.RekeningService(rekeningen);
        var postService = new Application.Services.JournaalpostService(rekeningen, posten);
        var lader = new Application.Services.GrootboekLader(rekeningen, posten);

        await rekeningService.VoegToeAsync(new Application.Models.RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa, Vlottend: true, LiquideMiddel: true), Ct);
        await rekeningService.VoegToeAsync(new Application.Models.RekeningToevoegenCommand("8000", "Omzet", RekeningSoort.Opbrengsten), Ct);
        await postService.BoekAsync(new Application.Models.JournaalpostBoekenCommand(
            new DateOnly(2026, 1, 10), "Verkoop",
            [new("1000", 100m, 0m), new("8000", 0m, 100m)]), Ct);

        await rekeningService.DeactiveerAsync("8000", Ct);

        var grootboek = await lader.LaadAsync(Ct);

        Assert.Single(grootboek.Posten);
    }
}
