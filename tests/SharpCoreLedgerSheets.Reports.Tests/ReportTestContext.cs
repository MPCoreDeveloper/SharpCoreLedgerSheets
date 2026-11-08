using SharpCoreLedgerSheets.Reports.Abstractions;
using SharpCoreLedgerSheets.Reports.Extensions;
using SharpCoreLedgerSheets.Reports.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SharpCoreLedgerSheets.Reports.Tests;

/// <summary>
/// Hosts the report engine in a real service provider so the Razor rendering pipeline is exercised
/// end-to-end (view resolution, compiled templates, layouts, ViewData injection).
/// </summary>
internal sealed class ReportTestContext : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    internal ReportTestContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharpCoreLedgerSheetsReports();
        _provider = services.BuildServiceProvider();
    }

    internal IReportRenderer Renderer => _provider.GetRequiredService<IReportRenderer>();

    internal IReportExporter Exporter => _provider.GetRequiredService<IReportExporter>();

    internal ReportRegistry Registry => _provider.GetRequiredService<ReportRegistry>();

    internal IReportLabels Labels => _provider.GetRequiredService<IReportLabels>();

    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
