using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Tests;

/// <summary>
/// Spins up a throwaway, encrypted SharpCoreDB database in the temp folder for a single test.
/// </summary>
internal sealed class SharpCoreDbTestContext : IAsyncDisposable
{
    // A throwaway database in the temp folder still needs a password, but it must not be a literal
    // in the repository: one is generated per test process and never reused anywhere else.
    private static readonly string Password = Guid.NewGuid().ToString("N");

    private readonly string _map;
    private ServiceProvider _provider;

    public SharpCoreDbTestContext(bool reuseExistingDatabase = false)
    {
        _map = Path.Combine(Path.GetTempPath(), "sharpcoreledgersheets-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_map);
        _provider = BuildProvider();
        Reuse = reuseExistingDatabase;
    }

    /// <summary>When true the database is reopened (not recreated) by <see cref="Heropen"/>.</summary>
    public bool Reuse { get; }

    /// <summary>The database file path.</summary>
    public string DatabasePath => Path.Combine(_map, "sharpcoreledgersheets.scdb");

    public IAccountRepository Accounts => _provider.GetRequiredService<IAccountRepository>();

    public IJournalEntryRepository Entries => _provider.GetRequiredService<IJournalEntryRepository>();

    public IBankStatementRepository BankStatement => _provider.GetRequiredService<IBankStatementRepository>();

    public ICounterAccountRuleRepository MatchRules => _provider.GetRequiredService<ICounterAccountRuleRepository>();

    /// <summary>The statement readers, in the order the application asks them.</summary>
    public IReadOnlyList<IBankStatementImporter> Importers => [.. _provider.GetServices<IBankStatementImporter>()];

    public IDatabase Database => _provider.GetRequiredService<IDatabase>();

    /// <summary>Closes and reopens the database on the same file to prove durability.</summary>
    public async Task HeropenAsync()
    {
        await _provider.DisposeAsync();
        _provider = BuildProvider();
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

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddSharpCoreLedgerSheetsStorage(options =>
        {
            options.DatabasePath = DatabasePath;
            options.MasterPassword = Password;
        });

        return services.BuildServiceProvider();
    }
}
