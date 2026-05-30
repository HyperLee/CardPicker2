using System.Text.Json;

using CardPicker2.IntegrationTests.Infrastructure;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

using Microsoft.AspNetCore.Localization;
using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Browser;

[Collection(NonParallelBrowserCollection.Name)]
public sealed class CardPreferenceLibraryResponsiveAccessibilityTests : IClassFixture<ThemeBrowserFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ThemeBrowserFixture _fixture;

    public CardPreferenceLibraryResponsiveAccessibilityTests(ThemeBrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("zh-TW", "/Cards?favoriteFilter=FavoritesOnly&drawEligibilityFilter=DrawableOnly", 390, 844)]
    [InlineData("en-US", "/Cards/22222222-2222-2222-2222-222222222223", 768, 1024)]
    [InlineData("zh-TW", "/Cards?drawEligibilityFilter=ExcludedOnly", 1366, 768)]
    public async Task CardPreferenceLibraryAndDetailsControls_AreResponsiveAccessibleAndOperable(
        string cultureName,
        string path,
        int width,
        int height)
    {
        await WritePreferenceDocumentAsync();
        var context = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            ReducedMotion = ReducedMotion.Reduce,
            IsMobile = width <= 390,
            HasTouch = width <= 390
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

        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}{path}");

        Assert.True(await page.Locator("[data-card-preference-controls]").First.IsVisibleAsync());
        Assert.True(await page.Locator("[data-preference-badge]").First.IsVisibleAsync());
        Assert.True(await page.Locator("[data-preference-submit]").First.IsEnabledAsync());
        Assert.True(await HasNoHorizontalOverflowAsync(page), $"Preference UI should not overflow at {width}x{height} for {cultureName}.");

        var firstPreferenceButton = page.Locator("[data-preference-submit]").First;
        var buttonBox = await firstPreferenceButton.BoundingBoxAsync();
        Assert.NotNull(buttonBox);
        Assert.True(buttonBox.Width > 0);
        Assert.True(buttonBox.Height > 0);

        await firstPreferenceButton.FocusAsync();
        Assert.True(await HasVisibleFocusAsync(page));

        var results = await page.RunAxe(new AxeRunOptions
        {
            Rules = new Dictionary<string, RuleOptions>
            {
                ["color-contrast"] = new RuleOptions { Enabled = false }
            }
        });
        Assert.DoesNotContain(results.Violations, violation => violation.Impact is "serious" or "critical");
    }

    private Task WritePreferenceDocumentAsync()
    {
        var document = MetadataFilterTestData.PreferenceAwareSchemaV5Document(
            favoriteCardId: MetadataFilterTestData.VegetarianLunchCardId,
            excludedCardId: MetadataFilterTestData.MissingMetadataDinnerCardId);
        return File.WriteAllTextAsync(_fixture.CardLibraryFilePath, JsonSerializer.Serialize(document, JsonOptions));
    }

    private static Task<bool> HasNoHorizontalOverflowAsync(IPage page)
    {
        return page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= document.documentElement.clientWidth");
    }

    private static Task<bool> HasVisibleFocusAsync(IPage page)
    {
        return page.EvaluateAsync<bool>(
            @"() => {
                const active = document.activeElement;
                if (!active) return false;
                const style = window.getComputedStyle(active);
                return style.outlineStyle !== 'none' || style.boxShadow !== 'none';
            }");
    }
}
