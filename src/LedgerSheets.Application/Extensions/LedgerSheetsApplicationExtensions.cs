using LedgerSheets.Application.Services;
using LedgerSheets.Domain.Banking;
using LedgerSheets.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerSheets.Application.Extensions;

/// <summary>
/// Registers the LedgerSheets application layer (domain services plus use cases).
/// </summary>
public static class LedgerSheetsApplicationExtensions
{
    /// <summary>
    /// Adds the domain statement builders and the application use-case services.
    /// The storage ports (<c>IAccountRepository</c>, <c>IJournalEntryRepository</c>) must be
    /// registered separately by the infrastructure layer.
    /// </summary>
    public static IServiceCollection AddLedgerSheetsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Domain services: stateless, pure calculations.
        services.AddSingleton<IBalanceSheetBuilder, BalanceSheetBuilder>();
        services.AddSingleton<IProfitAndLossBuilder, ProfitAndLossBuilder>();
        services.AddSingleton<ICashFlowBuilder, CashFlowBuilder>();
        services.AddSingleton<IRatioCalculator, RatioCalculator>();
        services.AddSingleton<IMatchingAssistant, MatchingAssistant>();

        // Application use cases.
        services.AddScoped<ILedgerLoader, LedgerLoader>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IBankStatementService, BankStatementService>();

        return services;
    }
}
