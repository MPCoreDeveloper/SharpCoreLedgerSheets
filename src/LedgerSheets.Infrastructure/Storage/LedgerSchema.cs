using SharpCoreDB.Interfaces;

namespace LedgerSheets.Infrastructure.Storage;

/// <summary>
/// The LedgerSheets database schema: accounts, journal entries and their lines.
/// </summary>
/// <remarks>
/// Amounts are stored as <c>DECIMAL</c> and dates as ISO-8601 <c>TEXT</c> (<c>yyyy-MM-dd</c>) so
/// they sort chronologically and are unambiguous across cultures. Every statement is idempotent
/// (<c>IF NOT EXISTS</c>) so applying the schema is safe on every start-up.
/// </remarks>
internal static class LedgerSchema
{
    internal const string TableAccounts = "rekeningen";
    internal const string TableEntries = "posten";
    internal const string TableLines = "postregels";

    // SharpCoreDB derives metadata block names from table and index names (for example
    // "table:<table>:overflow" and "<prefix>:<table>_<index>") and caps block names at 31
    // characters. Both the table names above and the index names below are therefore kept short.
    private static readonly string[] Statements =
    [
        """
        CREATE TABLE IF NOT EXISTS rekeningen (
            id TEXT,
            nummer TEXT,
            naam TEXT,
            soort INTEGER,
            is_actief INTEGER,
            is_vlottend INTEGER,
            is_liquide_middel INTEGER,
            is_voorraad INTEGER
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_rek_id ON rekeningen (id)",
        "CREATE INDEX IF NOT EXISTS ix_rek_nr ON rekeningen (nummer)",
        """
        CREATE TABLE IF NOT EXISTS posten (
            id TEXT,
            datum TEXT,
            omschrijving TEXT,
            referentie TEXT
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_post_id ON posten (id)",
        "CREATE INDEX IF NOT EXISTS ix_post_dt ON posten (datum)",
        """
        CREATE TABLE IF NOT EXISTS postregels (
            id TEXT,
            post_id TEXT,
            volgnummer INTEGER,
            rekening_id TEXT,
            debet DECIMAL,
            credit DECIMAL,
            omschrijving TEXT
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_regel_id ON postregels (id)",
        "CREATE INDEX IF NOT EXISTS ix_regel_post ON postregels (post_id)",
    ];

    /// <summary>Creates every table and index when it does not exist yet.</summary>
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
