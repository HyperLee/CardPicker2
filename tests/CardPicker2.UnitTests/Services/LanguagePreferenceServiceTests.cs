using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class LanguagePreferenceServiceTests
{
    [Fact]
    public void ResolveSupportedLanguage_WithMissingOrInvalidCulture_ReturnsTraditionalChinese()
    {
        var service = new LanguagePreferenceService();

        Assert.Equal(SupportedLanguage.ZhTw, service.ResolveSupportedLanguage(null));
        Assert.Equal(SupportedLanguage.ZhTw, service.ResolveSupportedLanguage("fr-FR"));
        Assert.Equal("zh-Hant", service.ResolveSupportedLanguage("zh-TW").HtmlLang);
    }

    [Fact]
    public void ResolveSupportedLanguage_WithEnglishCulture_ReturnsEnglish()
    {
        var service = new LanguagePreferenceService();

        var language = service.ResolveSupportedLanguage("en-US");

        Assert.Equal(SupportedLanguage.EnUs, language);
        Assert.Equal("en", language.HtmlLang);
    }

    [Fact]
    public void ResolveCookieValue_RequiresSupportedMatchingCultureAndUiCulture()
    {
        var service = new LanguagePreferenceService();

        var english = service.ResolveCookieValue(service.CreateCookieValue(SupportedLanguage.EnUs));
        var unsupported = service.ResolveCookieValue("c=fr-FR|uic=fr-FR");
        var mismatched = service.ResolveCookieValue("c=en-US|uic=zh-TW");

        Assert.Equal(SupportedLanguage.EnUs, english.CultureName);
        Assert.Equal(SupportedLanguage.ZhTw, unsupported.CultureName);
        Assert.Equal(SupportedLanguage.ZhTw, mismatched.CultureName);
    }

    [Theory]
    [InlineData("/Cards?keyword=Rice", "/Cards?keyword=Rice")]
    [InlineData("", "/")]
    [InlineData("https://example.com", "/")]
    [InlineData("//example.com/Cards", "/")]
    public void GetSafeReturnUrl_AllowsOnlyLocalUrls(string? returnUrl, string expected)
    {
        var service = new LanguagePreferenceService();

        Assert.Equal(expected, service.GetSafeReturnUrl(returnUrl));
    }
}
