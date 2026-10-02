using FinOverzicht.Reports.Models;

namespace FinOverzicht.Reports.Abstractions;

/// <summary>
/// All user-visible text used by the report engine.
/// </summary>
/// <remarks>
/// Reports never hard-code labels. The web application supplies an implementation backed by
/// <c>IStringLocalizer</c> so the same template renders in every supported language; the
/// fallback <see cref="Reporting.NederlandseReportLabels"/> keeps the engine usable standalone.
/// </remarks>
public interface IReportLabels
{
    /// <summary>Label for the balance sheet.</summary>
    string Balans { get; }

    /// <summary>Label for the profit and loss statement.</summary>
    string WinstEnVerlies { get; }

    /// <summary>Label for the cash flow statement.</summary>
    string Kasstroomoverzicht { get; }

    /// <summary>Label for the ratios section.</summary>
    string Ratio { get; }

    /// <summary>Assets.</summary>
    string Activa { get; }

    /// <summary>Liabilities.</summary>
    string Passiva { get; }

    /// <summary>Equity.</summary>
    string EigenVermogen { get; }

    /// <summary>Result for the period.</summary>
    string Resultaat { get; }

    /// <summary>Revenue.</summary>
    string Opbrengsten { get; }

    /// <summary>Expenses.</summary>
    string Kosten { get; }

    /// <summary>Total.</summary>
    string Totaal { get; }

    /// <summary>Operating activities.</summary>
    string Operationeel { get; }

    /// <summary>Investing activities.</summary>
    string Investering { get; }

    /// <summary>Financing activities.</summary>
    string Financiering { get; }

    /// <summary>Opening cash balance.</summary>
    string BeginSaldo { get; }

    /// <summary>Closing cash balance.</summary>
    string EindSaldo { get; }

    /// <summary>Net cash movement.</summary>
    string NettoMutatie { get; }

    /// <summary>Current ratio.</summary>
    string CurrentRatio { get; }

    /// <summary>Quick ratio.</summary>
    string QuickRatio { get; }

    /// <summary>Solvency.</summary>
    string Solvabiliteit { get; }

    /// <summary>Net profit margin.</summary>
    string NettoWinstmarge { get; }

    /// <summary>Return on equity.</summary>
    string RentabiliteitEigenVermogen { get; }

    /// <summary>Column header: account.</summary>
    string Rekening { get; }

    /// <summary>Column header: description.</summary>
    string Omschrijving { get; }

    /// <summary>Column header: amount.</summary>
    string Bedrag { get; }

    /// <summary>Text shown when a statement has no data.</summary>
    string GeenGegevens { get; }

    /// <summary>Label prefix for the generation timestamp.</summary>
    string GegenereerdOp { get; }

    /// <summary>Label for the reporting period.</summary>
    string Periode { get; }

    /// <summary>Message shown when the ledger balances.</summary>
    string InBalans { get; }

    /// <summary>Message shown when the ledger does not balance.</summary>
    string NietInBalans { get; }

    /// <summary>Title of the combined dashboard report.</summary>
    string OverzichtTitel { get; }

    /// <summary>Description of the combined dashboard report.</summary>
    string OverzichtOmschrijving { get; }

    /// <summary>Resolves a label by key (used by generic exporters).</summary>
    /// <param name="sleutel">The label key, matching one of the property names.</param>
    string this[string sleutel] { get; }
}
