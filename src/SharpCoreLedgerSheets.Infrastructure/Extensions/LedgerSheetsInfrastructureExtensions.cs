using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Infrastructure.Banking;
using SharpCoreLedgerSheets.Infrastructure.Options;
using SharpCoreLedgerSheets.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB;
using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Extensions;

/// <summary>
/// Registers the SharpCoreDB storage adapters for SharpCoreLedgerSheets.
/// </summary>
public static class SharpCoreLedgerSheetsInfrastructureExtensions
{
    /// <summary>
    /// Adds the encrypted embedded SharpCoreDB storage and registers the repository ports.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the database location and master password.</param>
    public static IServiceCollection AddSharpCoreLedgerSheetsStorage(
        this IServiceCollection services,
        Action<SharpCoreLedgerSheetsStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<SharpCoreLedgerSheetsStorageOptions>().Configure(configure);

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
