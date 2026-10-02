using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Rapporteren;

namespace FinOverzicht.Domain.Services;

/// <summary>
/// Default implementation of <see cref="IWinstVerliesOpsteller"/>.
/// </summary>
/// <remarks>
/// Only revenue and expense accounts contribute; the amounts are the movements within the period.
/// Accounts with no movement in the period are omitted.
/// </remarks>
public sealed class WinstVerliesOpsteller : IWinstVerliesOpsteller
{
    /// <inheritdoc />
    public WinstVerlies StelOp(Grootboek grootboek, Periode periode)
    {
        ArgumentNullException.ThrowIfNull(grootboek);

        var opbrengsten = new List<SaldoRegel>();
        var kosten = new List<SaldoRegel>();

        foreach (var rekening in grootboek.Rekeningen.OrderBy(r => r.Nummer, StringComparer.Ordinal))
        {
            var mutatie = grootboek.MutatieInPeriode(rekening.Id, periode);
            if (mutatie == 0m)
            {
                continue;
            }

            switch (rekening.Soort)
            {
                case RekeningSoort.Opbrengsten:
                    // Revenue has a credit balance; negate so the reported amount is positive.
                    opbrengsten.Add(new SaldoRegel(rekening.Nummer, rekening.Naam, -mutatie));
                    break;

                case RekeningSoort.Kosten:
                    kosten.Add(new SaldoRegel(rekening.Nummer, rekening.Naam, mutatie));
                    break;

                default:
                    break;
            }
        }

        return new WinstVerlies(
            opbrengsten,
            kosten,
            opbrengsten.Sum(r => r.Bedrag),
            kosten.Sum(r => r.Bedrag),
            periode);
    }
}
