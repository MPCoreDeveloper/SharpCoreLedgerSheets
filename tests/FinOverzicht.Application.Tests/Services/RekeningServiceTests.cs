using FinOverzicht.Application.Exceptions;
using FinOverzicht.Application.Models;
using FinOverzicht.Application.Services;
using FinOverzicht.Application.Tests.Fakes;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using FluentAssertions;

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

        rekening.Nummer.Should().Be("1000");
        rekening.IsLiquideMiddel.Should().BeTrue();
        (await _service.AlleAsync(Ct)).Should().ContainSingle();
    }

    [Fact]
    public async Task VoegToeAsync_ThrowsForDuplicateNumber()
    {
        await _service.VoegToeAsync(new RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa), Ct);

        var act = () => _service.VoegToeAsync(new RekeningToevoegenCommand("1000", "Kas 2", RekeningSoort.Activa), Ct);

        await act.Should().ThrowAsync<DomeinException>();
    }

    [Fact]
    public async Task VoegToeAsync_ThrowsForNullCommand()
    {
        var act = () => _service.VoegToeAsync(null!, Ct);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HernoemAsync_UpdatesStoredAccount()
    {
        await _service.VoegToeAsync(new RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa), Ct);

        await _service.HernoemAsync("1000", "Kas en bank", Ct);

        var rekening = await _repository.ZoekOpNummerAsync("1000", Ct);
        rekening!.Naam.Should().Be("Kas en bank");
    }

    [Fact]
    public async Task HernoemAsync_ThrowsWhenAccountIsUnknown()
    {
        var act = () => _service.HernoemAsync("9999", "Onbekend", Ct);

        await act.Should().ThrowAsync<NietGevondenException>();
    }

    [Fact]
    public async Task DeactiveerAsync_MarksAccountInactive()
    {
        await _service.VoegToeAsync(new RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa), Ct);

        await _service.DeactiveerAsync("1000", Ct);

        (await _repository.ZoekOpNummerAsync("1000", Ct))!.IsActief.Should().BeFalse();
    }

    [Fact]
    public async Task DeactiveerAsync_ThrowsWhenAccountIsUnknown()
    {
        var act = () => _service.DeactiveerAsync("9999", Ct);

        await act.Should().ThrowAsync<NietGevondenException>();
    }
}
