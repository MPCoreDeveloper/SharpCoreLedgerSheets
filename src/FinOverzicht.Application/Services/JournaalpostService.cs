using FinOverzicht.Application.Abstractions;
using FinOverzicht.Application.Exceptions;
using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Application.Services;

/// <summary>
/// Default <see cref="IJournaalpostService"/>.
/// </summary>
/// <remarks>
/// Account numbers are resolved to account identities here so the domain stays free of
/// presentation concerns, and so a typo results in a clear, localised error message.
/// </remarks>
public sealed class JournaalpostService(
    IRekeningRepository rekeningRepository,
    IJournaalpostRepository journaalpostRepository) : IJournaalpostService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<Journaalpost>> AlleAsync(Periode? periode = null, CancellationToken ct = default) =>
        journaalpostRepository.AlleAsync(periode, ct);

    /// <inheritdoc />
    public async Task<Journaalpost> BoekAsync(JournaalpostBoekenCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rekeningen = (await rekeningRepository.AlleAsync(ct).ConfigureAwait(false))
            .ToDictionary(r => r.Nummer, StringComparer.OrdinalIgnoreCase);

        var post = Journaalpost.Nieuw(command.Datum, command.Omschrijving, command.Referentie);

        foreach (var regel in command.Regels)
        {
            if (!rekeningen.TryGetValue(regel.RekeningNummer, out var rekening))
            {
                throw new NietGevondenException($"Rekening '{regel.RekeningNummer}' bestaat niet.");
            }

            if (!rekening.IsActief)
            {
                throw new DomeinException($"Rekening '{rekening.Nummer}' ({rekening.Naam}) is niet actief.");
            }

            post.VoegRegelToe(rekening.Id, regel.Debet, regel.Credit, regel.Omschrijving);
        }

        post.Valideer();
        await journaalpostRepository.ToevoegenAsync(post, ct).ConfigureAwait(false);
        return post;
    }

    /// <inheritdoc />
    public Task VerwijderAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Geef een geldig id op.", nameof(id));
        }

        return journaalpostRepository.VerwijderenAsync(id, ct);
    }
}
