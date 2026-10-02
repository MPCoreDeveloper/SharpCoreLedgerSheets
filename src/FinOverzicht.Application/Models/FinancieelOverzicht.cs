using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Rapporteren;

namespace FinOverzicht.Application.Models;

/// <summary>
/// The complete financial picture for a period: the three core statements plus the key ratios.
/// </summary>
/// <remarks>
/// This is the primary read model of the application layer; both the web UI and the report
/// engine consume it.
/// </remarks>
public sealed record FinancieelOverzicht(
    Balans Balans,
    WinstVerlies WinstVerlies,
    KasstroomOverzicht Kasstroom,
    RatioOverzicht Ratio,
    Periode Periode)
{
    /// <summary>True when all three statements are internally consistent.</summary>
    public bool IsConsistent => Balans.IsInBalans && Kasstroom.IsInBalans;
}
