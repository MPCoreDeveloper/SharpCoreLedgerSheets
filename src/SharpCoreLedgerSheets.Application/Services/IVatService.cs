using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Application.Services;

/// <summary>
/// Use cases for booking amounts with VAT.
/// </summary>
/// <remarks>
/// The result is an ordinary balanced journal entry - the net amount on the revenue or cost account, the VAT
/// on the rate's VAT account and the gross amount on the counterparty - so nothing about VAT is hidden from
/// the ledger, the statements or the audit trail.
/// </remarks>
public interface IVatService
{
    /// <summary>Books a sale: the customer owes the gross amount, the revenue is net and the VAT is owed.</summary>
    Task<JournalEntry> PostSaleAsync(VatInvoiceCommand command, CancellationToken ct = default);

    /// <summary>
    /// Books a purchase: the cost is net, the VAT is reclaimable and the supplier is owed the gross amount.
    /// </summary>
    Task<JournalEntry> PostPurchaseAsync(VatInvoiceCommand command, CancellationToken ct = default);
}
