using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

/// <summary>
/// The SharpCoreLedgerSheets database schema: accounts, journal entries and their lines.
/// </summary>
/// <remarks>
/// Table, column and index names are English, because the schema is the most public part of the
/// storage contract. They stay short because SharpCoreDB derives metadata block names from table and
/// index names (for example <c>table:&lt;table&gt;:overflow</c> and
/// <c>&lt;prefix&gt;:&lt;table&gt;_&lt;index&gt;</c>) and caps a block name at 31 characters - hence
/// <c>banktxns</c> rather than <c>bank_transactions</c>, and abbreviated index names.
/// Amounts are stored as <c>DECIMAL</c> and dates as ISO-8601 <c>TEXT</c> (<c>yyyy-MM-dd</c>) so they
/// sort chronologically and are unambiguous across cultures. Every statement is idempotent
/// (<c>IF NOT EXISTS</c>) so applying the schema is safe on every start-up.
/// </remarks>
internal static class LedgerSchema
{
    internal const string TableAccounts = "accounts";
    internal const string TableEntries = "entries";
    internal const string TableLines = "entry_lines";

    private static readonly string[] Statements =
    [
        """
        CREATE TABLE IF NOT EXISTS accounts (
            id TEXT,
            number TEXT,
            name TEXT,
            type INTEGER,
            is_active INTEGER,
            is_current INTEGER,
            is_cash_equivalent INTEGER,
            is_inventory INTEGER
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_acc_id ON accounts (id)",
        "CREATE INDEX IF NOT EXISTS ix_acc_number ON accounts (number)",
        """
        CREATE TABLE IF NOT EXISTS entries (
            id TEXT,
            date TEXT,
            description TEXT,
            reference TEXT
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_entry_id ON entries (id)",
        "CREATE INDEX IF NOT EXISTS ix_entry_date ON entries (date)",
        """
        CREATE TABLE IF NOT EXISTS entry_lines (
            id TEXT,
            entry_id TEXT,
            sequence INTEGER,
            account_id TEXT,
            debit DECIMAL,
            credit DECIMAL,
            description TEXT
        )
        """,
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_line_id ON entry_lines (id)",
        "CREATE INDEX IF NOT EXISTS ix_line_entry ON entry_lines (entry_id)",
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
