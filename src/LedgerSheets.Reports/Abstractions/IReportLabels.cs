using LedgerSheets.Reports.Models;

namespace LedgerSheets.Reports.Abstractions;

/// <summary>
/// All user-visible text used by the report engine.
/// </summary>
/// <remarks>
/// Reports never hard-code labels. The web application supplies an implementation backed by
/// <c>IStringLocalizer</c> so the same template renders in every supported language; the
/// fallback <see cref="Reporting.EnglishReportLabels"/> keeps the engine usable standalone.
/// </remarks>
public interface IReportLabels
{
    /// <summary>Label for the balance sheet.</summary>
    string BalanceSheet { get; }

    /// <summary>Label for the profit and loss statement.</summary>
    string ProfitAndLoss { get; }

    /// <summary>Label for the cash flow statement.</summary>
    string CashFlowStatement { get; }

    /// <summary>Label for the ratios section.</summary>
    string Ratio { get; }

    /// <summary>Assets.</summary>
    string Assets { get; }

    /// <summary>Liabilities.</summary>
    string Liabilities { get; }

    /// <summary>Equity.</summary>
    string Equity { get; }

    /// <summary>Result for the period.</summary>
    string Result { get; }

    /// <summary>Revenue.</summary>
    string Revenue { get; }

    /// <summary>Expenses.</summary>
    string Costs { get; }

    /// <summary>Total.</summary>
    string Total { get; }

    /// <summary>Operating activities.</summary>
    string Operating { get; }

    /// <summary>Investing activities.</summary>
    string Investing { get; }

    /// <summary>Financing activities.</summary>
    string Financing { get; }

    /// <summary>Opening cash balance.</summary>
    string OpeningBalance { get; }

    /// <summary>Closing cash balance.</summary>
    string ClosingBalance { get; }

    /// <summary>Net cash movement.</summary>
    string NetMovement { get; }

    /// <summary>Current ratio.</summary>
    string CurrentRatio { get; }

    /// <summary>Quick ratio.</summary>
    string QuickRatio { get; }

    /// <summary>Solvency.</summary>
    string Solvency { get; }

    /// <summary>Net profit margin.</summary>
    string NetProfitMargin { get; }

    /// <summary>Return on equity.</summary>
    string ReturnOnEquity { get; }

    /// <summary>Column header: account.</summary>
    string Account { get; }

    /// <summary>Column header: description.</summary>
    string Description { get; }

    /// <summary>Column header: amount.</summary>
    string Amount { get; }

    /// <summary>Text shown when a statement has no data.</summary>
    string NoData { get; }

    /// <summary>Label prefix for the generation timestamp.</summary>
    string GeneratedOn { get; }

    /// <summary>Label for the reporting period.</summary>
    string Period { get; }

    /// <summary>Message shown when the ledger balances.</summary>
    string Balanced { get; }

    /// <summary>Message shown when the ledger does not balance.</summary>
    string NotBalanced { get; }

    /// <summary>Title of the combined dashboard report.</summary>
    string OverviewTitle { get; }

    /// <summary>Description of the combined dashboard report.</summary>
    string OverviewDescription { get; }

    /// <summary>Resolves a label by key (used by generic exporters).</summary>
    /// <param name="key">The label key, matching one of the property names.</param>
    string this[string key] { get; }
}
