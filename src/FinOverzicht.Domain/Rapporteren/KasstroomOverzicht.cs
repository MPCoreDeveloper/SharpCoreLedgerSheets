using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Domain.Rapporteren;

/// <summary>
/// The cash flow statement ("kasstroomoverzicht") for a period, split into operating,
/// investing and financing activities.
/// </summary>
/// <remarks>
/// All amounts are expressed as cash movements: a positive amount increases the cash position.
/// <see cref="IsInBalans"/> verifies that the opening balance plus the net movement equals the
/// closing balance.
/// </remarks>
public sealed record KasstroomOverzicht(
    IReadOnlyList<SaldoRegel> Operationeel,
    IReadOnlyList<SaldoRegel> Investering,
    IReadOnlyList<SaldoRegel> Financiering,
    decimal BeginSaldo,
    decimal EindSaldo,
    Periode Periode)
{
    /// <summary>Net cash from operating activities.</summary>
    public decimal NettoOperationeel => Som(Operationeel);

    /// <summary>Net cash from investing activities.</summary>
    public decimal NettoInvestering => Som(Investering);

    /// <summary>Net cash from financing activities.</summary>
    public decimal NettoFinanciering => Som(Financiering);

    /// <summary>Net change in cash for the period.</summary>
    public decimal NettoMutatie => NettoOperationeel + NettoInvestering + NettoFinanciering;

    /// <summary>The closing balance implied by the opening balance and the net movement.</summary>
    public decimal VerwachteEindSaldo => BeginSaldo + NettoMutatie;

    /// <summary>True when the statement reconciles (opening + movement == closing).</summary>
    public bool IsInBalans => decimal.Abs(EindSaldo - VerwachteEindSaldo) < 0.005m;

    private static decimal Som(IReadOnlyList<SaldoRegel> regels) => regels.Sum(r => r.Bedrag);
}
