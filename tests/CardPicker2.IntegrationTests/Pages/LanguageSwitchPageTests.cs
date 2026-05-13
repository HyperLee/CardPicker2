using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class LanguageSwitchPageTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-language-switch-tests-");
    private readonly WebApplicationFactory<Program> _factory;

    public LanguageSwitchPageTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    _library.Configure(services);
                });
            });
    }

    [Fact]
    public async Task GetHome_WithoutCultureCookie_RendersTraditionalChineseLayoutAndHome()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        LanguageHtmlAssertions.AssertHtmlLanguage(html, "zh-Hant");
        LanguageHtmlAssertions.AssertLanguageSwitcher(html);
        LanguageHtmlAssertions.AssertVisibleText(html, "餐點抽卡機", "目前語言：繁體中文", "抽卡", "卡牌庫", "早餐", "投幣確認");
    }

    [Fact]
    public async Task GetHome_WithEnglishCultureCookie_RendersEnglishLayoutAndHome()
    {
        var client = _factory.CreateClient();
        client.AddCultureCookie("en-US");

        var html = await client.GetStringAsync("/");

        LanguageHtmlAssertions.AssertHtmlLanguage(html, "en");
        LanguageHtmlAssertions.AssertLanguageSwitcher(html);
        LanguageHtmlAssertions.AssertVisibleText(html, "Meal Card Slot", "Current language: English", "Draw", "Card Library", "Breakfast", "Insert coin");
        LanguageHtmlAssertions.AssertNoVisibleText(html, "目前語言：繁體中文", "投幣確認");
    }

    public void Dispose()
    {
        _factory.Dispose();
        _library.Dispose();
    }
}
