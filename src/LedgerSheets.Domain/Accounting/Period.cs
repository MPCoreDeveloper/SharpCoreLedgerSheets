using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Accounting;

/// <summary>
/// An inclusive date range (a financial period such as a month, quarter or financial year).
/// </summary>
public readonly record struct Period
{
    /// <summary>Creates a period from a start and end date (both inclusive).</summary>
    /// <exception cref="DomainException">Thrown when the end date precedes the start date.</exception>
    public Period(DateOnly start, DateOnly closing)
    {
        if (closing < start)
        {
            throw new DomainException($"De einddatum ({closing:yyyy-MM-dd}) ligt voor de startdatum ({start:yyyy-MM-dd}).");
        }

        Start = start;
        Closing = closing;
    }

    /// <summary>The first day of the period (inclusive).</summary>
    public DateOnly Start { get; }

    /// <summary>The last day of the period (inclusive).</summary>
    public DateOnly Closing { get; }

    /// <summary>The calendar year in which the period starts.</summary>
    public int Year => Start.Year;

    /// <summary>The number of days covered by the period.</summary>
    public int CountDays => Closing.DayNumber - Start.DayNumber + 1;

    /// <summary>True when the given date falls within the period (inclusive).</summary>
    public bool Contains(DateOnly date) => date >= Start && date <= Closing;

    /// <summary>Creates a full calendar-year period.</summary>
    public static Period ForYear(int year) => new(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));

    /// <summary>Creates a single-day period.</summary>
    public static Period ForDay(DateOnly date) => new(date, date);

    /// <inheritdoc />
    public override string ToString() => $"{Start:yyyy-MM-dd} t/m {Closing:yyyy-MM-dd}";
}
