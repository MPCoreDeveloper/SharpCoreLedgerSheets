using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Rapporteren;

namespace FinOverzicht.Domain.Services;

/// <summary>
/// Default implementation of <see cref="IBalansOpsteller"/>.
/// </summary>
/// <remarks>
/// Accounts are valued at their cumulative signed balance up to the reporting date. Assets and
/// expenses have a normal debit balance; liabilities, equity and revenue have a normal credit
/// balance and are therefore sign-flipped for display. Accounts with a zero balance are omitted.
/// </remarks>
public sealed class BalansOpsteller : IBalansOpsteller
{
    /// <inheritdoc />
    public Balans StelOp(Grootboek grootboek, DateOnly perDatum)
    {
        ArgumentNullException.ThrowIfNull(grootboek);

        var activa = new List<SaldoRegel>();
        var passiva = new List<SaldoRegel>();
        var eigenVermogen = new List<SaldoRegel>();
        var resultaat = 0m;

        foreach (var rekening in grootboek.Rekeningen.OrderBy(r => r.Nummer, StringComparer.Ordinal))
        {
            var saldo = grootboek.Saldo(rekening.Id, perDatum);
            if (saldo == 0m)
            {
                continue;
            }

            switch (rekening.Soort)
            {
                case RekeningSoort.Activa:
                    activa.Add(new SaldoRegel(rekening.Nummer, rekening.Naam, saldo)
                    {
                        Vlottend = rekening.IsVlottend,
                        Voorraad = rekening.IsVoorraad,
                        LiquideMiddel = rekening.IsLiquideMiddel,
                    });
                    break;

                case RekeningSoort.Passiva:
                    passiva.Add(new SaldoRegel(rekening.Nummer, rekening.Naam, -saldo)
                    {
                        Vlottend = rekening.IsVlottend,
                    });
                    break;

                case RekeningSoort.EigenVermogen:
                    eigenVermogen.Add(new SaldoRegel(rekening.Nummer, rekening.Naam, -saldo));
                    break;

                case RekeningSoort.Opbrengsten:
                case RekeningSoort.Kosten:
                    // Revenue has a credit balance, expenses a debit balance; negating keeps
                    // "revenue minus expenses" correct for both.
                    resultaat -= saldo;
                    break;

                default:
                    break;
            }
        }

        return new Balans(
            activa,
            passiva,
            eigenVermogen,
            activa.Sum(r => r.Bedrag),
            passiva.Sum(r => r.Bedrag),
            eigenVermogen.Sum(r => r.Bedrag),
            resultaat,
            perDatum);
    }
}
