using FinOverzicht.Infrastructure.Options;
using Microsoft.Extensions.Options;
using SharpCoreDB;
using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Opslag;

/// <summary>
/// Default <see cref="ISharpCoreDbFactory"/> backed by SharpCoreDB's <see cref="DatabaseFactory"/>.
/// </summary>
internal sealed class SharpCoreDbFactory(
    DatabaseFactory databaseFactory,
    IOptions<FinOverzichtOpslagOptions> options) : ISharpCoreDbFactory
{
    private readonly FinOverzichtOpslagOptions _options = options.Value;

    /// <inheritdoc />
    public IDatabase Open()
    {
        if (string.IsNullOrWhiteSpace(_options.MasterWachtwoord))
        {
            throw new InvalidOperationException(
                "Er is geen master-wachtwoord geconfigureerd voor de database " +
                $"('{FinOverzichtOpslagOptions.Sectie}:{nameof(FinOverzichtOpslagOptions.MasterWachtwoord)}').");
        }

        var pad = Path.GetFullPath(_options.DatabasePad);
        var map = Path.GetDirectoryName(pad);

        if (!string.IsNullOrEmpty(map))
        {
            Directory.CreateDirectory(map);
        }

        var database = databaseFactory.Create(pad, _options.MasterWachtwoord);
        GrootboekSchema.ZorgVoorSchema(database);
        BankstaatSchema.ZorgVoorSchema(database);
        return database;
    }
}
