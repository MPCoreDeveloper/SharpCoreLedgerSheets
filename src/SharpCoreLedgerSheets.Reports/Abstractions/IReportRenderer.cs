using SharpCoreLedgerSheets.Reports.Models;

namespace SharpCoreLedgerSheets.Reports.Abstractions;

/// <summary>
/// Renders a report template to HTML.
/// </summary>
public interface IReportRenderer
{
    /// <summary>Renders the named template with the given model to a complete HTML document.</summary>
    /// <typeparam name="TModel">The report model type.</typeparam>
    /// <param name="templateName">The template name, for example <c>BalanceSheetReport</c>.</param>
    /// <param name="model">The report model.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> RenderHtmlAsync<TModel>(string templateName, TModel model, CancellationToken ct = default)
        where TModel : ReportBaseViewModel;
}
