using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Application.Tests.Fakes;

/// <summary>In-memory <see cref="IRekeningRepository"/> used by the application tests.</summary>
internal sealed class InMemoryRekeningRepository : IRekeningRepository
{
    private readonly Dictionary<Guid, Rekening> _rekeningen = [];

    /// <summary>How often the accounts were read; proves that a batch proposal reads them once.</summary>
    public int AantalKeerGelezen { get; private set; }

    public Task<IReadOnlyList<Rekening>> AlleAsync(CancellationToken ct = default)
    {
        AantalKeerGelezen++;

        return Task.FromResult<IReadOnlyList<Rekening>>(
            _rekeningen.Values.OrderBy(r => r.Nummer, StringComparer.Ordinal).ToList());
    }

    public Task<Rekening?> ZoekOpNummerAsync(string nummer, CancellationToken ct = default) =>
        Task.FromResult(_rekeningen.Values.FirstOrDefault(
            r => string.Equals(r.Nummer, nummer, StringComparison.OrdinalIgnoreCase)));

    public Task ToevoegenAsync(Rekening rekening, CancellationToken ct = default)
    {
        if (_rekeningen.ContainsKey(rekening.Id))
        {
            throw new DomeinException("Deze rekening bestaat al in de opslag.");
        }

        _rekeningen.Add(rekening.Id, rekening);
        return Task.CompletedTask;
    }

    public Task BijwerkenAsync(Rekening rekening, CancellationToken ct = default)
    {
        if (!_rekeningen.ContainsKey(rekening.Id))
        {
            throw new DomeinException("Deze rekening bestaat niet in de opslag.");
        }

        _rekeningen[rekening.Id] = rekening;
        return Task.CompletedTask;
    }
}
