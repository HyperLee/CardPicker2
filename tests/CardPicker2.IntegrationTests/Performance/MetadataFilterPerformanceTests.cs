using System.Diagnostics;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Performance;

[Collection(NonParallelPerformanceCollection.Name)]
public sealed class MetadataFilterPerformanceTests : IDisposable
{
    private static readonly TimeSpan Budget = TimeSpan.FromMilliseconds(200);
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task MetadataFilterSurfaces_P95CompletesWithinLocalJsonBudget()
    {
        await _factory.WriteLibraryDocumentAsync(CreateLargeDocument());
        var client = _factory.CreateClient();
        var token = await _factory.GetAntiForgeryTokenAsync(client);
        (await client.GetAsync("/?tags=便當&priceRange=Low")).EnsureSuccessStatusCode();
        (await client.GetAsync("/Cards?tags=便當&priceRange=Low&dietaryPreferences=TakeoutFriendly")).EnsureSuccessStatusCode();

        var homeDurations = new List<TimeSpan>();
        var searchDurations = new List<TimeSpan>();
        var drawDurations = new List<TimeSpan>();
        for (var i = 0; i < 12; i++)
        {
            homeDurations.Add(await MeasureAsync(async () =>
            {
                var home = await client.GetAsync("/?tags=便當&priceRange=Low");
                home.EnsureSuccessStatusCode();
            }));
            searchDurations.Add(await MeasureAsync(async () =>
            {
                var cards = await client.GetAsync("/Cards?tags=便當&priceRange=Low&dietaryPreferences=TakeoutFriendly");
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
                    ["Tags"] = "便當",
                    ["PriceRange"] = nameof(PriceRange.Low),
                    ["DietaryPreferences"] = nameof(DietaryPreference.TakeoutFriendly)
                }));
                draw.EnsureSuccessStatusCode();
            }));
        }

        AssertP95WithinBudget(homeDurations, "metadata home GET");
        AssertP95WithinBudget(searchDurations, "metadata card search GET");
        AssertP95WithinBudget(drawDurations, "metadata draw POST");
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

    private static CardLibraryDocument CreateLargeDocument()
    {
        var cards = Enumerable.Range(0, 150)
            .Select(index => CreateCard(index))
            .ToArray();
        var history = Enumerable.Range(0, 1_000)
            .Select(index => new DrawHistoryRecord
            {
                Id = Guid.NewGuid(),
                OperationId = Guid.NewGuid(),
                DrawMode = index % 2 == 0 ? DrawMode.Random : DrawMode.Normal,
                CardId = cards[index % cards.Length].Id,
                MealTypeAtDraw = cards[index % cards.Length].MealType,
                SucceededAtUtc = DateTimeOffset.UtcNow.AddMinutes(-index)
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
                [SupportedLanguage.ZhTw.CultureName] = new($"效能餐點 {index}", $"效能描述 {index}"),
                [SupportedLanguage.EnUs.CultureName] = new($"Performance Meal {index}", $"Performance description {index}")
            },
            CardStatus.Active,
            deletedAtUtc: null,
            new MealCardDecisionMetadata
            {
                Tags = index % 2 == 0 ? new[] { "便當", "外帶" } : new[] { "麵食" },
                PriceRange = index % 2 == 0 ? PriceRange.Low : PriceRange.Medium,
                PreparationTimeRange = index % 2 == 0 ? PreparationTimeRange.Quick : PreparationTimeRange.Standard,
                DietaryPreferences = index % 2 == 0 ? new[] { DietaryPreference.TakeoutFriendly } : new[] { DietaryPreference.HeavyFlavor },
                SpiceLevel = index % 4 == 0 ? SpiceLevel.Mild : SpiceLevel.None
            });
    }
}
