using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Performance;

[Collection(NonParallelPerformanceCollection.Name)]
public sealed class CardPreferencePerformanceTests : IDisposable
{
    private static readonly TimeSpan Budget = TimeSpan.FromMilliseconds(300);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory =
        DrawFeatureWebApplicationFactory.CreateWithDeterministicRandomizer(0);

    [Fact]
    public async Task PreferenceSurfaces_P95CompletesWithinLocalJsonBudget()
    {
        var document = CreateLargeDocument();
        var targetCardId = document.Cards.First(card => card.Status == CardStatus.Active && !card.Preferences.IsExcludedFromDraw).Id;
        await _factory.WriteLibraryDocumentAsync(document, JsonOptions);
        var client = _factory.CreateClient();
        var token = await _factory.GetAntiForgeryTokenAsync(client);

        (await client.GetAsync("/")).EnsureSuccessStatusCode();
        (await client.GetAsync("/Cards?favoriteFilter=FavoritesOnly&drawEligibilityFilter=DrawableOnly")).EnsureSuccessStatusCode();

        var homeDurations = new List<TimeSpan>();
        var drawDurations = new List<TimeSpan>();
        var preferenceDurations = new List<TimeSpan>();
        var searchDurations = new List<TimeSpan>();
        for (var i = 0; i < 8; i++)
        {
            homeDurations.Add(await MeasureAsync(async () =>
            {
                var home = await client.GetAsync("/");
                home.EnsureSuccessStatusCode();
            }));
            drawDurations.Add(await MeasureAsync(async () =>
            {
                var draw = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(
                    DrawFeatureWebApplicationFactory.CreateFilteredDrawPayload(
                        token,
                        drawMode: nameof(DrawMode.Random),
                        mealType: null,
                        drawOperationId: Guid.NewGuid(),
                        tags: new[] { "偏好效能" })));
                draw.EnsureSuccessStatusCode();
            }));
            preferenceDurations.Add(await MeasureAsync(async () =>
            {
                var preference = await client.PostAsync("/?handler=Preference", new FormUrlEncodedContent(
                    DrawFeatureWebApplicationFactory.CreatePreferencePayload(
                        token,
                        targetCardId,
                        targetIsFavorite: true,
                        resultCardId: targetCardId)));
                preference.EnsureSuccessStatusCode();
            }));
            searchDurations.Add(await MeasureAsync(async () =>
            {
                var cards = await client.GetAsync("/Cards?favoriteFilter=FavoritesOnly&drawEligibilityFilter=DrawableOnly&tags=%E5%81%8F%E5%A5%BD%E6%95%88%E8%83%BD");
                cards.EnsureSuccessStatusCode();
            }));
        }

        AssertP95WithinBudget(homeDurations, "preference-aware home/statistics GET");
        AssertP95WithinBudget(drawDurations, "preference-aware draw POST");
        AssertP95WithinBudget(preferenceDurations, "preference update POST");
        AssertP95WithinBudget(searchDurations, "preference-filtered card search GET");
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
        var cards = Enumerable.Range(0, 120)
            .Select(CreateCard)
            .ToArray();
        var history = Enumerable.Range(0, 600)
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
                [SupportedLanguage.ZhTw.CultureName] = new($"偏好效能餐點 {index}", $"偏好效能描述 {index}"),
                [SupportedLanguage.EnUs.CultureName] = new($"Preference Performance Meal {index}", $"Preference performance description {index}")
            },
            CardStatus.Active,
            deletedAtUtc: null,
            decisionMetadata: new MealCardDecisionMetadata
            {
                Tags = index % 2 == 0 ? new[] { "偏好效能", "外帶" } : new[] { "一般效能" },
                PriceRange = index % 2 == 0 ? PriceRange.Low : PriceRange.Medium,
                PreparationTimeRange = index % 2 == 0 ? PreparationTimeRange.Quick : PreparationTimeRange.Standard,
                DietaryPreferences = index % 2 == 0 ? new[] { DietaryPreference.TakeoutFriendly } : new[] { DietaryPreference.Light },
                SpiceLevel = SpiceLevel.None
            },
            preferences: new CardPreferenceState
            {
                IsFavorite = index % 5 == 0,
                IsExcludedFromDraw = index % 10 == 0
            });
    }
}
