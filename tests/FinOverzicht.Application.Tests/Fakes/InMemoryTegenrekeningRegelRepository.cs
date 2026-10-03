using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Bankzaken;

namespace FinOverzicht.Application.Tests.Fakes;

/// <summary>In-memory <see cref="ITegenrekeningRegelRepository"/> used by the application tests.</summary>
internal sealed class InMemoryTegenrekeningRegelRepository : ITegenrekeningRegelRepository
{
    private readonly Dictionary<string, TegenrekeningRegel> _regels = new(StringComparer.Ordinal);

    /// <summary>How often the rules were read; proves that a batch proposal reads them once.</summary>
    public int AantalKeerGelezen { get; private set; }

    public Task<IReadOnlyList<TegenrekeningRegel>> AlleAsync(CancellationToken ct = default)
    {
        AantalKeerGelezen++;

        return Task.FromResult<IReadOnlyList<TegenrekeningRegel>>(
            _regels.Values.OrderByDescending(regel => regel.Bevestigingen)
                .ThenBy(regel => regel.Trefwoord, StringComparer.Ordinal)
                .ToList());
    }

    public Task BewaarAsync(TegenrekeningRegel regel, CancellationToken ct = default)
    {
        _regels[regel.Trefwoord] = regel;
        return Task.CompletedTask;
    }
}
