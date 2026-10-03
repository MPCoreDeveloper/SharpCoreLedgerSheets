using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Bankzaken;
using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Opslag;

/// <summary>
/// SharpCoreDB-backed <see cref="ITegenrekeningRegelRepository"/>.
/// </summary>
/// <remarks>
/// A keyword identifies a rule, so storing one replaces the row with that keyword: an exception such
/// as "the rule for this keyword now points at another account" can never leave two conflicting rules
/// behind.
/// </remarks>
internal sealed class SharpCoreDbTegenrekeningRegelRepository(IDatabase database)
    : SharpCoreDbRepository(database), ITegenrekeningRegelRepository
{
    private const string Kolommen = "trefwoord, tegenrekening, bevestigingen";

    /// <inheritdoc />
    public Task<IReadOnlyList<TegenrekeningRegel>> AlleAsync(CancellationToken ct = default)
    {
        var rijen = MetLock(() => Database.ExecuteQuery(
            $"SELECT {Kolommen} FROM {BankstaatSchema.TabelMatchregels}"));

        IReadOnlyList<TegenrekeningRegel> regels = rijen
            .Select(NaarRegel)
            .OrderByDescending(regel => regel.Bevestigingen)
            .ThenBy(regel => regel.Trefwoord, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(regels);
    }

    /// <inheritdoc />
    public Task BewaarAsync(TegenrekeningRegel regel, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(regel);

        MetLock(() =>
        {
            Database.ExecuteSQL(
                $"DELETE FROM {BankstaatSchema.TabelMatchregels} WHERE trefwoord = @trefwoord",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@trefwoord"] = regel.Trefwoord,
                });

            Database.ExecuteSQL(
                $"INSERT INTO {BankstaatSchema.TabelMatchregels} ({Kolommen}) " +
                "VALUES (@trefwoord, @tegenrekening, @bevestigingen)",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@trefwoord"] = regel.Trefwoord,
                    ["@tegenrekening"] = regel.TegenrekeningNummer,
                    ["@bevestigingen"] = regel.Bevestigingen,
                });

            Database.Flush();
        });

        return Task.CompletedTask;
    }

    private static TegenrekeningRegel NaarRegel(IReadOnlyDictionary<string, object> rij) =>
        new(LeesTekst(rij, "trefwoord"), LeesTekst(rij, "tegenrekening"), LeesGeheel(rij, "bevestigingen"));
}
