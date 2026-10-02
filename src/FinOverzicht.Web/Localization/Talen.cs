namespace FinOverzicht.Web.Localization;

/// <summary>
/// The languages FinOverzicht ships translations for.
/// </summary>
public static class Talen
{
    /// <summary>The default culture (Dutch).</summary>
    public const string Standaard = "nl";

    /// <summary>The query-string key used to switch language (matches the ASP.NET Core default).</summary>
    public const string CultuurQuerySleutel = "culture";

    /// <summary>Every supported culture, Dutch first.</summary>
    public static readonly string[] Ondersteund = ["nl", "en", "de", "fr", "es", "it", "pt", "pl"];

    /// <summary>True when the culture (for example <c>de</c>) is supported.</summary>
    public static bool IsOndersteund(string cultuur) =>
        Ondersteund.Contains(cultuur, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the display name of a culture in its own language (for the language picker).</summary>
    public static string Weergavenaam(string cultuur) => cultuur switch
    {
        "nl" => "Nederlands",
        "en" => "English",
        "de" => "Deutsch",
        "fr" => "Français",
        "es" => "Español",
        "it" => "Italiano",
        "pt" => "Português",
        "pl" => "Polski",
        _ => cultuur,
    };
}
