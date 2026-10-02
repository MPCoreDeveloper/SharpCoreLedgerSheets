using FinOverzicht.Application.Models;

namespace FinOverzicht.Reports.Models;

/// <summary>
/// View model for every FinOverzicht report: it wraps the application read model so a single
/// model serves the balance sheet, the profit and loss statement, the cash flow statement and
/// the dashboard template.
/// </summary>
public sealed record FinancieelOverzichtModel(FinancieelOverzicht Overzicht) : ReportBaseViewModel;
