using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Browser;

public sealed class ThemeModeStorageSyncTests : IClassFixture<ThemeBrowserFixture>
{
    private readonly ThemeBrowserFixture _fixture;

    public ThemeModeStorageSyncTests(ThemeBrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StorageEvent_UpdatesOtherTabWithinTwoSecondsWithoutRenderingSelector()
    {
        var context = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ColorScheme = ColorScheme.Light
        });
        var homePage = await context.NewPageAsync();
        var otherPage = await context.NewPageAsync();

        await homePage.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");
        await otherPage.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/Privacy");

        await homePage.GetByText("暗黑模式").ClickAsync();

        await ThemeModeBrowserTests.WaitForThemeAsync(otherPage, "dark", "dark", 2000);
        Assert.Equal(0, await otherPage.Locator("[data-theme-mode-selector]").CountAsync());
    }

    [Fact]
    public async Task StorageEventFailure_LogsOnlySafeSyncWarningName()
    {
        var context = await _fixture.CreateContextAsync("chromium");
        var otherPage = await context.NewPageAsync();
        var warnings = new List<string>();
        otherPage.Console += (_, message) => warnings.Add(message.Text);

        await otherPage.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/Privacy");
        await otherPage.EvaluateAsync(
            @"() => {
                const event = new StorageEvent('storage', { key: 'cardpicker.theme.mode', newValue: 'dark' });
                Object.defineProperty(event, 'key', { get: () => { throw new Error('sync failure'); } });
                window.dispatchEvent(event);
            }");

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline && !warnings.Contains("CardPickerThemeSyncFailed"))
        {
            await Task.Delay(50);
        }

        Assert.Contains("CardPickerThemeSyncFailed", warnings);
    }
}
