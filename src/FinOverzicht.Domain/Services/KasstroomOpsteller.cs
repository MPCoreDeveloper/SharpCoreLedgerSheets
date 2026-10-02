using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Rapporteren;

namespace FinOverzicht.Domain.Services;

/// <summary>
/// Default implementation of <see cref="IKasstroomOpsteller"/>.
/// </summary>
/// <remarks>
/// The statement is derived directly from the ledger. Every journal entry that touches a cash
/// account (<see cref="Rekening.IsLiquideMiddel"/>) produces a cash movement which is classified
/// by its counterpart accounts:
/// <list type="bullet">
///   <item>revenue or expense counterpart = operating activity;</item>
///   <item>non-cash asset counterpart = investing activity;</item>
///   <item>liability or equity counterpart = financing activity.</item>
/// </list>
/// Movements are aggregated per category and description.
/// </remarks>
public sealed class KasstroomOpsteller : IKasstroomOpsteller
{
    /// <inheritdoc />
    public KasstroomOverzicht StelOp(Grootboek grootboek, Periode periode)
    {
        ArgumentNullException.ThrowIfNull(grootboek);

        var rekeningen = grootboek.Rekeningen.ToDictionary(r => r.Id);
        var liquide = grootboek.Rekeningen.Where(r => r.IsLiquideMiddel).Select(r => r.Id).ToHashSet();

        var beginSaldo = SaldoLiquide(grootboek, liquide, periode.Start == DateOnly.MinValue ? null : periode.Start.AddDays(-1));
        var eindSaldo = SaldoLiquide(grootboek, liquide, periode.Eind);

        var perCategorie = new Dictionary<KasstroomCategorie, Dictionary<string, decimal>>(3)
        {
            [KasstroomCategorie.Operationeel] = [],
            [KasstroomCategorie.Investering] = [],
            [KasstroomCategorie.Financiering] = [],
        };

        foreach (var post in grootboek.Posten.Where(p => periode.Bevat(p.Datum)))
        {
            if (post.Regels.All(r => !liquide.Contains(r.RekeningId)))
            {
                continue;
            }

            var kasMutatie = post.Regels.Where(r => liquide.Contains(r.RekeningId)).Sum(r => r.Mutatie);
            if (kasMutatie == 0m)
            {
                continue;
            }

            var tegenpartijen = post.Regels.Where(r => !liquide.Contains(r.RekeningId)).ToList();
            var categorie = BepaalCategorie(tegenpartijen, rekeningen);
            var omschrijving = string.IsNullOrWhiteSpace(post.Omschrijving) ? "Overige kasstroom" : post.Omschrijving;

            var categorieRegels = perCategorie[categorie];
            categorieRegels[omschrijving] = categorieRegels.TryGetValue(omschrijving, out var huidig)
                ? huidig + kasMutatie
                : kasMutatie;
        }

        return new KasstroomOverzicht(
            NaarRegels(perCategorie[KasstroomCategorie.Operationeel]),
            NaarRegels(perCategorie[KasstroomCategorie.Investering]),
            NaarRegels(perCategorie[KasstroomCategorie.Financiering]),
            beginSaldo,
            eindSaldo,
            periode);
    }

    private static decimal SaldoLiquide(Grootboek grootboek, IReadOnlySet<Guid> liquide, DateOnly? totEnMet)
    {
        if (liquide.Count == 0)
        {
            return 0m;
        }

        return liquide.Sum(rekeningId => grootboek.Saldo(rekeningId, totEnMet));
    }

    private static KasstroomCategorie BepaalCategorie(
        IReadOnlyList<JournaalpostRegel> tegenpartijen,
        IReadOnlyDictionary<Guid, Rekening> rekeningen)
    {
        var soorten = tegenpartijen
            .Where(r => rekeningen.ContainsKey(r.RekeningId))
            .Select(r => rekeningen[r.RekeningId].Soort)
            .ToHashSet();

        if (soorten.Contains(RekeningSoort.Opbrengsten) || soorten.Contains(RekeningSoort.Kosten))
        {
            return KasstroomCategorie.Operationeel;
        }

        if (soorten.Contains(RekeningSoort.Activa))
        {
            return KasstroomCategorie.Investering;
        }

        if (soorten.Contains(RekeningSoort.Passiva) || soorten.Contains(RekeningSoort.EigenVermogen))
        {
            return KasstroomCategorie.Financiering;
        }

        return KasstroomCategorie.Operationeel;
    }

    private static List<SaldoRegel> NaarRegels(Dictionary<string, decimal> mutaties) =>
        mutaties
            .Where(kv => kv.Value != 0m)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new SaldoRegel(null, kv.Key, kv.Value))
            .ToList();
}
