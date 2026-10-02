using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Rapporteren;

namespace FinOverzicht.Domain.Services;

/// <summary>
/// Builds a <see cref="KasstroomOverzicht"/> for a given period from a <see cref="Grootboek"/>.
/// </summary>
public interface IKasstroomOpsteller
{
    /// <summary>
    /// Builds the cash flow statement for <paramref name="periode"/>.
    /// </summary>
    KasstroomOverzicht StelOp(Grootboek grootboek, Periode periode);
}
