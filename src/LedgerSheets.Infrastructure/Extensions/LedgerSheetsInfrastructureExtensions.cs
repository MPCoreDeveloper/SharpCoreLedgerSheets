using LedgerSheets.Application.Abstractions;
using LedgerSheets.Infrastructure.Banking;
using LedgerSheets.Infrastructure.Options;
using LedgerSheets.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB;
using SharpCoreDB.Interfaces;

namespace LedgerSheets.Infrastructure.Extensions;

/// <summary>
/// Registers the SharpCoreDB storage adapters for LedgerSheets.
/// </summary>
public static class LedgerSheetsInfrastructureExtensions
{
    /// <summary>
    /// Adds the encrypted embedded SharpCoreDB storage and registers the repository ports.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the database location and master password.</param>
    public static IServiceCollection AddLedgerSheetsStorage(
        this IServiceCollection services,
        Action<LedgerSheetsStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<LedgerSheetsStorageOptions>().Configure(configure);

        // SharpCoreDB owns the encryption/crypto services and the DatabaseFactory.
        services.AddSharpCoreDB();

        services.AddSingleton<ISharpCoreDbFactory, SharpCoreDbFactory>();

        // One long-lived, encrypted database handle for the whole application lifetime.
        // The container disposes it (IDatabase is IAsyncDisposable) on shutdown.
        services.AddSingleton(provider => provider.GetRequiredService<ISharpCoreDbFactory>().Open());

        services.AddSingleton<IAccountRepository, SharpCoreDbAccountRepository>();
        services.AddSingleton<IJournalEntryRepository, SharpCoreDbJournalEntryRepository>();
        services.AddSingleton<IBankStatementRepository, SharpCoreDbBankStatementRepository>();
        services.AddSingleton<ICounterAccountRuleRepository, SharpCoreDbCounterAccountRuleRepository>();

        // The statement readers: the bank import picks the first one that recognises the file, so the
        // XML reader is asked before the CSV reader (a CAMT file is never valid CSV).
        services.AddSingleton<IBankStatementImporter, Camt053Importer>();
        services.AddSingleton<IBankStatementImporter, CsvImporter>();

        return services;
    }
}
