using FinOverzicht.Application.Exceptions;
using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Application.Tests.Services;

public sealed class JournaalpostServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<TestAdministratie> MetRekeningenAsync()
    {
        var administratie = new TestAdministratie();
        await administratie.RekeningService.VoegToeAsync(
            new RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa, Vlottend: true, LiquideMiddel: true), Ct);
        await administratie.RekeningService.VoegToeAsync(
            new RekeningToevoegenCommand("8000", "Omzet", RekeningSoort.Opbrengsten), Ct);
        return administratie;
    }

    [Fact]
    public async Task BoekAsync_ResolvesAccountNumbersAndPersistsEntry()
    {
        var administratie = await MetRekeningenAsync();
        var command = new JournaalpostBoekenCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("8000", 0m, 250m)]);

        var post = await administratie.JournaalpostService.BoekAsync(command, Ct);

        Assert.Equal(2, post.Regels.Count);
        Assert.True(post.IsInBalans);
        Assert.Single(await administratie.JournaalpostService.AlleAsync(null, Ct));
    }

    [Fact]
    public async Task BoekAsync_ThrowsWhenAccountNumberIsUnknown()
    {
        var administratie = await MetRekeningenAsync();
        var command = new JournaalpostBoekenCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("9999", 0m, 250m)]);

        await Assert.ThrowsAsync<NietGevondenException>(
            () => administratie.JournaalpostService.BoekAsync(command, Ct));
    }

    [Fact]
    public async Task BoekAsync_ThrowsWhenAccountIsInactive()
    {
        var administratie = await MetRekeningenAsync();
        await administratie.RekeningService.DeactiveerAsync("8000", Ct);
        var command = new JournaalpostBoekenCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("8000", 0m, 250m)]);

        var exceptie = await Assert.ThrowsAsync<DomeinException>(
            () => administratie.JournaalpostService.BoekAsync(command, Ct));

        Assert.Contains("niet actief", exceptie.Message);
    }

    [Fact]
    public async Task BoekAsync_ThrowsWhenEntryIsUnbalanced()
    {
        var administratie = await MetRekeningenAsync();
        var command = new JournaalpostBoekenCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("8000", 0m, 100m)]);

        var exceptie = await Assert.ThrowsAsync<DomeinException>(
            () => administratie.JournaalpostService.BoekAsync(command, Ct));

        Assert.Contains("niet in balans", exceptie.Message);
    }

    [Fact]
    public async Task BoekAsync_ThrowsForNullCommand()
    {
        var administratie = await MetRekeningenAsync();

        await Assert.ThrowsAsync<ArgumentNullException>(() => administratie.JournaalpostService.BoekAsync(null!, Ct));
    }

    [Fact]
    public async Task AlleAsync_FiltersByPeriod()
    {
        var administratie = await MetRekeningenAsync();
        await administratie.JournaalpostService.BoekAsync(
            new JournaalpostBoekenCommand(new DateOnly(2025, 12, 31), "Vorig jaar",
                [new("1000", 10m, 0m), new("8000", 0m, 10m)]), Ct);
        await administratie.JournaalpostService.BoekAsync(
            new JournaalpostBoekenCommand(new DateOnly(2026, 3, 1), "Dit jaar",
                [new("1000", 20m, 0m), new("8000", 0m, 20m)]), Ct);

        var resultaat = await administratie.JournaalpostService.AlleAsync(Periode.VoorJaar(2026), Ct);

        Assert.Single(resultaat, p => p.Omschrijving == "Dit jaar");
    }

    [Fact]
    public async Task VerwijderAsync_RemovesEntry()
    {
        var administratie = await MetRekeningenAsync();
        var post = await administratie.JournaalpostService.BoekAsync(
            new JournaalpostBoekenCommand(new DateOnly(2026, 3, 1), "Verkoop",
                [new("1000", 10m, 0m), new("8000", 0m, 10m)]), Ct);

        await administratie.JournaalpostService.VerwijderAsync(post.Id, Ct);

        Assert.Empty(await administratie.JournaalpostService.AlleAsync(null, Ct));
    }

    [Fact]
    public async Task VerwijderAsync_ThrowsForEmptyId()
    {
        var administratie = await MetRekeningenAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => administratie.JournaalpostService.VerwijderAsync(Guid.Empty, Ct));
    }
}
