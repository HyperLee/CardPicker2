namespace CardPicker2.Models;

/// <summary>
/// Represents one runtime UI language supported by the application.
/// </summary>
/// <param name="CultureName">The .NET culture name.</param>
/// <param name="TraditionalChineseDisplayName">The display name shown in Traditional Chinese UI.</param>
/// <param name="EnglishDisplayName">The display name shown in English UI.</param>
/// <param name="HtmlLang">The HTML lang attribute value.</param>
/// <param name="IsDefault">Whether this language is the safe default.</param>
public sealed record SupportedLanguage(
    string CultureName,
    string TraditionalChineseDisplayName,
    string EnglishDisplayName,
    string HtmlLang,
    bool IsDefault)
{
    /// <summary>
    /// Gets the Traditional Chinese culture name.
    /// </summary>
    public const string ZhTwCultureName = "zh-TW";

    /// <summary>
    /// Gets the English culture name.
    /// </summary>
    public const string EnUsCultureName = "en-US";

    /// <summary>
    /// Gets the default Traditional Chinese language.
    /// </summary>
    public static readonly SupportedLanguage ZhTw = new(
        ZhTwCultureName,
        "繁體中文",
        "Traditional Chinese",
        "zh-Hant",
        true);

    /// <summary>
    /// Gets the supported English language.
    /// </summary>
    public static readonly SupportedLanguage EnUs = new(
        EnUsCultureName,
        "英文",
        "English",
        "en",
        false);

    /// <summary>
    /// Gets all supported languages in display order.
    /// </summary>
    public static IReadOnlyList<SupportedLanguage> All { get; } = new[] { ZhTw, EnUs };

    /// <summary>
    /// Resolves a culture name to a supported language.
    /// </summary>
    /// <param name="cultureName">The candidate culture name.</param>
    /// <param name="language">The supported language when found.</param>
    /// <returns><see langword="true"/> when the culture is supported.</returns>
    public static bool TryGet(string? cultureName, out SupportedLanguage language)
    {
        language = All.FirstOrDefault(candidate =>
            string.Equals(candidate.CultureName, cultureName, StringComparison.OrdinalIgnoreCase)) ?? ZhTw;

        return string.Equals(language.CultureName, cultureName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves a culture name to a supported language, falling back to Traditional Chinese.
    /// </summary>
    /// <param name="cultureName">The candidate culture name.</param>
    /// <returns>The supported language.</returns>
    public static SupportedLanguage FromCultureNameOrDefault(string? cultureName)
    {
        return TryGet(cultureName, out var language) ? language : ZhTw;
    }

    /// <summary>
    /// Gets this language's name as it should appear in the current UI language.
    /// </summary>
    /// <param name="currentLanguage">The current UI language.</param>
    /// <returns>A localized display name for this language.</returns>
    public string GetDisplayName(SupportedLanguage currentLanguage)
    {
        return currentLanguage == EnUs ? EnglishDisplayName : TraditionalChineseDisplayName;
    }
}
