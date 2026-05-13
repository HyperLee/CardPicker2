using CardPicker2.Services;

using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker2.Pages;

public sealed class LanguageModel : PageModel
{
    private readonly LanguagePreferenceService _languagePreferenceService;
    private readonly ILogger<LanguageModel> _logger;

    public LanguageModel(LanguagePreferenceService languagePreferenceService, ILogger<LanguageModel> logger)
    {
        _languagePreferenceService = languagePreferenceService;
        _logger = logger;
    }

    public IActionResult OnPostSet(string? culture, string? returnUrl)
    {
        var language = _languagePreferenceService.ResolveSupportedLanguage(culture);
        var safeReturnUrl = _languagePreferenceService.GetSafeReturnUrl(returnUrl);

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            _languagePreferenceService.CreateCookieValue(language),
            _languagePreferenceService.CreateCookieOptions(Request));

        _logger.LogInformation(
            "Language preference changed to {CultureName}; fallback applied: {FallbackApplied}",
            language.CultureName,
            !string.Equals(culture, language.CultureName, StringComparison.OrdinalIgnoreCase));

        return LocalRedirect(safeReturnUrl);
    }
}
