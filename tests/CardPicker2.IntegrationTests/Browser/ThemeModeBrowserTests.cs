using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Browser;

public sealed class ThemeModeBrowserTests : IClassFixture<ThemeBrowserFixture>
{
    private readonly ThemeBrowserFixture _fixture;

    public ThemeModeBrowserTests(ThemeBrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HomeThemeSelector_UpdatesModeWithMouseKeyboardAndMobileTouch()
    {
        var page = await _fixture.CreateDesktopPageAsync();
        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");

        await page.GetByText("亮色模式").ClickAsync();
        await WaitForThemeAsync(page, "light", "light", 1000);

        await page.Locator("input[name=\"theme-mode\"][value=\"dark\"]").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await WaitForThemeAsync(page, "dark", "dark", 1000);

        var mobilePage = await _fixture.CreateMobileTouchPageAsync();
        await mobilePage.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");
        await mobilePage.GetByText("跟隨系統").TapAsync();
        await WaitForThemeAsync(mobilePage, "system", null, 1000);
    }

    [Fact]
    public async Task StoredThemeMode_AppliesBeforeSubsequentNavigationAndSelectorStaysHomeOnly()
    {
        var page = await _fixture.CreateDesktopPageAsync();
        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");

        await page.GetByText("暗黑模式").ClickAsync();
        await WaitForThemeAsync(page, "dark", "dark", 1000);

        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/Privacy");

        await WaitForThemeAsync(page, "dark", "dark", 1000);
        Assert.Equal("dark", await page.EvaluateAsync<string>("localStorage.getItem('cardpicker.theme.mode')"));
        Assert.Equal(0, await page.Locator("[data-theme-mode-selector]").CountAsync());
    }

    [Fact]
    public async Task HeadBootstrapScript_AppliesStoredDarkAndSystemBeforePageScriptsRun()
    {
        var darkContext = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ColorScheme = ColorScheme.Light
        });
        await darkContext.AddInitScriptAsync("localStorage.setItem('cardpicker.theme.mode', 'dark');");
        var darkPage = await darkContext.NewPageAsync();

        await darkPage.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");

        await WaitForThemeAsync(darkPage, "dark", "dark", 1000);

        var systemContext = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ColorScheme = ColorScheme.Dark
        });
        await systemContext.AddInitScriptAsync("localStorage.setItem('cardpicker.theme.mode', 'system');");
        var systemPage = await systemContext.NewPageAsync();

        await systemPage.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");

        await WaitForThemeAsync(systemPage, "system", "dark", 1000);
    }

    [Theory]
    [InlineData("light", "light")]
    [InlineData("dark", "dark")]
    public async Task SystemMode_DerivesEffectiveThemeFromBrowserColorScheme(string colorScheme, string expectedTheme)
    {
        var context = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ColorScheme = colorScheme == "dark" ? ColorScheme.Dark : ColorScheme.Light
        });
        await context.AddInitScriptAsync("localStorage.setItem('cardpicker.theme.mode', 'system');");
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");

        await WaitForThemeAsync(page, "system", expectedTheme, 1000);
    }

    [Fact]
    public async Task SystemPreferenceChanges_UpdateOnlyWhenCurrentModeIsSystem()
    {
        var context = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ColorScheme = ColorScheme.Light
        });
        await context.AddInitScriptAsync("localStorage.setItem('cardpicker.theme.mode', 'system');");
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");
        await WaitForThemeAsync(page, "system", "light", 1000);

        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
        await WaitForThemeAsync(page, "system", "dark", 2000);

        await page.GetByText("亮色模式").ClickAsync();
        await WaitForThemeAsync(page, "light", "light", 1000);

        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        await WaitForThemeAsync(page, "light", "light", 1000);
    }

    internal static async Task WaitForThemeAsync(IPage page, string mode, string? effectiveTheme, float timeout)
    {
        await page.WaitForFunctionAsync(
            @"expected => {
                const html = document.documentElement;
                return html.getAttribute('data-theme-mode') === expected.mode
                    && (expected.effectiveTheme === null
                        || html.getAttribute('data-bs-theme') === expected.effectiveTheme);
            }",
            new { mode, effectiveTheme },
            new PageWaitForFunctionOptions { Timeout = timeout });
    }
}
