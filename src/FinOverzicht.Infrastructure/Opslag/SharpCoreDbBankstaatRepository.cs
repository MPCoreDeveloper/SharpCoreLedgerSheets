using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Opslag;

/// <summary>
/// SharpCoreDB-backed <see cref="IBankstaatRepository"/>.
/// </summary>
/// <remarks>
/// A stored movement only ever changes status (open, booked or ignored) and the journal entry it was
/// booked with, so <see cref="BijwerkenAsync"/> writes just those two columns.
/// </remarks>
internal sealed class SharpCoreDbBankstaatRepository(IDatabase database)
    : SharpCoreDbRepository(database), IBankstaatRepository
{
    private const string Kolommen =
        "id, bank_nr, datum, bedrag, valuta, referentie, tegenpartij, iban, omschrijving, status, post_id";

    /// <inheritdoc />
    public Task<IReadOnlyList<BankTransactie>> AlleAsync(
        BankTransactieStatus? status = null,
        CancellationToken ct = default)
    {
        var rijen = status is null
            ? MetLock(() => Database.ExecuteQuery(
                $"SELECT {Kolommen} FROM {BankstaatSchema.TabelMutaties}"))
            : MetLock(() => Database.ExecuteQuery(
                $"SELECT {Kolommen} FROM {BankstaatSchema.TabelMutaties} WHERE status = @status",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@status"] = (int)status.Value,
                }));

        IReadOnlyList<BankTransactie> transacties = rijen
            .Select(NaarTransactie)
            .OrderBy(transactie => transactie.Datum)
            .ThenBy(transactie => transactie.Referentie, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(transacties);
    }

    /// <inheritdoc />
    public Task<BankTransactie?> ZoekAsync(Guid id, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["@id"] = NaarTekst(id),
        };

        var rijen = MetLock(() => Database.ExecuteQuery(
            $"SELECT {Kolommen} FROM {BankstaatSchema.TabelMutaties} WHERE id = @id", parameters));

        return Task.FromResult(rijen.Count == 0 ? null : NaarTransactie(rijen[0]));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> BestaandeSleutelsAsync(CancellationToken ct = default)
    {
        var rijen = MetLock(() => Database.ExecuteQuery(
            $"SELECT {Kolommen} FROM {BankstaatSchema.TabelMutaties}"));

        // The key is derived from the movement itself, so reading the rows keeps the normalisation in
        // one place instead of duplicating it here.
        IReadOnlyList<string> sleutels = rijen
            .Select(rij => NaarTransactie(rij).Sleutel)
            .ToList();

        return Task.FromResult(sleutels);
    }

    /// <inheritdoc />
    public Task ToevoegenAsync(IEnumerable<BankTransactie> transacties, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transacties);

        var nieuw = transacties.ToList();

        if (nieuw.Count == 0)
        {
            return Task.CompletedTask;
        }

        MetLock(() =>
        {
            foreach (var transactie in nieuw)
            {
                Database.ExecuteSQL(
                    $"INSERT INTO {BankstaatSchema.TabelMutaties} ({Kolommen}) VALUES (@id, @bank_nr, " +
                    "@datum, @bedrag, @valuta, @referentie, @tegenpartij, @iban, @omschrijving, @status, @post_id)",
                    Parameters(transactie));
            }

            // A single flush: an import writes a whole statement in one go.
            Database.Flush();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task BijwerkenAsync(BankTransactie transactie, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transactie);

        MetLock(() =>
        {
            Database.ExecuteSQL(
                $"UPDATE {BankstaatSchema.TabelMutaties} SET status = @status, post_id = @post_id WHERE id = @id",
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["@id"] = NaarTekst(transactie.Id),
                    ["@status"] = (int)transactie.Status,
                    ["@post_id"] = transactie.JournaalpostId is { } post ? NaarTekst(post) : null,
                });
            Database.Flush();
        });

        return Task.CompletedTask;
    }

    private static Dictionary<string, object?> Parameters(BankTransactie transactie) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["@id"] = NaarTekst(transactie.Id),
            ["@bank_nr"] = transactie.BankRekeningNummer,
            ["@datum"] = NaarTekst(transactie.Datum),
            ["@bedrag"] = transactie.Mutatie.Waarde,
            ["@valuta"] = transactie.Mutatie.Valuta.Code,
            ["@referentie"] = transactie.Referentie,
            ["@tegenpartij"] = transactie.TegenpartijNaam,
            ["@iban"] = transactie.TegenpartijIban,
            ["@omschrijving"] = transactie.Omschrijving,
            ["@status"] = (int)transactie.Status,
            ["@post_id"] = transactie.JournaalpostId is { } post ? NaarTekst(post) : null,
        };

    private static BankTransactie NaarTransactie(IReadOnlyDictionary<string, object> rij)
    {
        var transactie = new BankTransactie(
            LeesGuid(rij, "id"),
            LeesTekst(rij, "bank_nr"),
            LeesDatum(rij, "datum"),
            new Bedrag(LeesGeld(rij, "bedrag"), Valuta.VanCode(LeesTekst(rij, "valuta"))),
            LeesTekst(rij, "referentie"),
            LeesOptioneleTekst(rij, "tegenpartij"),
            LeesOptioneleTekst(rij, "iban"),
            LeesOptioneleTekst(rij, "omschrijving"));

        var postId = LeesGuid(rij, "post_id");

        return (BankTransactieStatus)LeesGeheel(rij, "status") switch
        {
            BankTransactieStatus.Geboekt when postId != Guid.Empty => transactie.MarkeerGeboekt(postId),
            BankTransactieStatus.Genegeerd => transactie.Negeer(),
            _ => transactie,
        };
    }
}
