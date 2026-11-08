using SharpCoreLedgerSheets.Infrastructure.Options;
using Microsoft.Extensions.Options;
using SharpCoreDB;
using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

/// <summary>
/// Default <see cref="ISharpCoreDbFactory"/> backed by SharpCoreDB's <see cref="DatabaseFactory"/>.
/// </summary>
internal sealed class SharpCoreDbFactory(
    DatabaseFactory databaseFactory,
    IOptions<SharpCoreLedgerSheetsStorageOptions> options) : ISharpCoreDbFactory
{
    private readonly SharpCoreLedgerSheetsStorageOptions _options = options.Value;

    /// <inheritdoc />
    public IDatabase Open()
    {
        if (string.IsNullOrWhiteSpace(_options.MasterPassword))
        {
            throw new InvalidOperationException(
                "No master password is configured for the ledger database " +
                $"('{SharpCoreLedgerSheetsStorageOptions.Section}:{nameof(SharpCoreLedgerSheetsStorageOptions.MasterPassword)}').");
        }

        var path = Path.GetFullPath(_options.DatabasePath);
        var map = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(map))
        {
            Directory.CreateDirectory(map);
        }

        var database = databaseFactory.Create(path, _options.MasterPassword);
        LedgerSchema.EnsureSchema(database);
        BankStatementSchema.EnsureSchema(database);
        return database;
    }
}
