using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Boekhouden;
using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Opslag;

/// <summary>
/// SharpCoreDB-backed <see cref="IJournaalpostRepository"/>.
/// </summary>
/// <remarks>
/// A journal entry is stored as one row in <c>journaalposten</c> plus one row per line in
/// <c>journaalpost_regels</c>. The period filter is applied in memory: the volumes are small and
/// this keeps the query free of string-collation assumptions.
/// </remarks>
internal sealed class SharpCoreDbJournaalpostRepository(IDatabase database)
    : SharpCoreDbRepository(database), IJournaalpostRepository
{
    private const string PostKolommen = "id, datum, omschrijving, referentie";

    private const string RegelKolommen =
        "id, post_id, volgnummer, rekening_id, debet, credit, omschrijving";

    /// <inheritdoc />
    public Task<IReadOnlyList<Journaalpost>> AlleAsync(Periode? periode = null, CancellationToken ct = default)
    {
        var postRijen = MetLock(() => Database.ExecuteQuery(
            $"SELECT {PostKolommen} FROM {GrootboekSchema.TabelPosten}"));

        var regelRijen = MetLock(() => Database.ExecuteQuery(
            $"SELECT {RegelKolommen} FROM {GrootboekSchema.TabelRegels}"));

        var regelsPerPost = regelRijen
            .GroupBy(rij => LeesGuid(rij, "post_id"))
            .ToDictionary(
                groep => groep.Key,
                groep => groep.OrderBy(rij => LeesGeheel(rij, "volgnummer")).ToList());

        var posten = new List<Journaalpost>(postRijen.Count);

        foreach (var rij in postRijen)
        {
            var id = LeesGuid(rij, "id");
            var datum = LeesDatum(rij, "datum");

            if (periode.HasValue && !periode.Value.Bevat(datum))
            {
                continue;
            }

            var post = new Journaalpost(id, datum, LeesTekst(rij, "omschrijving"), LeesOptioneleTekst(rij, "referentie"));

            if (regelsPerPost.TryGetValue(id, out var regels))
            {
                foreach (var regel in regels)
                {
                    post.VoegRegelToe(
                        LeesGuid(regel, "rekening_id"),
                        LeesGeld(regel, "debet"),
                        LeesGeld(regel, "credit"),
                        LeesOptioneleTekst(regel, "omschrijving"));
                }
            }

            posten.Add(post);
        }

        IReadOnlyList<Journaalpost> resultaat = posten
            .OrderBy(post => post.Datum)
            .ThenBy(post => post.Id)
            .ToList();

        return Task.FromResult(resultaat);
    }

    /// <inheritdoc />
    public Task ToevoegenAsync(Journaalpost post, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        MetLock(() =>
        {
            Database.ExecuteSQL(
                $"INSERT INTO {GrootboekSchema.TabelPosten} ({PostKolommen}) " +
                "VALUES (@id, @datum, @omschrijving, @referentie)",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@id"] = NaarTekst(post.Id),
                    ["@datum"] = NaarTekst(post.Datum),
                    ["@omschrijving"] = post.Omschrijving,
                    ["@referentie"] = post.Referentie ?? string.Empty,
                });

            var volgnummer = 0;

            foreach (var regel in post.Regels)
            {
                Database.ExecuteSQL(
                    $"INSERT INTO {GrootboekSchema.TabelRegels} ({RegelKolommen}) " +
                    "VALUES (@id, @post_id, @volgnummer, @rekening_id, @debet, @credit, @omschrijving)",
                    new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["@id"] = NaarTekst(Guid.NewGuid()),
                        ["@post_id"] = NaarTekst(post.Id),
                        ["@volgnummer"] = volgnummer++,
                        ["@rekening_id"] = NaarTekst(regel.RekeningId),
                        ["@debet"] = regel.Debet,
                        ["@credit"] = regel.Credit,
                        ["@omschrijving"] = regel.Omschrijving ?? string.Empty,
                    });
            }

            Database.Flush();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task VerwijderenAsync(Guid id, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["@id"] = NaarTekst(id),
        };

        MetLock(() =>
        {
            Database.ExecuteSQL($"DELETE FROM {GrootboekSchema.TabelRegels} WHERE post_id = @id", parameters);
            Database.ExecuteSQL($"DELETE FROM {GrootboekSchema.TabelPosten} WHERE id = @id", parameters);
            Database.Flush();
        });

        return Task.CompletedTask;
    }
}
