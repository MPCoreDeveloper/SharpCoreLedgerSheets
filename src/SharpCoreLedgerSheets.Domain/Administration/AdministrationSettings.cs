using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Domain.Administration;

/// <summary>
/// The settings of one administration: what it is called, when its book year starts, in which currency it
/// keeps its books and which VAT rates it uses.
/// </summary>
/// <remarks>
/// The fiscal year is expressed as the month it starts in (day 1). Dutch small businesses commonly use a
/// broken book year - February to January, for example - and every statement has to follow it, so the
/// setting is part of the administration rather than a per-report choice.
/// </remarks>
public sealed class AdministrationSettings
{
    /// <summary>The name used when no company name has been configured.</summary>
    public const string DefaultCompanyName = "Mijn administratie";

    /// <summary>January: a book year that runs from 1 January to 31 December.</summary>
    public const int CalendarYearStart = 1;

    /// <summary>Creates administration settings.</summary>
    /// <exception cref="DomainException">Thrown when the month is not 1..12 or the name is blank.</exception>
    public AdministrationSettings(
        string companyName,
        int fiscalYearStartMonth = CalendarYearStart,
        Currency? currency = null,
        VatScheme? vatScheme = null)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new DomainException(MessageCode.CompanyNameRequired, "Een administratie moet een naam hebben.");
        }

        if (fiscalYearStartMonth is < 1 or > 12)
        {
            throw new DomainException(
                MessageCode.FiscalYearMonthInvalid,
                "De startmaand van het boekjaar moet tussen 1 en 12 liggen.");
        }

        CompanyName = companyName.Trim();
        FiscalYearStartMonth = fiscalYearStartMonth;
        Currency = currency ?? Currency.Eur;
        Vat = vatScheme ?? VatScheme.Dutch;
    }

    /// <summary>The defaults for a new administration: calendar year, euro, Dutch VAT.</summary>
    public static AdministrationSettings Default { get; } = new(DefaultCompanyName);

    /// <summary>The name of the administration, shown on reports.</summary>
    public string CompanyName { get; }

    /// <summary>The month the book year starts in (1..12).</summary>
    public int FiscalYearStartMonth { get; }

    /// <summary>The currency the books are kept in.</summary>
    public Currency Currency { get; }

    /// <summary>The VAT scheme of the administration.</summary>
    public VatScheme Vat { get; }

    /// <summary>True when the book year follows the calendar.</summary>
    public bool UsesCalendarYear => FiscalYearStartMonth == CalendarYearStart;

    /// <summary>Returns the book year with the given label, honouring the configured start month.</summary>
    public Period PeriodFor(int year) => Period.ForFiscalYear(year, FiscalYearStartMonth);

    /// <summary>Returns a copy of these settings with another company name.</summary>
    public AdministrationSettings WithCompanyName(string companyName) =>
        new(companyName, FiscalYearStartMonth, Currency, Vat);

    /// <summary>Returns a copy of these settings with another fiscal year start month.</summary>
    public AdministrationSettings WithFiscalYearStartMonth(int month) =>
        new(CompanyName, month, Currency, Vat);

    /// <summary>Returns a copy of these settings with another VAT scheme.</summary>
    public AdministrationSettings WithVatScheme(VatScheme vatScheme) =>
        new(CompanyName, FiscalYearStartMonth, Currency, vatScheme);
}
