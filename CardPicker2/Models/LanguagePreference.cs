using Microsoft.AspNetCore.Localization;

namespace CardPicker2.Models;

/// <summary>
/// Describes the current request's language preference state.
/// </summary>
/// <param name="CookieName">The ASP.NET Core culture cookie name.</param>
/// <param name="CultureName">The resolved supported language.</param>
/// <param name="ExpiresUtc">The cookie expiration when persistence is available.</param>
/// <param name="CanPersist">Whether the preference can be persisted to a cookie.</param>
/// <param name="IsFallback">Whether the safe default was used because input was missing or invalid.</param>
public sealed record LanguagePreference(
    string CookieName,
    SupportedLanguage CultureName,
    DateTimeOffset? ExpiresUtc,
    bool CanPersist,
    bool IsFallback)
{
    /// <summary>
    /// Creates a preference result.
    /// </summary>
    /// <param name="language">The resolved language.</param>
    /// <param name="canPersist">Whether persistence is available.</param>
    /// <param name="expiresUtc">The cookie expiration when known.</param>
    /// <param name="isFallback">Whether fallback was used.</param>
    /// <returns>A language preference result.</returns>
    public static LanguagePreference Create(
        SupportedLanguage language,
        bool canPersist = true,
        DateTimeOffset? expiresUtc = null,
        bool isFallback = false)
    {
        return new LanguagePreference(
            CookieRequestCultureProvider.DefaultCookieName,
            language,
            expiresUtc,
            canPersist,
            isFallback);
    }
}
