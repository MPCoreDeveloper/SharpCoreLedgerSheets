using System.Globalization;
using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Opslag;

/// <summary>
/// Shared helpers for the SharpCoreDB repositories.
/// </summary>
/// <remarks>
/// The embedded engine is not designed for concurrent writers, so every database interaction is
/// serialised through the database handle's monitor. A row is exposed as
/// <c>Dictionary&lt;string, object&gt;</c>; the accessors below tolerate differing column-name
/// casing and any numeric representation the engine returns.
/// </remarks>
internal abstract class SharpCoreDbRepository
{
    /// <summary>Creates a repository over the given database handle.</summary>
    protected SharpCoreDbRepository(IDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        Database = database;
    }

    /// <summary>The shared database handle.</summary>
    protected IDatabase Database { get; }

    /// <summary>Runs a database operation while holding the database lock.</summary>
    protected T MetLock<T>(Func<T> actie)
    {
        lock (Database)
        {
            return actie();
        }
    }

    /// <summary>Runs a database operation while holding the database lock.</summary>
    protected void MetLock(Action actie)
    {
        lock (Database)
        {
            actie();
        }
    }

    /// <summary>Formats a GUID for storage.</summary>
    protected static string NaarTekst(Guid waarde) => waarde.ToString("D");

    /// <summary>Formats a date for storage as ISO-8601 (<c>yyyy-MM-dd</c>).</summary>
    protected static string NaarTekst(DateOnly waarde) =>
        waarde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Returns the raw value of a column, ignoring casing and missing columns.</summary>
    protected static object? Waarde(IReadOnlyDictionary<string, object> rij, string kolom)
    {
        if (rij.TryGetValue(kolom, out var direct))
        {
            return direct;
        }

        foreach (var paar in rij)
        {
            if (string.Equals(paar.Key, kolom, StringComparison.OrdinalIgnoreCase))
            {
                return paar.Value;
            }
        }

        return null;
    }

    /// <summary>Reads a string column.</summary>
    protected static string LeesTekst(IReadOnlyDictionary<string, object> rij, string kolom) =>
        Waarde(rij, kolom) is { } waarde
            ? Convert.ToString(waarde, CultureInfo.InvariantCulture) ?? string.Empty
            : string.Empty;

    /// <summary>Reads an optional string column (<see langword="null"/> when absent or empty).</summary>
    protected static string? LeesOptioneleTekst(IReadOnlyDictionary<string, object> rij, string kolom)
    {
        var tekst = LeesTekst(rij, kolom);
        return string.IsNullOrWhiteSpace(tekst) ? null : tekst;
    }

    /// <summary>Reads an integer column.</summary>
    protected static int LeesGeheel(IReadOnlyDictionary<string, object> rij, string kolom) =>
        Waarde(rij, kolom) is { } waarde ? Convert.ToInt32(waarde, CultureInfo.InvariantCulture) : 0;

    /// <summary>Reads a boolean column stored as 0/1.</summary>
    protected static bool LeesBool(IReadOnlyDictionary<string, object> rij, string kolom) =>
        Waarde(rij, kolom) is { } waarde && Convert.ToInt32(waarde, CultureInfo.InvariantCulture) != 0;

    /// <summary>Reads a monetary column.</summary>
    protected static decimal LeesGeld(IReadOnlyDictionary<string, object> rij, string kolom) =>
        Waarde(rij, kolom) is { } waarde ? Convert.ToDecimal(waarde, CultureInfo.InvariantCulture) : 0m;

    /// <summary>Reads a GUID column.</summary>
    protected static Guid LeesGuid(IReadOnlyDictionary<string, object> rij, string kolom) =>
        Guid.TryParse(LeesTekst(rij, kolom), out var waarde) ? waarde : Guid.Empty;

    /// <summary>Reads an ISO-8601 date column.</summary>
    protected static DateOnly LeesDatum(IReadOnlyDictionary<string, object> rij, string kolom) =>
        DateOnly.TryParseExact(LeesTekst(rij, kolom), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var waarde)
            ? waarde
            : DateOnly.MinValue;
}
