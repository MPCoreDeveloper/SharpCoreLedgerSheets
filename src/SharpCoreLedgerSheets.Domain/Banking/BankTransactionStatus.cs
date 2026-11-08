namespace SharpCoreLedgerSheets.Domain.Banking;

/// <summary>
/// What has happened with an imported bank transaction.
/// </summary>
/// <remarks>
/// An imported transaction is never booked automatically: it waits in
/// <see cref="Open"/> until the user either confirms a booking (<see cref="Posted"/>) or
/// decides it needs no entry at all (<see cref="Ignored"/>).
/// </remarks>
public enum BankTransactionStatus
{
    /// <summary>Imported, not yet booked: the user still has to decide.</summary>
    Open = 0,

    /// <summary>Booked: a journal entry was created from this movement.</summary>
    Posted = 1,

    /// <summary>Deliberately ignored: no journal entry will be created for it.</summary>
    Ignored = 2,
}
