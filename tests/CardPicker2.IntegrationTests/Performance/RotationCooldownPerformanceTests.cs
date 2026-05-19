using System.Diagnostics;

using CardPicker2.IntegrationTests.Browser;
using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

using Microsoft.AspNetCore.Localization;
using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Performance;

[Collection(NonParallelPerformanceCollection.Name)]
public sealed class RotationCooldownPerformanceTests : IDisposable
{
    private static readonly TimeSpan HandlerBudget = TimeSpan.FromMilliseconds(200);
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task RotationCooldownSurfaces_P95CompletesWithinLocalJsonBudget()
    {
        await _factory.WriteLibraryDocumentAsync(CreateLargeDocument());
        var client = _factory.CreateClient();
        var token = await _factory.GetAntiForgeryTokenAsync(client);
        (await client.GetAsync("/?tags=rotation&priceRange=Low&avoidRecentRepeats=true&recentDrawCount=5")).EnsureSuccessStatusCode();
        (await client.GetAsync("/Cards?tags=rotation&priceRange=Low&dietaryPreferences=TakeoutFriendly")).EnsureSuccessStatusCode();

        var homeDurations = new List<TimeSpan>();
        var searchDurations = new List<TimeSpan>();
        var drawDurations = new List<TimeSpan>();
        var statisticsDurations = new List<TimeSpan>();
        for (var i = 0; i < 12; i++)
        {
            homeDurations.Add(await MeasureAsync(async () =>
            {
                var home = await client.GetAsync("/?tags=rotation&priceRange=Low&avoidRecentRepeats=true&recentDrawCount=5");
                home.EnsureSuccessStatusCode();
            }));
            searchDurations.Add(await MeasureAsync(async () =>
            {
                var cards = await client.GetAsync("/Cards?tags=rotation&priceRange=Low&dietaryPreferences=TakeoutFriendly");
                cards.EnsureSuccessStatusCode();
            }));
            drawDurations.Add(await MeasureAsync(async () =>
            {
                var draw = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["DrawMode"] = nameof(DrawMode.Random),
                    ["CoinInserted"] = "true",
                    ["DrawOperationId"] = Guid.NewGuid().ToString(),
                    ["Tags"] = "rotation",
                    ["PriceRange"] = nameof(PriceRange.Low),
                    ["DietaryPreferences"] = nameof(DietaryPreference.TakeoutFriendly),
                    ["AvoidRecentRepeats"] = "true",
                    ["RecentDrawCount"] = "5"
                }));
                draw.EnsureSuccessStatusCode();
            }));
            statisticsDurations.Add(await MeasureAsync(async () =>
            {
                var home = await client.GetAsync("/");
                home.EnsureSuccessStatusCode();
                var html = await home.Content.ReadAsStringAsync();
                Assert.Contains("Rotation meal", html, StringComparison.Ordinal);
            }));
        }

        AssertP95WithinBudget(homeDurations, "rotation home GET");
        AssertP95WithinBudget(searchDurations, "rotation card search GET");
        AssertP95WithinBudget(drawDurations, "rotation filtered draw POST");
        AssertP95WithinBudget(statisticsDurations, "rotation statistics projection");
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static async Task<TimeSpan> MeasureAsync(Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private static void AssertP95WithinBudget(IReadOnlyList<TimeSpan> durations, string scenario)
    {
        var ordered = durations.OrderBy(duration => duration).ToArray();
        var index = (int)Math.Ceiling(ordered.Length * 0.95) - 1;
        var p95 = ordered[Math.Clamp(index, 0, ordered.Length - 1)];

        Assert.True(p95 < HandlerBudget, $"{scenario} p95 was {p95.TotalMilliseconds:0.##}ms; budget is {HandlerBudget.TotalMilliseconds:0.##}ms.");
    }

    private static CardLibraryDocument CreateLargeDocument()
    {
        var cards = Enumerable.Range(0, 150)
            .Select(CreateCard)
            .ToArray();
        var snapshot = RotationSnapshot.Create(new RotationCooldownSettings(true, 3), cards.Length, 1);
        var history = Enumerable.Range(0, 1_000)
            .Select(index => new DrawHistoryRecord
            {
                Id = Guid.NewGuid(),
                OperationId = Guid.NewGuid(),
                DrawMode = index % 2 == 0 ? DrawMode.Random : DrawMode.Normal,
                CardId = cards[index % cards.Length].Id,
                MealTypeAtDraw = cards[index % cards.Length].MealType,
                SucceededAtUtc = DateTimeOffset.UtcNow.AddMinutes(-index),
                RotationSnapshot = snapshot
            })
            .ToArray();

        return new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = cards,
            DrawHistory = history
        };
    }

    private static MealCard CreateCard(int index)
    {
        var mealType = (MealType)(index % 3);
        return new MealCard(
            Guid.NewGuid(),
            mealType,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new($"Rotation meal {index}", $"Rotation description {index}"),
                [SupportedLanguage.EnUs.CultureName] = new($"Rotation meal {index}", $"Rotation description {index}")
            },
            CardStatus.Active,
            deletedAtUtc: null,
            new MealCardDecisionMetadata
            {
                Tags = index % 2 == 0 ? new[] { "rotation", "quick" } : new[] { "slow" },
                PriceRange = index % 2 == 0 ? PriceRange.Low : PriceRange.Medium,
                PreparationTimeRange = index % 2 == 0 ? PreparationTimeRange.Quick : PreparationTimeRange.Standard,
                DietaryPreferences = index % 2 == 0 ? new[] { DietaryPreference.TakeoutFriendly } : new[] { DietaryPreference.HeavyFlavor },
                SpiceLevel = index % 4 == 0 ? SpiceLevel.Mild : SpiceLevel.None
            });
    }
}

public sealed class RotationCooldownWebVitalsTests : IClassFixture<ThemeBrowserFixture>
{
    private readonly ThemeBrowserFixture _fixture;

    public RotationCooldownWebVitalsTests(ThemeBrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RotationCooldownHome_WebVitalsStayWithinBudget()
    {
        var context = await _fixture.CreateContextAsync("chromium", new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
            ReducedMotion = ReducedMotion.Reduce
        });
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = CookieRequestCultureProvider.DefaultCookieName,
                Value = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en-US", "en-US")),
                Domain = "cardpicker.test",
                Path = "/"
            }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(
            $"{ThemeBrowserFixture.BaseUrl}/?avoidRecentRepeats=true&recentDrawCount=3",
            new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await page.Locator("[data-rotation-cooldown-panel]").WaitForAsync();

        var metrics = await page.EvaluateAsync<double[]>(
            """
            async () => {
                const navigation = performance.getEntriesByType('navigation')[0];
                const fcpEntry = performance.getEntriesByName('first-contentful-paint')[0];
                let lcp = 0;
                await new Promise((resolve) => {
                    let observer;
                    try {
                        observer = new PerformanceObserver((list) => {
                            const entries = list.getEntries();
                            if (entries.length > 0) {
                                lcp = entries[entries.length - 1].startTime;
                            }
                        });
                        observer.observe({ type: 'largest-contentful-paint', buffered: true });
                    } catch {
                    }

                    requestAnimationFrame(() => setTimeout(() => {
                        if (observer) {
                            observer.disconnect();
                        }
                        resolve();
                    }, 50));
                });

                return [
                    navigation.domContentLoadedEventEnd,
                    fcpEntry?.startTime || navigation.responseEnd,
                    lcp || navigation.loadEventEnd || navigation.domContentLoadedEventEnd
                ];
            }
            """);

        Assert.True(metrics[0] < 1_000, $"Main content updated in {metrics[0]:0.##}ms.");
        Assert.True(metrics[1] < 1_500, $"FCP was {metrics[1]:0.##}ms.");
        Assert.True(metrics[2] < 2_500, $"LCP was {metrics[2]:0.##}ms.");
    }
}
