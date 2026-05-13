using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Browser;

public sealed class DrawModeResponsiveAccessibilityTests : IClassFixture<ThemeBrowserFixture>
{
    private readonly ThemeBrowserFixture _fixture;

    public DrawModeResponsiveAccessibilityTests(ThemeBrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(1366, 768)]
    public async Task HomeDrawModeSurface_HasNoHorizontalOverflowAndKeyboardFocus(int width, int height)
    {
        var context = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            ReducedMotion = ReducedMotion.Reduce
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");

        var hasNoHorizontalOverflow = await page.EvaluateAsync<bool>(
            "document.documentElement.scrollWidth <= document.documentElement.clientWidth");
        Assert.True(hasNoHorizontalOverflow, $"Home page should not overflow horizontally at {width}x{height}.");

        await page.Keyboard.PressAsync("Tab");
        var focusVisible = await page.EvaluateAsync<bool>(
            @"() => {
                const active = document.activeElement;
                if (!active) return false;
                const style = window.getComputedStyle(active);
                return style.outlineStyle !== 'none' || style.boxShadow !== 'none';
            }");
        Assert.True(focusVisible);
    }
}
