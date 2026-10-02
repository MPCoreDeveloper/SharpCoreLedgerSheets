using FinOverzicht.Domain.Rapporteren;

namespace FinOverzicht.Domain.Services;

/// <summary>
/// Derives the key <see cref="RatioOverzicht"/> from a balance sheet and a profit and loss statement.
/// </summary>
public interface IRatioBerekening
{
    /// <summary>
    /// Calculates the ratios. <paramref name="balans"/> should be taken at the end of the period
    /// of <paramref name="winstVerlies"/>.
    /// </summary>
    RatioOverzicht Bereken(Balans balans, WinstVerlies winstVerlies);
}
