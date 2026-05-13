using System.Diagnostics;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Performance;

[Collection(NonParallelPerformanceCollection.Name)]
public sealed class DrawModePerformanceTests : IDisposable
{
    private static readonly TimeSpan Budget = TimeSpan.FromMilliseconds(200);
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task HomeGet_P95CompletesWithinLocalJsonBudget()
    {
        var client = _factory.CreateClient();
        await client.GetAsync("/");

        var durations = new List<TimeSpan>();
        for (var i = 0; i < 12; i++)
        {
            durations.Add(await MeasureAsync(async () =>
            {
                var response = await client.GetAsync("/");
                response.EnsureSuccessStatusCode();
            }));
        }

        AssertP95WithinBudget(durations, nameof(HomeGet_P95CompletesWithinLocalJsonBudget));
    }

    [Fact]
    public async Task DrawPost_P95CompletesWithinLocalJsonBudget()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetAntiForgeryTokenAsync(client);

        var durations = new List<TimeSpan>();
        for (var i = 0; i < 12; i++)
        {
            durations.Add(await MeasureAsync(async () =>
            {
                var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["DrawMode"] = nameof(DrawMode.Random),
                    ["CoinInserted"] = "true",
                    ["DrawOperationId"] = Guid.NewGuid().ToString()
                }));
                response.EnsureSuccessStatusCode();
            }));
        }

        AssertP95WithinBudget(durations, nameof(DrawPost_P95CompletesWithinLocalJsonBudget));
    }

    [Fact]
    public async Task StatisticsProjection_P95CompletesWithinLocalJsonBudget()
    {
        await _factory.WriteLibraryDocumentAsync(CreateDocumentWithHistory(60));
        var client = _factory.CreateClient();
        await client.GetAsync("/");

        var durations = new List<TimeSpan>();
        for (var i = 0; i < 12; i++)
        {
            durations.Add(await MeasureAsync(async () =>
            {
                var response = await client.GetAsync("/");
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();
                Assert.Contains("60", html, StringComparison.Ordinal);
            }));
        }

        AssertP95WithinBudget(durations, nameof(StatisticsProjection_P95CompletesWithinLocalJsonBudget));
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

        Assert.True(p95 < Budget, $"{scenario} p95 was {p95.TotalMilliseconds:0.##}ms; budget is {Budget.TotalMilliseconds:0.##}ms.");
    }

    private static CardLibraryDocument CreateDocumentWithHistory(int historyCount)
    {
        var breakfastId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var lunchId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var dinnerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var cards = new[]
        {
            CreateCard(breakfastId, MealType.Breakfast, "早餐效能", "Breakfast Performance"),
            CreateCard(lunchId, MealType.Lunch, "午餐效能", "Lunch Performance"),
            CreateCard(dinnerId, MealType.Dinner, "晚餐效能", "Dinner Performance")
        };
        var cardIds = cards.Select(card => card.Id).ToArray();
        var history = Enumerable.Range(0, historyCount)
            .Select(index => new DrawHistoryRecord
            {
                Id = Guid.NewGuid(),
                OperationId = Guid.NewGuid(),
                DrawMode = index % 2 == 0 ? DrawMode.Random : DrawMode.Normal,
                CardId = cardIds[index % cardIds.Length],
                MealTypeAtDraw = cards[index % cards.Length].MealType,
                SucceededAtUtc = DateTimeOffset.UtcNow.AddSeconds(-index)
            })
            .ToArray();

        return new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = cards,
            DrawHistory = history
        };
    }

    private static MealCard CreateCard(Guid id, MealType mealType, string zhTwName, string enUsName)
    {
        return new MealCard(
            id,
            mealType,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new(zhTwName, $"{zhTwName} 描述"),
                [SupportedLanguage.EnUs.CultureName] = new(enUsName, $"{enUsName} description")
            });
    }
}
