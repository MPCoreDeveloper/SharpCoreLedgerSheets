namespace SharpCoreLedgerSheets.Web.Resources;

/// <summary>
/// Marker type for the shared localized strings.
/// </summary>
/// <remarks>
/// The resource files live in <c>Resources/SharedResource.resx</c> (Dutch, the neutral fallback)
/// and <c>Resources/SharedResource.&lt;culture&gt;.resx</c> for English, German, French, Spanish,
/// Italian, Portuguese and Polish. <c>IStringLocalizer&lt;SharedResource&gt;</c> resolves them.
/// </remarks>
public sealed class SharedResource;
