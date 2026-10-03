using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Opslag;

/// <summary>
/// The part of the FinOverzicht schema that belongs to the bank import: the imported bank movements
/// and the matching rules the assistant learned from confirmed bookings.
/// </summary>
/// <remarks>
/// A movement keeps the reference of the bank (<c>referentie</c>) next to the bank account it belongs
/// to, because together they form the import key: importing the same statement twice must never
/// duplicate a movement. The status is stored as an integer so it can be filtered without a string
/// comparison and the booking is stored as the identifier of the journal entry it produced. Every
/// statement is idempotent (<c>IF NOT EXISTS</c>), so applying the schema is safe on every start-up.
/// </remarks>
internal static class BankstaatSchema
{
    internal const string TabelMutaties = "bankmutaties";
    internal const string TabelMatchregels = "matchregels";

    // SharpCoreDB derives metadata block names from table and index names and caps block names at 31
    // characters (see GrootboekSchema), so both the table names above and the index names below are
    // kept short: "bankmutaties_ux_bank_id" is 23 characters.
    private static readonly string[] Statements =
    [
        """
        CREATE TABLE IF NOT EXISTS bankmutaties (
            id TEXT,
            bank_nr TEXT,
            datum TEXT,
            bedrag DECIMAL,
            valuta TEXT,
            referentie TEXT,
            tegenpartij TEXT,
            iban TEXT,
            omschrijving TEXT,
            status INTEGER,
            post_id TEXT
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_bank_id ON bankmutaties (id)",
        "CREATE INDEX IF NOT EXISTS ix_bank_datum ON bankmutaties (datum)",
        """
        CREATE TABLE IF NOT EXISTS matchregels (
            trefwoord TEXT,
            tegenrekening TEXT,
            bevestigingen INTEGER
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_match_tw ON matchregels (trefwoord)",
    ];

    /// <summary>Creates every table and index when it does not exist yet.</summary>
    internal static void ZorgVoorSchema(IDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        foreach (var statement in Statements)
        {
            database.ExecuteSQL(statement);
        }

        database.Flush();
    }
}
