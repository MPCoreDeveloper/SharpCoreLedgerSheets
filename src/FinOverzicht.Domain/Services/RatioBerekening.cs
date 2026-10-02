using FinOverzicht.Domain.Rapporteren;

namespace FinOverzicht.Domain.Services;

/// <summary>
/// Default implementation of <see cref="IRatioBerekening"/>.
/// </summary>
/// <remarks>
/// All proportions are returned as fractions (0.25 = 25%). A ratio is <see langword="null"/>
/// when its denominator is zero. Ratios are rounded to four decimals for stable comparisons.
/// </remarks>
public sealed class RatioBerekening : IRatioBerekening
{
    /// <inheritdoc />
    public RatioOverzicht Bereken(Balans balans, WinstVerlies winstVerlies)
    {
        ArgumentNullException.ThrowIfNull(balans);
        ArgumentNullException.ThrowIfNull(winstVerlies);

        var vlottendeActiva = balans.Activa.Where(r => r.Vlottend).Sum(r => r.Bedrag);
        var voorraad = balans.Activa.Where(r => r.Voorraad).Sum(r => r.Bedrag);
        var kortlopendePassiva = balans.Passiva.Where(r => r.Vlottend).Sum(r => r.Bedrag);

        var eigenVermogen = balans.EigenVermogenInclusiefResultaat;
        var totaalActiva = balans.TotaalActiva;
        var omzet = winstVerlies.TotaalOpbrengsten;
        var resultaat = winstVerlies.Resultaat;

        return new RatioOverzicht(
            CurrentRatio: Deel(vlottendeActiva, kortlopendePassiva),
            QuickRatio: Deel(vlottendeActiva - voorraad, kortlopendePassiva),
            Solvabiliteit: Deel(eigenVermogen, totaalActiva),
            NettoWinstmarge: Deel(resultaat, omzet),
            RentabiliteitEigenVermogen: Deel(resultaat, eigenVermogen));
    }

    private static decimal? Deel(decimal teller, decimal noemer) =>
        noemer == 0m ? null : decimal.Round(teller / noemer, 4, MidpointRounding.AwayFromZero);
}
