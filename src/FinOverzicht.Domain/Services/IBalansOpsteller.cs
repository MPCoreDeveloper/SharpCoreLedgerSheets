using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Rapporteren;

namespace FinOverzicht.Domain.Services;

/// <summary>
/// Builds a <see cref="Balans"/> at a given date from a <see cref="Grootboek"/>.
/// </summary>
public interface IBalansOpsteller
{
    /// <summary>
    /// Builds the balance sheet as of <paramref name="perDatum"/> (inclusive).
    /// </summary>
    Balans StelOp(Grootboek grootboek, DateOnly perDatum);
}
