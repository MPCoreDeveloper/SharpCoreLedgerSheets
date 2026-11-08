using SharpCoreLedgerSheets.Reports.Models;

namespace SharpCoreLedgerSheets.Reports.Services;

/// <summary>
/// Registry of the reports the engine can produce.
/// </summary>
/// <remarks>
/// Titles and descriptions are stored as <see cref="Abstractions.IReportLabels"/> keys rather than
/// literal text, so a single registration serves every culture.
/// </remarks>
public sealed class ReportRegistry
{
    private readonly List<ReportDefinition> _reports = [];

    /// <summary>Registers a report. The key must be unique.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the key is already registered.</exception>
    public void Register(
        string key,
        string titleKey,
        string descriptionKey,
        string templateName,
        ReportFormat[]? formaten = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        if (_reports.Any(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Er is al een report geregistreerd met sleutel '{key}'.");
        }

        _reports.Add(new ReportDefinition(
            key,
            titleKey,
            descriptionKey,
            templateName,
            formaten ?? [ReportFormat.Html, ReportFormat.Csv]));
    }

    /// <summary>Returns every registered report.</summary>
    public IReadOnlyList<ReportDefinition> All() => _reports.AsReadOnly();

    /// <summary>Finds a report by key (case-insensitive).</summary>
    public ReportDefinition? Find(string key) =>
        _reports.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Describes a registered report.</summary>
    /// <param name="Key">The unique key.</param>
    /// <param name="TitleKey">The label key that resolves the title.</param>
    /// <param name="DescriptionKey">The label key that resolves the description.</param>
    /// <param name="TemplateName">The Razor template name.</param>
    /// <param name="Formaten">The supported output formats.</param>
    public sealed record ReportDefinition(
        string Key,
        string TitleKey,
        string DescriptionKey,
        string TemplateName,
        ReportFormat[] Formaten);

    /// <summary>Creates the registry with SharpCoreLedgerSheets's four built-in reports.</summary>
    public static ReportRegistry WithDefaultReports()
    {
        var registry = new ReportRegistry();
        registry.Register(ReportKeys.Overview, nameof(Abstractions.IReportLabels.OverviewTitle), nameof(Abstractions.IReportLabels.OverviewDescription), "FinancialOverviewReport");
        registry.Register(ReportKeys.BalanceSheet, nameof(Abstractions.IReportLabels.BalanceSheet), nameof(Abstractions.IReportLabels.Assets), "BalanceSheetReport");
        registry.Register(ReportKeys.ProfitAndLoss, nameof(Abstractions.IReportLabels.ProfitAndLoss), nameof(Abstractions.IReportLabels.Revenue), "ProfitAndLossReport");
        registry.Register(ReportKeys.CashFlow, nameof(Abstractions.IReportLabels.CashFlowStatement), nameof(Abstractions.IReportLabels.Operating), "CashFlowReport");
        return registry;
    }
}
