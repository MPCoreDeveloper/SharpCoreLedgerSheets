using LedgerSheets.Infrastructure.Options;
using Microsoft.Extensions.Options;
using SharpCoreDB;
using SharpCoreDB.Interfaces;

namespace LedgerSheets.Infrastructure.Storage;

/// <summary>
/// Default <see cref="ISharpCoreDbFactory"/> backed by SharpCoreDB's <see cref="DatabaseFactory"/>.
/// </summary>
internal sealed class SharpCoreDbFactory(
    DatabaseFactory databaseFactory,
    IOptions<LedgerSheetsStorageOptions> options) : ISharpCoreDbFactory
{
    private readonly LedgerSheetsStorageOptions _options = options.Value;

    /// <inheritdoc />
    public IDatabase Open()
    {
        if (string.IsNullOrWhiteSpace(_options.MasterPassword))
        {
            throw new InvalidOperationException(
                "Er is geen master-wachtwoord geconfigureerd voor de database " +
                $"('{LedgerSheetsStorageOptions.Section}:{nameof(LedgerSheetsStorageOptions.MasterPassword)}').");
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
