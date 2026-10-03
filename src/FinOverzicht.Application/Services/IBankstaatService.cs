using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Services;

/// <summary>
/// Use cases for the bank import: reading a statement, inspecting what is still open, and turning a
/// movement into a journal entry (or dismissing it).
/// </summary>
public interface IBankstaatService
{
    /// <summary>
    /// Imports a statement, skipping every movement that was imported before.
    /// </summary>
    Task<BankstaatImportResultaat> ImporteerAsync(
        BankstaatImportCommand command,
        CancellationToken ct = default);

    /// <summary>Returns the imported movements ordered by date, optionally restricted to one status.</summary>
    Task<IReadOnlyList<BankTransactie>> AlleAsync(
        BankTransactieStatus? status = null,
        CancellationToken ct = default);

    /// <summary>
    /// Proposes a counterparty account for one movement, based on earlier confirmed bookings, or
    /// <see langword="null"/> when nothing in the ledger points at a plausible account.
    /// </summary>
    Task<Boekingsvoorstel?> VoorstelAsync(Guid transactieId, CancellationToken ct = default);

    /// <summary>
    /// Proposes a counterparty account for a whole list of movements in one pass: the ledger and the
    /// learned rules are read once, however many movements are passed in.
    /// </summary>
    /// <remarks>
    /// The import page uses this overload; a statement of a few hundred movements would otherwise
    /// query the same two tables once per movement. A movement the assistant has no signal for is
    /// simply left out — the caller shows it as 'no proposal'.
    /// </remarks>
    /// <returns>
    /// The proposals that could be made, in the order of <paramref name="transacties"/>.
    /// </returns>
    Task<IReadOnlyList<Boekingsvoorstel>> VoorstellenAsync(
        IReadOnlyList<BankTransactie> transacties,
        CancellationToken ct = default);

    /// <summary>
    /// Books a movement against the given counterparty account, marks the movement as booked, and
    /// remembers the match as a rule for the next import.
    /// </summary>
    /// <param name="transactieId">The imported movement to book.</param>
    /// <param name="tegenrekeningNummer">The ledger number of the counterparty account.</param>
    /// <param name="omschrijving">Overrides the description of the journal entry.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Journaalpost> BoekAsync(
        Guid transactieId,
        string tegenrekeningNummer,
        string? omschrijving = null,
        CancellationToken ct = default);

    /// <summary>Marks a movement as deliberately ignored: no journal entry will be created for it.</summary>
    Task NegeerAsync(Guid transactieId, CancellationToken ct = default);
}
