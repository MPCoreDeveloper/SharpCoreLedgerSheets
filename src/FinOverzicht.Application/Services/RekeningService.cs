using FinOverzicht.Application.Abstractions;
using FinOverzicht.Application.Exceptions;
using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Application.Services;

/// <summary>
/// Default <see cref="IRekeningService"/>.
/// </summary>
public sealed class RekeningService(IRekeningRepository repository) : IRekeningService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<Rekening>> AlleAsync(CancellationToken ct = default) =>
        repository.AlleAsync(ct);

    /// <inheritdoc />
    public async Task<Rekening> VoegToeAsync(RekeningToevoegenCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await repository.ZoekOpNummerAsync(command.Nummer, ct).ConfigureAwait(false) is not null)
        {
            throw new DomeinException($"Rekeningnummer '{command.Nummer}' bestaat al.");
        }

        var rekening = new Rekening(
            Guid.NewGuid(),
            command.Nummer,
            command.Naam,
            command.Soort,
            isActief: true,
            isVlottend: command.Vlottend,
            isLiquideMiddel: command.LiquideMiddel,
            isVoorraad: command.Voorraad);

        await repository.ToevoegenAsync(rekening, ct).ConfigureAwait(false);
        return rekening;
    }

    /// <inheritdoc />
    public async Task HernoemAsync(string nummer, string nieuweNaam, CancellationToken ct = default)
    {
        var rekening = await ZoekAsync(nummer, ct).ConfigureAwait(false);
        rekening.Hernoem(nieuweNaam);
        await repository.BijwerkenAsync(rekening, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeactiveerAsync(string nummer, CancellationToken ct = default)
    {
        var rekening = await ZoekAsync(nummer, ct).ConfigureAwait(false);
        rekening.Deactiveer();
        await repository.BijwerkenAsync(rekening, ct).ConfigureAwait(false);
    }

    private async Task<Rekening> ZoekAsync(string nummer, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nummer);

        return await repository.ZoekOpNummerAsync(nummer, ct).ConfigureAwait(false)
            ?? throw new NietGevondenException($"Rekening '{nummer}' is niet gevonden.");
    }
}
