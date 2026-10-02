using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Boekhouden;
using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Opslag;

/// <summary>
/// SharpCoreDB-backed <see cref="IRekeningRepository"/>.
/// </summary>
internal sealed class SharpCoreDbRekeningRepository(IDatabase database)
    : SharpCoreDbRepository(database), IRekeningRepository
{
    private const string Kolommen =
        "id, nummer, naam, soort, is_actief, is_vlottend, is_liquide_middel, is_voorraad";

    /// <inheritdoc />
    public Task<IReadOnlyList<Rekening>> AlleAsync(CancellationToken ct = default)
    {
        var rijen = MetLock(() => Database.ExecuteQuery(
            $"SELECT {Kolommen} FROM {GrootboekSchema.TabelRekeningen}"));

        IReadOnlyList<Rekening> rekeningen = rijen
            .Select(NaarRekening)
            .OrderBy(r => r.Nummer, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(rekeningen);
    }

    /// <inheritdoc />
    public Task<Rekening?> ZoekOpNummerAsync(string nummer, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nummer);

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["@nummer"] = nummer,
        };

        var rijen = MetLock(() => Database.ExecuteQuery(
            $"SELECT {Kolommen} FROM {GrootboekSchema.TabelRekeningen} WHERE nummer = @nummer", parameters));

        return Task.FromResult(rijen.Count == 0 ? null : NaarRekening(rijen[0]));
    }

    /// <inheritdoc />
    public Task ToevoegenAsync(Rekening rekening, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rekening);

        MetLock(() =>
        {
            Database.ExecuteSQL(
                $"INSERT INTO {GrootboekSchema.TabelRekeningen} ({Kolommen}) " +
                "VALUES (@id, @nummer, @naam, @soort, @is_actief, @is_vlottend, @is_liquide_middel, @is_voorraad)",
                Parameters(rekening));
            Database.Flush();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task BijwerkenAsync(Rekening rekening, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rekening);

        MetLock(() =>
        {
            Database.ExecuteSQL(
                $"UPDATE {GrootboekSchema.TabelRekeningen} SET nummer = @nummer, naam = @naam, soort = @soort, " +
                "is_actief = @is_actief, is_vlottend = @is_vlottend, is_liquide_middel = @is_liquide_middel, " +
                "is_voorraad = @is_voorraad WHERE id = @id",
                Parameters(rekening));
            Database.Flush();
        });

        return Task.CompletedTask;
    }

    private static Dictionary<string, object?> Parameters(Rekening rekening) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["@id"] = NaarTekst(rekening.Id),
            ["@nummer"] = rekening.Nummer,
            ["@naam"] = rekening.Naam,
            ["@soort"] = (int)rekening.Soort,
            ["@is_actief"] = rekening.IsActief ? 1 : 0,
            ["@is_vlottend"] = rekening.IsVlottend ? 1 : 0,
            ["@is_liquide_middel"] = rekening.IsLiquideMiddel ? 1 : 0,
            ["@is_voorraad"] = rekening.IsVoorraad ? 1 : 0,
        };

    private static Rekening NaarRekening(IReadOnlyDictionary<string, object> rij) =>
        new(
            LeesGuid(rij, "id"),
            LeesTekst(rij, "nummer"),
            LeesTekst(rij, "naam"),
            (RekeningSoort)LeesGeheel(rij, "soort"),
            LeesBool(rij, "is_actief"),
            LeesBool(rij, "is_vlottend"),
            LeesBool(rij, "is_liquide_middel"),
            LeesBool(rij, "is_voorraad"));
}
