using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Administration;

namespace SharpCoreLedgerSheets.Application.Tests.Fakes;

/// <summary>In-memory <see cref="ISettingsRepository"/> used by the application tests.</summary>
internal sealed class InMemorySettingsRepository : ISettingsRepository
{
    private AdministrationSettings _settings = AdministrationSettings.Default;

    /// <summary>The settings currently stored.</summary>
    public AdministrationSettings Current => _settings;

    /// <summary>How often the settings were read.</summary>
    public int CountTimesRead { get; private set; }

    public Task<AdministrationSettings> GetAsync(CancellationToken ct = default)
    {
        CountTimesRead++;
        return Task.FromResult(_settings);
    }

    public Task SaveAsync(AdministrationSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
        return Task.CompletedTask;
    }

    /// <summary>Replaces the stored settings without going through the port (test setup).</summary>
    public void Set(AdministrationSettings settings) => _settings = settings;
}
