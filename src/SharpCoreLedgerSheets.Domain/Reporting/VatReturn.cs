using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Domain.Reporting;

/// <summary>
/// The VAT position of one rate for a period: what is owed on sales and what is reclaimable on purchases.
/// </summary>
/// <param name="RateCode">The code of the VAT rate (for a Dutch administration: <c>21</c>, <c>9</c> or <c>0</c>).</param>
/// <param name="Description">The description of the rate, as shown on the return.</param>
/// <param name="OutputVat">VAT owed on sales (output VAT) in the period.</param>
/// <param name="InputVat">VAT reclaimable on purchases (input VAT) in the period.</param>
public sealed record VatReturnLine(string RateCode, string Description, decimal OutputVat, decimal InputVat)
{
    /// <summary>Output minus input VAT: positive means this rate adds to what has to be paid.</summary>
    public decimal Balance => OutputVat - InputVat;
}

/// <summary>
/// The VAT return of a period: per rate the output and input VAT, plus what has to be paid or is reclaimable.
/// </summary>
/// <param name="Period">The period the return covers (a quarter, for example).</param>
/// <param name="Lines">One line per VAT rate of the administration.</param>
public sealed record VatReturn(Period Period, IReadOnlyList<VatReturnLine> Lines)
{
    /// <summary>Total VAT owed on sales in the period.</summary>
    public decimal TotalOutputVat => Lines.Sum(line => line.OutputVat);

    /// <summary>Total VAT reclaimable on purchases in the period.</summary>
    public decimal TotalInputVat => Lines.Sum(line => line.InputVat);

    /// <summary>
    /// Output minus input VAT. A positive balance is payable to the tax office, a negative balance can be
    /// reclaimed.
    /// </summary>
    public decimal Balance => TotalOutputVat - TotalInputVat;

    /// <summary>True when the period ends with VAT to pay (or nothing at all).</summary>
    public bool IsPayable => Balance >= 0m;

    /// <summary>The amount payable, or zero when the period is reclaimable.</summary>
    public decimal Payable => Balance > 0m ? Balance : 0m;

    /// <summary>The amount reclaimable, or zero when the period is payable.</summary>
    public decimal Reclaimable => Balance < 0m ? -Balance : 0m;
}
