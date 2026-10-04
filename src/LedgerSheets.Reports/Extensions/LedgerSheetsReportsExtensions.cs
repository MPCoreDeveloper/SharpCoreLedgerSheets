using System.Diagnostics;
using LedgerSheets.Reports.Abstractions;
using LedgerSheets.Reports.Reporting;
using LedgerSheets.Reports.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LedgerSheets.Reports.Extensions;

/// <summary>
/// Registers the LedgerSheets report engine.
/// </summary>
public static class LedgerSheetsReportsExtensions
{
    /// <summary>
    /// Adds the Razor-backed report engine, the built-in report registry and the CSV exporter.
    /// </summary>
    /// <remarks>
    /// <see cref="IReportLabels"/> is registered with <c>TryAdd</c>, so a host that already
    /// registered a localized implementation (the web application does) keeps it.
    /// </remarks>
    public static IServiceCollection AddLedgerSheetsReports(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Ensures IRazorViewEngine, ITempDataProvider and friends are registered. AddControllerWithViews
        // is idempotent, and AddApplicationPart registers this assembly so the view engine can find
        // its compiled .cshtml templates.
        services.AddControllersWithViews()
                .AddApplicationPart(typeof(HtmlReportRenderer).Assembly)
                .AddRazorOptions(options => options.ViewLocationExpanders.Add(new ReportViewLocationExpander()));

        // The MVC Razor engine and page activator resolve DiagnosticSource/DiagnosticListener.
        // A full ASP.NET Core host registers them already; registering them here keeps the engine
        // usable from a bare service provider (unit tests, console tools).
        services.TryAddSingleton<DiagnosticListener>(_ => new DiagnosticListener("LedgerSheets.Reports"));
        services.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());

        services.AddMemoryCache();
        services.TryAddSingleton<IReportLabels, EnglishReportLabels>();
        services.AddSingleton(ReportRegistry.WithDefaultReports());
        services.AddScoped<RazorViewToStringRenderer>();
        services.AddScoped<IReportRenderer, HtmlReportRenderer>();
        services.AddScoped<IReportExporter, CsvReportExporter>();

        return services;
    }
}
