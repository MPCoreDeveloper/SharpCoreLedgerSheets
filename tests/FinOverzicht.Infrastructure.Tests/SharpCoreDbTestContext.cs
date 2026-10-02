using FinOverzicht.Application.Abstractions;
using FinOverzicht.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Tests;

/// <summary>
/// Spins up a throwaway, encrypted SharpCoreDB database in the temp folder for a single test.
/// </summary>
internal sealed class SharpCoreDbTestContext : IAsyncDisposable
{
    private const string Wachtwoord = "FinOverzicht-Test-2026!";

    private readonly string _map;
    private ServiceProvider _provider;

    public SharpCoreDbTestContext(bool hergebruikBestaandeDatabase = false)
    {
        _map = Path.Combine(Path.GetTempPath(), "finoverzicht-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_map);
        _provider = BouwProvider();
        Hergebruik = hergebruikBestaandeDatabase;
    }

    /// <summary>When true the database is reopened (not recreated) by <see cref="Heropen"/>.</summary>
    public bool Hergebruik { get; }

    /// <summary>The database file path.</summary>
    public string DatabasePad => Path.Combine(_map, "finoverzicht.scdb");

    public IRekeningRepository Rekeningen => _provider.GetRequiredService<IRekeningRepository>();

    public IJournaalpostRepository Posten => _provider.GetRequiredService<IJournaalpostRepository>();

    public IDatabase Database => _provider.GetRequiredService<IDatabase>();

    /// <summary>Closes and reopens the database on the same file to prove durability.</summary>
    public async Task HeropenAsync()
    {
        await _provider.DisposeAsync();
        _provider = BouwProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();

        try
        {
            Directory.Delete(_map, recursive: true);
        }
        catch (IOException ex)
        {
            // Best effort: the engine may still hold the file handle briefly.
            _ = ex.Message;
        }
        catch (UnauthorizedAccessException ex)
        {
            // Best effort: the OS cleans up the temp folder eventually.
            _ = ex.Message;
        }
    }

    private ServiceProvider BouwProvider()
    {
        var services = new ServiceCollection();

        services.AddFinOverzichtOpslag(options =>
        {
            options.DatabasePad = DatabasePad;
            options.MasterWachtwoord = Wachtwoord;
        });

        return services.BuildServiceProvider();
    }
}
