using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Boekhouden;

/// <summary>
/// An inclusive date range (a financial period such as a month, quarter or financial year).
/// </summary>
public readonly record struct Periode
{
    /// <summary>Creates a period from a start and end date (both inclusive).</summary>
    /// <exception cref="DomeinException">Thrown when the end date precedes the start date.</exception>
    public Periode(DateOnly start, DateOnly eind)
    {
        if (eind < start)
        {
            throw new DomeinException($"De einddatum ({eind:yyyy-MM-dd}) ligt voor de startdatum ({start:yyyy-MM-dd}).");
        }

        Start = start;
        Eind = eind;
    }

    /// <summary>The first day of the period (inclusive).</summary>
    public DateOnly Start { get; }

    /// <summary>The last day of the period (inclusive).</summary>
    public DateOnly Eind { get; }

    /// <summary>The calendar year in which the period starts.</summary>
    public int Jaar => Start.Year;

    /// <summary>The number of days covered by the period.</summary>
    public int AantalDagen => Eind.DayNumber - Start.DayNumber + 1;

    /// <summary>True when the given date falls within the period (inclusive).</summary>
    public bool Bevat(DateOnly datum) => datum >= Start && datum <= Eind;

    /// <summary>Creates a full calendar-year period.</summary>
    public static Periode VoorJaar(int jaar) => new(new DateOnly(jaar, 1, 1), new DateOnly(jaar, 12, 31));

    /// <summary>Creates a single-day period.</summary>
    public static Periode VoorDag(DateOnly datum) => new(datum, datum);

    /// <inheritdoc />
    public override string ToString() => $"{Start:yyyy-MM-dd} t/m {Eind:yyyy-MM-dd}";
}
