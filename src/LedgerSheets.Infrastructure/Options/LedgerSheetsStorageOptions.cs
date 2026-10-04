namespace LedgerSheets.Infrastructure.Options;

/// <summary>
/// Configuration for the embedded SharpCoreDB storage.
/// </summary>
/// <remarks>
/// The database file is encrypted with <see cref="MasterPassword"/>; losing the password means
/// losing the data, so the value must come from a secret store (user secrets, environment
/// variables or a key vault) and never from source control.
/// </remarks>
public sealed class LedgerSheetsStorageOptions
{
    /// <summary>The configuration section these options are bound to.</summary>
    public const string Section = "LedgerSheets:Storage";

    /// <summary>
    /// The database location. A path ending in <c>.scdb</c> (or an existing file) selects
    /// single-file storage; any other path selects directory storage.
    /// </summary>
    public string DatabasePath { get; set; } = "App_Data/ledgersheets.scdb";

    /// <summary>The master password used to encrypt the database.</summary>
    public string MasterPassword { get; set; } = string.Empty;
}
