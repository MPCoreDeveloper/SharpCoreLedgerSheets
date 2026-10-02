using FinOverzicht.Application.Exceptions;
using FinOverzicht.Application.Models;
using FinOverzicht.Application.Services;
using FinOverzicht.Application.Tests.Fakes;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Application.Tests.Services;

public sealed class RekeningServiceTests
{
    private readonly InMemoryRekeningRepository _repository = new();
    private readonly RekeningService _service;

    public RekeningServiceTests() => _service = new RekeningService(_repository);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task VoegToeAsync_PersistsAccountAndReturnsIt()
    {
        var rekening = await _service.VoegToeAsync(
            new RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa, Vlottend: true, LiquideMiddel: true), Ct);

        Assert.Equal("1000", rekening.Nummer);
        Assert.True(rekening.IsLiquideMiddel);
        Assert.Single(await _service.AlleAsync(Ct));
    }

    [Fact]
    public async Task VoegToeAsync_ThrowsForDuplicateNumber()
    {
        await _service.VoegToeAsync(new RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa), Ct);

        await Assert.ThrowsAsync<DomeinException>(
            () => _service.VoegToeAsync(new RekeningToevoegenCommand("1000", "Kas 2", RekeningSoort.Activa), Ct));
    }

    [Fact]
    public async Task VoegToeAsync_ThrowsForNullCommand()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.VoegToeAsync(null!, Ct));
    }

    [Fact]
    public async Task HernoemAsync_UpdatesStoredAccount()
    {
        await _service.VoegToeAsync(new RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa), Ct);

        await _service.HernoemAsync("1000", "Kas en bank", Ct);

        var rekening = await _repository.ZoekOpNummerAsync("1000", Ct);
        Assert.Equal("Kas en bank", rekening!.Naam);
    }

    [Fact]
    public async Task HernoemAsync_ThrowsWhenAccountIsUnknown()
    {
        await Assert.ThrowsAsync<NietGevondenException>(() => _service.HernoemAsync("9999", "Onbekend", Ct));
    }

    [Fact]
    public async Task DeactiveerAsync_MarksAccountInactive()
    {
        await _service.VoegToeAsync(new RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa), Ct);

        await _service.DeactiveerAsync("1000", Ct);

        Assert.False((await _repository.ZoekOpNummerAsync("1000", Ct))!.IsActief);
    }

    [Fact]
    public async Task DeactiveerAsync_ThrowsWhenAccountIsUnknown()
    {
        await Assert.ThrowsAsync<NietGevondenException>(() => _service.DeactiveerAsync("9999", Ct));
    }
}
