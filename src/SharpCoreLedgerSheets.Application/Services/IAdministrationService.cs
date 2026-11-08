using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Administration;

namespace SharpCoreLedgerSheets.Application.Services;

/// <summary>
/// Use cases for the administration itself: its settings and its opening balances.
/// </summary>
public interface IAdministrationService
{
    /// <summary>Returns the settings of the administration (defaults when nothing is stored yet).</summary>
    Task<AdministrationSettings> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>Stores the settings of the administration.</summary>
    Task SaveSettingsAsync(AdministrationSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Returns the opening balances of a book year, as entered by the user.
    /// </summary>
    /// <remarks>
    /// Opening balances are kept as one reserved journal entry dated the day before the book year starts,
    /// so they are part of the ledger like any other booking: auditable, included in the statements and
    /// impossible to diverge from the accounting rules.
    /// </remarks>
    Task<IReadOnlyList<OpeningBalanceLine>> OpeningBalancesAsync(int year, CancellationToken ct = default);

    /// <summary>
    /// Replaces the opening balances of a book year with the given lines.
    /// </summary>
    /// <exception cref="Domain.Common.DomainException">
    /// Thrown when the lines do not balance, when an amount is negative, or when a line holds both a debit
    /// and a credit.
    /// </exception>
    Task ReplaceOpeningBalancesAsync(int year, IReadOnlyList<OpeningBalanceLine> lines, CancellationToken ct = default);
}
