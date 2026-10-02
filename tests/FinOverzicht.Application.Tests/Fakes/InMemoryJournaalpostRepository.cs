using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Tests.Fakes;

/// <summary>In-memory <see cref="IJournaalpostRepository"/> used by the application tests.</summary>
internal sealed class InMemoryJournaalpostRepository : IJournaalpostRepository
{
    private readonly Dictionary<Guid, Journaalpost> _posten = [];

    public Task<IReadOnlyList<Journaalpost>> AlleAsync(Periode? periode = null, CancellationToken ct = default)
    {
        IEnumerable<Journaalpost> query = _posten.Values;

        if (periode.HasValue)
        {
            var grens = periode.Value;
            query = query.Where(post => grens.Bevat(post.Datum));
        }

        return Task.FromResult<IReadOnlyList<Journaalpost>>(query.OrderBy(p => p.Datum).ToList());
    }

    public Task ToevoegenAsync(Journaalpost post, CancellationToken ct = default)
    {
        _posten[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task VerwijderenAsync(Guid id, CancellationToken ct = default)
    {
        _posten.Remove(id);
        return Task.CompletedTask;
    }
}
