using System.Globalization;
using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

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
    protected T WithLock<T>(Func<T> action)
    {
        lock (Database)
        {
            return action();
        }
    }

    /// <summary>Runs a database operation while holding the database lock.</summary>
    protected void WithLock(Action action)
    {
        lock (Database)
        {
            action();
        }
    }

    /// <summary>Formats a GUID for storage.</summary>
    protected static string ToText(Guid value) => value.ToString("D");

    /// <summary>Formats a date for storage as ISO-8601 (<c>yyyy-MM-dd</c>).</summary>
    protected static string ToText(DateOnly value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Returns the raw value of a column, ignoring casing and missing columns.</summary>
    protected static object? Value(IReadOnlyDictionary<string, object> row, string column)
    {
        if (row.TryGetValue(column, out var direct))
        {
            return direct;
        }

        foreach (var paar in row)
        {
            if (string.Equals(paar.Key, column, StringComparison.OrdinalIgnoreCase))
            {
                return paar.Value;
            }
        }

        return null;
    }

    /// <summary>Reads a string column.</summary>
    protected static string ReadText(IReadOnlyDictionary<string, object> row, string column) =>
        Value(row, column) is { } value
            ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            : string.Empty;

    /// <summary>Reads an optional string column (<see langword="null"/> when absent or empty).</summary>
    protected static string? ReadOptionalText(IReadOnlyDictionary<string, object> row, string column)
    {
        var text = ReadText(row, column);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>Reads an integer column.</summary>
    protected static int ReadInteger(IReadOnlyDictionary<string, object> row, string column) =>
        Value(row, column) is { } value ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : 0;

    /// <summary>Reads a boolean column stored as 0/1.</summary>
    protected static bool ReadBool(IReadOnlyDictionary<string, object> row, string column) =>
        Value(row, column) is { } value && Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;

    /// <summary>Reads a monetary column.</summary>
    protected static decimal ReadAmount(IReadOnlyDictionary<string, object> row, string column) =>
        Value(row, column) is { } value ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) : 0m;

    /// <summary>Reads a GUID column.</summary>
    protected static Guid ReadGuid(IReadOnlyDictionary<string, object> row, string column) =>
        Guid.TryParse(ReadText(row, column), out var value) ? value : Guid.Empty;

    /// <summary>Reads an ISO-8601 date column.</summary>
    protected static DateOnly ReadDate(IReadOnlyDictionary<string, object> row, string column) =>
        DateOnly.TryParseExact(ReadText(row, column), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : DateOnly.MinValue;
}
