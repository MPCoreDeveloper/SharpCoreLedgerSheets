namespace LedgerSheets.Domain.Banking;

/// <summary>
/// The bank statement file formats LedgerSheets can read.
/// </summary>
public enum BankStatementFormat
{
    /// <summary>ISO 20022 CAMT.053: the XML bank-to-customer statement most banks offer.</summary>
    Camt053 = 0,

    /// <summary>A delimited text export (CSV) with one transaction per line.</summary>
    Csv = 1,
}
