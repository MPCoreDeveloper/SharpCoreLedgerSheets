using FinOverzicht.Application.Abstractions;
using FinOverzicht.Infrastructure.Bankzaken;
using FinOverzicht.Infrastructure.Options;
using FinOverzicht.Infrastructure.Opslag;
using Microsoft.Extensions.DependencyInjection;
using SharpCoreDB;
using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Extensions;

/// <summary>
/// Registers the SharpCoreDB storage adapters for FinOverzicht.
/// </summary>
public static class FinOverzichtInfrastructureExtensions
{
    /// <summary>
    /// Adds the encrypted embedded SharpCoreDB storage and registers the repository ports.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the database location and master password.</param>
    public static IServiceCollection AddFinOverzichtOpslag(
        this IServiceCollection services,
        Action<FinOverzichtOpslagOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<FinOverzichtOpslagOptions>().Configure(configure);

        // SharpCoreDB owns the encryption/crypto services and the DatabaseFactory.
        services.AddSharpCoreDB();

        services.AddSingleton<ISharpCoreDbFactory, SharpCoreDbFactory>();

        // One long-lived, encrypted database handle for the whole application lifetime.
        // The container disposes it (IDatabase is IAsyncDisposable) on shutdown.
        services.AddSingleton(provider => provider.GetRequiredService<ISharpCoreDbFactory>().Open());

        services.AddSingleton<IRekeningRepository, SharpCoreDbRekeningRepository>();
        services.AddSingleton<IJournaalpostRepository, SharpCoreDbJournaalpostRepository>();
        services.AddSingleton<IBankstaatRepository, SharpCoreDbBankstaatRepository>();
        services.AddSingleton<ITegenrekeningRegelRepository, SharpCoreDbTegenrekeningRegelRepository>();

        // The statement readers: the bank import picks the first one that recognises the file, so the
        // XML reader is asked before the CSV reader (a CAMT file is never valid CSV).
        services.AddSingleton<IBankstaatImporteur, Camt053Importeur>();
        services.AddSingleton<IBankstaatImporteur, CsvImporteur>();

        return services;
    }
}
