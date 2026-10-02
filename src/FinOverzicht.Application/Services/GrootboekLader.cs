using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Services;

/// <summary>
/// Default <see cref="IGrootboekLader"/>: rehydrates the ledger from the repositories.
/// </summary>
public sealed class GrootboekLader(
    IRekeningRepository rekeningRepository,
    IJournaalpostRepository journaalpostRepository) : IGrootboekLader
{
    /// <inheritdoc />
    public async Task<Grootboek> LaadAsync(CancellationToken ct = default)
    {
        var grootboek = new Grootboek();

        foreach (var rekening in await rekeningRepository.AlleAsync(ct).ConfigureAwait(false))
        {
            grootboek.RegistreerRekening(rekening);
        }

        foreach (var post in await journaalpostRepository.AlleAsync(periode: null, ct).ConfigureAwait(false))
        {
            grootboek.Herstel(post);
        }

        return grootboek;
    }
}
