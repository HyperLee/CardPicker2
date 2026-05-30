using Microsoft.AspNetCore.Localization;
using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Browser;

[Collection(NonParallelBrowserCollection.Name)]
public sealed class RotationCooldownResponsiveAccessibilityTests : IClassFixture<ThemeBrowserFixture>
{
    private readonly ThemeBrowserFixture _fixture;

    public RotationCooldownResponsiveAccessibilityTests(ThemeBrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("zh-TW", 390, 844)]
    [InlineData("en-US", 1366, 768)]
    public async Task RotationCooldownHomeSurface_HasNoHorizontalOverflowWithReducedMotion(
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

        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/?avoidRecentRepeats=true&recentDrawCount=3");

        Assert.True(await page.Locator("[data-rotation-cooldown-panel]").IsVisibleAsync());
        var hasNoHorizontalOverflow = await page.EvaluateAsync<bool>(
            "document.documentElement.scrollWidth <= document.documentElement.clientWidth");
        Assert.True(hasNoHorizontalOverflow, $"Rotation cooldown UI should not overflow at {width}x{height} for {cultureName}.");
    }
}
