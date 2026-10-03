using FinOverzicht.Application.Services;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinOverzicht.Application.Extensions;

/// <summary>
/// Registers the FinOverzicht application layer (domain services plus use cases).
/// </summary>
public static class FinOverzichtApplicationExtensions
{
    /// <summary>
    /// Adds the domain statement builders and the application use-case services.
    /// The storage ports (<c>IRekeningRepository</c>, <c>IJournaalpostRepository</c>) must be
    /// registered separately by the infrastructure layer.
    /// </summary>
    public static IServiceCollection AddFinOverzichtApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Domain services: stateless, pure calculations.
        services.AddSingleton<IBalansOpsteller, BalansOpsteller>();
        services.AddSingleton<IWinstVerliesOpsteller, WinstVerliesOpsteller>();
        services.AddSingleton<IKasstroomOpsteller, KasstroomOpsteller>();
        services.AddSingleton<IRatioBerekening, RatioBerekening>();
        services.AddSingleton<IMatchingAssistent, MatchingAssistent>();

        // Application use cases.
        services.AddScoped<IGrootboekLader, GrootboekLader>();
        services.AddScoped<IRekeningService, RekeningService>();
        services.AddScoped<IJournaalpostService, JournaalpostService>();
        services.AddScoped<IRapportageService, RapportageService>();
        services.AddScoped<IBankstaatService, BankstaatService>();

        return services;
    }
}
