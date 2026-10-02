using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Domain.Rapporteren;

/// <summary>
/// The profit and loss statement ("winst- en verliesrekening") for a period.
/// </summary>
public sealed record WinstVerlies(
    IReadOnlyList<SaldoRegel> Opbrengsten,
    IReadOnlyList<SaldoRegel> Kosten,
    decimal TotaalOpbrengsten,
    decimal TotaalKosten,
    Periode Periode)
{
    /// <summary>The result for the period (revenue minus expenses).</summary>
    public decimal Resultaat => TotaalOpbrengsten - TotaalKosten;

    /// <summary>True when the period ended with a profit.</summary>
    public bool IsWinst => Resultaat > 0m;

    /// <summary>True when the period ended with a loss.</summary>
    public bool IsVerlies => Resultaat < 0m;

    /// <summary>The result as a fraction of revenue (0.25 = 25%), or <see langword="null"/> when there is no revenue.</summary>
    public decimal? NettoMarge => TotaalOpbrengsten == 0m ? null : Resultaat / TotaalOpbrengsten;
}
