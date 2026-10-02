namespace FinOverzicht.Infrastructure.Options;

/// <summary>
/// Configuration for the embedded SharpCoreDB storage.
/// </summary>
/// <remarks>
/// The database file is encrypted with <see cref="MasterWachtwoord"/>; losing the password means
/// losing the data, so the value must come from a secret store (user secrets, environment
/// variables or a key vault) and never from source control.
/// </remarks>
public sealed class FinOverzichtOpslagOptions
{
    /// <summary>The configuration section these options are bound to.</summary>
    public const string Sectie = "FinOverzicht:Opslag";

    /// <summary>
    /// The database location. A path ending in <c>.scdb</c> (or an existing file) selects
    /// single-file storage; any other path selects directory storage.
    /// </summary>
    public string DatabasePad { get; set; } = "App_Data/finoverzicht.scdb";

    /// <summary>The master password used to encrypt the database.</summary>
    public string MasterWachtwoord { get; set; } = string.Empty;
}
