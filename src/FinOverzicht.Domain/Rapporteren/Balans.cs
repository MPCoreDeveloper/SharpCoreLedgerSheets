namespace FinOverzicht.Domain.Rapporteren;

/// <summary>
/// The balance sheet ("balans") at a specific date: assets versus liabilities plus equity.
/// </summary>
/// <remarks>
/// The current-period result is kept separate from the equity accounts so the statement shows
/// both the capital accounts and the profit or loss for the period under review.
/// The accounting equation is: <c>TotaalActiva == TotaalPassiva + TotaalEigenVermogen + Resultaat</c>.
/// </remarks>
public sealed record Balans(
    IReadOnlyList<SaldoRegel> Activa,
    IReadOnlyList<SaldoRegel> Passiva,
    IReadOnlyList<SaldoRegel> EigenVermogen,
    decimal TotaalActiva,
    decimal TotaalPassiva,
    decimal TotaalEigenVermogen,
    decimal Resultaat,
    DateOnly PerDatum)
{
    /// <summary>Equity including the accumulated result.</summary>
    public decimal EigenVermogenInclusiefResultaat => TotaalEigenVermogen + Resultaat;

    /// <summary>The total of the credit side (liabilities + equity + result).</summary>
    public decimal TotaalCreditZijde => TotaalPassiva + TotaalEigenVermogen + Resultaat;

    /// <summary>True when the balance sheet balances.</summary>
    public bool IsInBalans => TotaalActiva == TotaalCreditZijde;

    /// <summary>The difference between both sides (0 for a balanced sheet).</summary>
    public decimal Verschil => TotaalActiva - TotaalCreditZijde;
}
