using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

using Microsoft.AspNetCore.Localization;
using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Browser;

public sealed class MetadataFilterResponsiveAccessibilityTests : IClassFixture<ThemeBrowserFixture>
{
    private readonly ThemeBrowserFixture _fixture;

    public MetadataFilterResponsiveAccessibilityTests(ThemeBrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("zh-TW", 390, 844)]
    [InlineData("en-US", 390, 844)]
    [InlineData("zh-TW", 768, 1024)]
    [InlineData("zh-TW", 1366, 768)]
    public async Task MetadataFilterSurfaces_HaveNoHorizontalOverflowAndNoSeriousAxeViolations(
        string cultureName,
        int width,
        int height)
    {
        var context = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            ReducedMotion = ReducedMotion.Reduce
        });
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = CookieRequestCultureProvider.DefaultCookieName,
                Value = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureName, cultureName)),
                Domain = "cardpicker.test",
                Path = "/"
            }
        });
        var page = await context.NewPageAsync();

        foreach (var path in new[] { "/?tags=便當&priceRange=Low", "/Cards?tags=便當&priceRange=Low", "/Cards/Create" })
        {
            await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}{path}");
            var hasNoHorizontalOverflow = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth <= document.documentElement.clientWidth");
            Assert.True(hasNoHorizontalOverflow, $"{path} should not overflow horizontally at {width}x{height} for {cultureName}.");
        }

        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/?tags=便當&priceRange=Low");
        await page.GetByText(cultureName == "en-US" ? "Dark mode" : "暗黑模式").ClickAsync();
        await ThemeModeBrowserTests.WaitForThemeAsync(page, "dark", "dark", 2000);
        Assert.Equal("便當", await page.Locator("#filter-tags").InputValueAsync());

        var results = await page.RunAxe(new AxeRunOptions
        {
            Rules = new Dictionary<string, RuleOptions>
            {
                ["color-contrast"] = new RuleOptions { Enabled = false }
            }
        });
        Assert.DoesNotContain(results.Violations, violation => violation.Impact is "serious" or "critical");
    }
}
