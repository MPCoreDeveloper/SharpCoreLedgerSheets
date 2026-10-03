namespace FinOverzicht.Domain.Bankzaken;

/// <summary>
/// The bank statement file formats FinOverzicht can read.
/// </summary>
public enum BankstaatFormaat
{
    /// <summary>ISO 20022 CAMT.053: the XML bank-to-customer statement most banks offer.</summary>
    Camt053 = 0,

    /// <summary>A delimited text export (CSV) with one transaction per line.</summary>
    Csv = 1,
}
