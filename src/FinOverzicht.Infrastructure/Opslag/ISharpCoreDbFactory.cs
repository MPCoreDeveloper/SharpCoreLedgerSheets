using SharpCoreDB.Interfaces;

namespace FinOverzicht.Infrastructure.Opslag;

/// <summary>
/// Opens the encrypted SharpCoreDB database and guarantees that the schema exists.
/// </summary>
/// <remarks>
/// Registered as a singleton: a single long-lived database handle serves the whole application,
/// which matches both the embedded nature of the engine and the single-administration model.
/// </remarks>
public interface ISharpCoreDbFactory
{
    /// <summary>Opens (or creates) the database and applies the schema.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no master password is configured.</exception>
    IDatabase Open();
}
