using FinOverzicht.Application.Exceptions;
using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using FluentAssertions;

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

        post.Regels.Should().HaveCount(2);
        post.IsInBalans.Should().BeTrue();
        (await administratie.JournaalpostService.AlleAsync(null, Ct)).Should().ContainSingle();
    }

    [Fact]
    public async Task BoekAsync_ThrowsWhenAccountNumberIsUnknown()
    {
        var administratie = await MetRekeningenAsync();
        var command = new JournaalpostBoekenCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("9999", 0m, 250m)]);

        var act = () => administratie.JournaalpostService.BoekAsync(command, Ct);

        await act.Should().ThrowAsync<NietGevondenException>();
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

        var act = () => administratie.JournaalpostService.BoekAsync(command, Ct);

        await act.Should().ThrowAsync<DomeinException>().WithMessage("*niet actief*");
    }

    [Fact]
    public async Task BoekAsync_ThrowsWhenEntryIsUnbalanced()
    {
        var administratie = await MetRekeningenAsync();
        var command = new JournaalpostBoekenCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("8000", 0m, 100m)]);

        var act = () => administratie.JournaalpostService.BoekAsync(command, Ct);

        await act.Should().ThrowAsync<DomeinException>().WithMessage("*niet in balans*");
    }

    [Fact]
    public async Task BoekAsync_ThrowsForNullCommand()
    {
        var administratie = await MetRekeningenAsync();

        var act = () => administratie.JournaalpostService.BoekAsync(null!, Ct);

        await act.Should().ThrowAsync<ArgumentNullException>();
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

        resultaat.Should().ContainSingle(p => p.Omschrijving == "Dit jaar");
    }

    [Fact]
    public async Task VerwijderAsync_RemovesEntry()
    {
        var administratie = await MetRekeningenAsync();
        var post = await administratie.JournaalpostService.BoekAsync(
            new JournaalpostBoekenCommand(new DateOnly(2026, 3, 1), "Verkoop",
                [new("1000", 10m, 0m), new("8000", 0m, 10m)]), Ct);

        await administratie.JournaalpostService.VerwijderAsync(post.Id, Ct);

        (await administratie.JournaalpostService.AlleAsync(null, Ct)).Should().BeEmpty();
    }

    [Fact]
    public async Task VerwijderAsync_ThrowsForEmptyId()
    {
        var administratie = await MetRekeningenAsync();

        var act = () => administratie.JournaalpostService.VerwijderAsync(Guid.Empty, Ct);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
