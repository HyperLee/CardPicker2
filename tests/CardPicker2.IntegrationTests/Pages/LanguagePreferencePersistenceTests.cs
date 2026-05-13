using CardPicker2.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class LanguagePreferencePersistenceTests
{
    [Fact]
    public async Task GetHome_WithPersistedEnglishCookie_RendersEnglishOnReturnVisit()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        client.AddCultureCookie("en-US");

        var html = await client.GetStringAsync("/");

        LanguageHtmlAssertions.AssertHtmlLanguage(html, "en");
        LanguageHtmlAssertions.AssertVisibleText(html, "Current language: English");
    }

    [Fact]
    public async Task GetHome_WithInvalidCultureCookie_FallsBackToTraditionalChinese()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}=c=fr-FR|uic=fr-FR");

        var html = await client.GetStringAsync("/");

        LanguageHtmlAssertions.AssertHtmlLanguage(html, "zh-Hant");
        LanguageHtmlAssertions.AssertVisibleText(html, "目前語言：繁體中文");
    }
}
