using CardPicker2.Models;

using Microsoft.AspNetCore.Localization;

namespace CardPicker2.Services;

/// <summary>
/// Validates and persists the user's runtime language preference.
/// </summary>
public sealed class LanguagePreferenceService
{
    /// <summary>
    /// Resolves a candidate culture name to a supported language.
    /// </summary>
    /// <param name="cultureName">The candidate culture name.</param>
    /// <returns>A supported language, defaulting to Traditional Chinese.</returns>
    public SupportedLanguage ResolveSupportedLanguage(string? cultureName)
    {
        return SupportedLanguage.FromCultureNameOrDefault(cultureName);
    }

    /// <summary>
    /// Parses an ASP.NET Core culture cookie and resolves unsupported or mismatched values to the safe default.
    /// </summary>
    /// <param name="cookieValue">The raw cookie value.</param>
    /// <returns>The resolved language preference.</returns>
    public LanguagePreference ResolveCookieValue(string? cookieValue)
    {
        if (string.IsNullOrWhiteSpace(cookieValue))
        {
            return LanguagePreference.Create(SupportedLanguage.ZhTw, isFallback: true);
        }

        var providerResult = CookieRequestCultureProvider.ParseCookieValue(cookieValue);
        if (providerResult is null)
        {
            return LanguagePreference.Create(SupportedLanguage.ZhTw, isFallback: true);
        }

        var culture = providerResult.Cultures.FirstOrDefault().Value;
        var uiCulture = providerResult.UICultures.FirstOrDefault().Value;
        if (!string.Equals(culture, uiCulture, StringComparison.OrdinalIgnoreCase) ||
            !SupportedLanguage.TryGet(culture, out var language))
        {
            return LanguagePreference.Create(SupportedLanguage.ZhTw, isFallback: true);
        }

        return LanguagePreference.Create(language);
    }

    /// <summary>
    /// Creates the framework culture cookie value for a supported language.
    /// </summary>
    /// <param name="language">The supported language.</param>
    /// <returns>The cookie value.</returns>
    public string CreateCookieValue(SupportedLanguage language)
    {
        return CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language.CultureName, language.CultureName));
    }

    /// <summary>
    /// Safely normalizes a return URL.
    /// </summary>
    /// <param name="returnUrl">The requested return URL.</param>
    /// <returns>A local URL, or the home page.</returns>
    public string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !returnUrl.StartsWith("/", StringComparison.Ordinal) ||
            returnUrl.StartsWith("//", StringComparison.Ordinal) ||
            returnUrl.Contains("://", StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }

    /// <summary>
    /// Creates secure-by-default culture cookie options for the current request.
    /// </summary>
    /// <param name="request">The current HTTP request.</param>
    /// <returns>Cookie options for language preference persistence.</returns>
    public CookieOptions CreateCookieOptions(HttpRequest request)
    {
        return new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = request.IsHttps
        };
    }
}
