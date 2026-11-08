using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

/// <summary>
/// The part of the SharpCoreLedgerSheets schema that holds the administration settings.
/// </summary>
/// <remarks>
/// Settings are a small key/value set: company name, fiscal year start, currency and the VAT rates (as JSON).
/// Keeping them as rows rather than columns means a new setting never needs a schema change. The columns are
/// called <c>name</c> and <c>content</c> because <c>key</c> and <c>value</c> are risky identifiers in a SQL
/// dialect.
/// </remarks>
internal static class SettingsSchema
{
    internal const string TableSettings = "settings";

    private static readonly string[] Statements =
    [
        """
        CREATE TABLE IF NOT EXISTS settings (
            name TEXT,
            content TEXT
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_settings_name ON settings (name)",
    ];

    /// <summary>Creates the settings table when it does not exist yet.</summary>
    internal static void EnsureSchema(IDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        foreach (var statement in Statements)
        {
            database.ExecuteSQL(statement);
        }

        database.Flush();
    }
}
