using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Browser;

public sealed class ThemeModeResponsiveTests : IClassFixture<ThemeBrowserFixture>
{
    private readonly ThemeBrowserFixture _fixture;

    public ThemeModeResponsiveTests(ThemeBrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(390, 844, "light")]
    [InlineData(390, 844, "dark")]
    [InlineData(390, 844, "system")]
    [InlineData(768, 1024, "dark")]
    [InlineData(1366, 768, "light")]
    public async Task MainSurfaces_HaveNoHorizontalOverflowVisibleFocusAndNoSeriousAxeViolations(
        int width,
        int height,
        string mode)
    {
        var context = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            ColorScheme = ColorScheme.Dark
        });
        await context.AddInitScriptAsync($"localStorage.setItem('cardpicker.theme.mode', '{mode}');");
        var page = await context.NewPageAsync();

        foreach (var path in CardPicker2.IntegrationTests.Pages.ThemeControlledSurfaceData.MainPagePaths)
        {
            await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}{path}");
            await ThemeModeBrowserTests.WaitForThemeAsync(page, mode, null, 1000);

            var hasNoHorizontalOverflow = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth <= document.documentElement.clientWidth");
            Assert.True(hasNoHorizontalOverflow, $"{path} should not overflow horizontally at {width}x{height} in {mode} mode.");
        }

        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");
        await page.Keyboard.PressAsync("Tab");
        var focusOutlineVisible = await page.EvaluateAsync<bool>(
            @"() => {
                const active = document.activeElement;
                if (!active) return false;
                const style = window.getComputedStyle(active);
                return style.outlineStyle !== 'none' || style.boxShadow !== 'none';
            }");
        Assert.True(focusOutlineVisible, "Keyboard focus should be visibly styled.");

        var contrastFailures = await page.EvaluateAsync<string[]>(
            @"() => {
                const selectors = [
                    'label[for=""theme-mode-light""]',
                    'label[for=""theme-mode-dark""]',
                    'label[for=""theme-mode-system""]',
                    'label[for=""meal-Breakfast""]',
                    'label[for=""meal-Lunch""]',
                    'label[for=""meal-Dinner""]',
                    '.nav-link[href=""/""]',
                    'a[href$=""Cards""]',
                    '.nav-link[href$=""Privacy""]'
                ];
                const parseRgb = value => {
                    const match = value.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*([0-9.]+))?\)/);
                    if (!match) return null;
                    return {
                        r: Number(match[1]) / 255,
                        g: Number(match[2]) / 255,
                        b: Number(match[3]) / 255,
                        a: match[4] === undefined ? 1 : Number(match[4])
                    };
                };
                const channel = value => value <= 0.03928 ? value / 12.92 : Math.pow((value + 0.055) / 1.055, 2.4);
                const luminance = color => 0.2126 * channel(color.r) + 0.7152 * channel(color.g) + 0.0722 * channel(color.b);
                const ratio = (foreground, background) => {
                    const high = Math.max(luminance(foreground), luminance(background));
                    const low = Math.min(luminance(foreground), luminance(background));
                    return (high + 0.05) / (low + 0.05);
                };
                const effectiveBackground = element => {
                    let current = element;
                    while (current) {
                        const color = parseRgb(window.getComputedStyle(current).backgroundColor);
                        if (color && color.a > 0) return color;
                        current = current.parentElement;
                    }
                    return parseRgb(window.getComputedStyle(document.body).backgroundColor);
                };
                return selectors.map(selector => {
                    const element = document.querySelector(selector);
                    if (!element) return null;
                    const foreground = parseRgb(window.getComputedStyle(element).color);
                    const background = effectiveBackground(element);
                    const score = foreground && background ? ratio(foreground, background) : 0;
                    return score >= 4.5 ? null : `${selector}: ${score}; fg=${window.getComputedStyle(element).color}; bg=${window.getComputedStyle(element).backgroundColor}; eff=${background ? `${background.r},${background.g},${background.b}` : 'none'}`;
                }).filter(Boolean);
            }");
        Assert.Empty(contrastFailures);

        var results = await page.RunAxe(new AxeRunOptions
        {
            Rules = new Dictionary<string, RuleOptions>
            {
                ["color-contrast"] = new RuleOptions { Enabled = false }
            }
        });
        var seriousViolationIds = results.Violations
            .Where(violation => violation.Impact is "serious" or "critical")
            .Select(violation => $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => string.Join(" ", node.Target)))}")
            .ToArray();
        Assert.True(seriousViolationIds.Length == 0, string.Join("; ", seriousViolationIds));
    }
}
