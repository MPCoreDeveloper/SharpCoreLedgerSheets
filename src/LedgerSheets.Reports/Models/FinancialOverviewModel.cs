using LedgerSheets.Application.Models;

namespace LedgerSheets.Reports.Models;

/// <summary>
/// View model for every LedgerSheets report: it wraps the application read model so a single
/// model serves the balance sheet, the profit and loss statement, the cash flow statement and
/// the dashboard template.
/// </summary>
public sealed record FinancialOverviewModel(FinancialOverview Overview) : ReportBaseViewModel;
