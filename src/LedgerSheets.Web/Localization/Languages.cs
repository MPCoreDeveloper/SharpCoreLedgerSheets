namespace LedgerSheets.Web.Localization;

/// <summary>
/// The languages LedgerSheets ships translations for.
/// </summary>
public static class Languages
{
    /// <summary>The default culture (English: the neutral resource file).</summary>
    public const string Default = "en";

    /// <summary>The query-string key used to switch language (matches the ASP.NET Core default).</summary>
    public const string CultureQueryKey = "culture";

    /// <summary>Every supported culture, English first.</summary>
    public static readonly string[] Supported = ["en", "nl", "de", "fr", "es", "it", "pt", "pl"];

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
