using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Application.Services;

/// <summary>
/// Loads the complete ledger (accounts plus all entries) from storage.
/// </summary>
/// <remarks>
/// Reporting needs the full history (opening balances of cash accounts and cumulative equity),
/// so the loader never filters by period; the domain statements filter themselves.
/// </remarks>
public interface ILedgerLoader
{
    /// <summary>Loads the complete ledger.</summary>
    Task<Ledger> LoadAsync(CancellationToken ct = default);
}
