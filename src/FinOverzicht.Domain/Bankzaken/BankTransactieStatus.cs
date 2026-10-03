namespace FinOverzicht.Domain.Bankzaken;

/// <summary>
/// What has happened with an imported bank transaction.
/// </summary>
/// <remarks>
/// An imported transaction is never booked automatically: it waits in
/// <see cref="Open"/> until the user either confirms a booking (<see cref="Geboekt"/>) or
/// decides it needs no entry at all (<see cref="Genegeerd"/>).
/// </remarks>
public enum BankTransactieStatus
{
    /// <summary>Imported, not yet booked: the user still has to decide.</summary>
    Open = 0,

    /// <summary>Booked: a journal entry was created from this movement.</summary>
    Geboekt = 1,

    /// <summary>Deliberately ignored: no journal entry will be created for it.</summary>
    Genegeerd = 2,
}
