using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Rapporteren;

namespace FinOverzicht.Domain.Services;

/// <summary>
/// Builds a <see cref="WinstVerlies"/> for a given period from a <see cref="Grootboek"/>.
/// </summary>
public interface IWinstVerliesOpsteller
{
    /// <summary>
    /// Builds the profit and loss statement for <paramref name="periode"/>.
    /// </summary>
    WinstVerlies StelOp(Grootboek grootboek, Periode periode);
}
