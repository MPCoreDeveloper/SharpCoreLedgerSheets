using SharpCoreLedgerSheets.Domain.Administration;

namespace SharpCoreLedgerSheets.Application.Abstractions;

/// <summary>
/// Read/write port for the settings of the administration.
/// </summary>
/// <remarks>
/// Settings are small and change rarely, but they decide how every statement is read: the book year, the
/// currency and the VAT rates. The port returns the defaults of a fresh administration when nothing has
/// been stored yet, so a caller never has to handle "no settings" separately.
/// </remarks>
public interface ISettingsRepository
{
    /// <summary>Returns the stored settings, or the defaults when the administration has none yet.</summary>
    Task<AdministrationSettings> GetAsync(CancellationToken ct = default);

    /// <summary>Stores the settings, replacing what was there.</summary>
    Task SaveAsync(AdministrationSettings settings, CancellationToken ct = default);
}
