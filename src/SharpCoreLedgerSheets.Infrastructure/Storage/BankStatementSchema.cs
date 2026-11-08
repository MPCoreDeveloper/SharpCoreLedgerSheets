using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

/// <summary>
/// The part of the SharpCoreLedgerSheets schema that belongs to the bank import: the imported bank
/// transactions and the matching rules the assistant learned from confirmed bookings.
/// </summary>
/// <remarks>
/// A movement keeps the reference of the bank (<c>reference</c>) next to the bank account it belongs
/// to, because together they form the import key: importing the same statement twice must never
/// duplicate a movement. The status is stored as an integer so it can be filtered without a string
/// comparison and the booking is stored as the identifier of the journal entry it produced
/// (<c>entry_id</c>). Every statement is idempotent (<c>IF NOT EXISTS</c>), so applying the schema is
/// safe on every start-up.
/// </remarks>
internal static class BankStatementSchema
{
    // English names, kept short: SharpCoreDB caps derived metadata block names at 31 characters (see
    // LedgerSchema), so "bank transactions" becomes "banktxns" and the index names are abbreviated.
    internal const string TableMovements = "banktxns";
    internal const string TableMatchRules = "matchrules";

    private static readonly string[] Statements =
    [
        """
        CREATE TABLE IF NOT EXISTS banktxns (
            id TEXT,
            bank_account TEXT,
            date TEXT,
            amount DECIMAL,
            currency TEXT,
            reference TEXT,
            counterparty TEXT,
            iban TEXT,
            description TEXT,
            status INTEGER,
            entry_id TEXT
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_bank_id ON banktxns (id)",
        "CREATE INDEX IF NOT EXISTS ix_bank_date ON banktxns (date)",
        """
        CREATE TABLE IF NOT EXISTS matchrules (
            keyword TEXT,
            counter_account TEXT,
            confirmations INTEGER
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_match_kw ON matchrules (keyword)",
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
