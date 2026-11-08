namespace SharpCoreLedgerSheets.Application.Models;

/// <summary>
/// Command to book an invoice amount with VAT: the net amount goes to a revenue or cost account, the VAT
/// goes to the VAT account of the chosen rate, and the counterparty carries the gross amount.
/// </summary>
/// <param name="Date">The booking date.</param>
/// <param name="Description">The description of the resulting journal entry.</param>
/// <param name="NetAccountNumber">
/// The revenue account (for a sale) or the cost account (for a purchase) that receives the net amount.
/// </param>
/// <param name="CounterAccountNumber">
/// The account that settles the invoice: the debtor, the creditor, or a bank/cash account when it is paid
/// immediately.
/// </param>
/// <param name="NetAmount">The amount excluding VAT.</param>
/// <param name="VatRateCode">The code of the VAT rate (for a Dutch administration: <c>21</c>, <c>9</c> or <c>0</c>).</param>
/// <param name="Reference">Optional external reference (invoice number, ...).</param>
public sealed record VatInvoiceCommand(
    DateOnly Date,
    string Description,
    string NetAccountNumber,
    string CounterAccountNumber,
    decimal NetAmount,
    string VatRateCode,
    string? Reference = null);
