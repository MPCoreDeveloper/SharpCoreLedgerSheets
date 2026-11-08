namespace SharpCoreLedgerSheets.Web.Localization;

/// <summary>
/// The languages SharpCoreLedgerSheets ships translations for.
/// </summary>
/// <remarks>
/// The product targets the Dutch MKB (small and medium-sized businesses) first, so Dutch is the
/// default culture: a Dutch entrepreneur gets a Dutch application without configuring anything, with
/// Dutch number and date formatting (1.234,56 and 31-12-2026). English is the neutral fallback - the
/// neutral <c>SharedResource.resx</c> holds the English text - so an international user is never
/// stranded on a missing key, and every culture can be selected explicitly.
/// </remarks>
public static class Languages
{
    /// <summary>The default culture: Dutch, because the target market is the Dutch MKB.</summary>
    public const string Default = "nl";

    /// <summary>The query-string key used to switch language (matches the ASP.NET Core default).</summary>
    public const string CultureQueryKey = "culture";

    /// <summary>Every supported culture, Dutch (the default) first.</summary>
    public static readonly string[] Supported = ["nl", "en", "de", "fr", "es", "it", "pt", "pl"];

    /// <summary>True when the culture (for example <c>the</c>) is supported.</summary>
    public static bool IsSupported(string culture) =>
        Supported.Contains(culture, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the display name of a culture in its own language (for the language picker).</summary>
    public static string DisplayName(string culture) => culture switch
    {
        // Each name stays in its own language on purpose: a language picker that translates them is
        // unusable for the reader who needs it (mapping.md 12, 'Language picker - do not translate').
        "nl" => "Nederlands",
        "en" => "English",
        "de" => "Deutsch",
        "fr" => "Français",
        "es" => "Español",
        "it" => "Italiano",
        "pt" => "Português",
        "pl" => "Polski",
        _ => culture,
    };
}
