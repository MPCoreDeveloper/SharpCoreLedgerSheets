using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SharpCoreLedgerSheets.Application.Extensions;

/// <summary>
/// Registers the SharpCoreLedgerSheets application layer (domain services plus use cases).
/// </summary>
public static class SharpCoreLedgerSheetsApplicationExtensions
{
    /// <summary>
    /// Adds the domain statement builders and the application use-case services.
    /// The storage ports (<c>IAccountRepository</c>, <c>IJournalEntryRepository</c>) must be
    /// registered separately by the infrastructure layer.
    /// </summary>
    public static IServiceCollection AddSharpCoreLedgerSheetsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Domain services: stateless, pure calculations.
        services.AddSingleton<IBalanceSheetBuilder, BalanceSheetBuilder>();
        services.AddSingleton<IProfitAndLossBuilder, ProfitAndLossBuilder>();
        services.AddSingleton<ICashFlowBuilder, CashFlowBuilder>();
        services.AddSingleton<IRatioCalculator, RatioCalculator>();
        services.AddSingleton<IMatchingAssistant, MatchingAssistant>();
        services.AddSingleton<IVatReturnBuilder, VatReturnBuilder>();

        // Application use cases.
        services.AddScoped<ILedgerLoader, LedgerLoader>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IBankStatementService, BankStatementService>();
        services.AddScoped<IAdministrationService, AdministrationService>();
        services.AddScoped<IVatService, VatService>();

        return services;
    }
}
