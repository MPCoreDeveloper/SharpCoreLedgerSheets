using FinOverzicht.Application.Abstractions;
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

        return services;
    }
}
