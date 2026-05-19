using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class RotationCooldownStatisticsTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task PostDraw_WhenRotationEmptiesPool_DoesNotChangeSuccessfulDrawCount()
    {
        await _factory.WriteLibraryDocumentAsync(DocumentWithAllBentoCandidatesRecent(), JsonOptions);
        var before = await ReadHistoryCountAsync();
        var client = _factory.CreateClient();
        using var content = await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch),
            tags: new[] { "便當" },
            avoidRecentRepeats: true,
            recentDrawCount: "3");

        var response = await client.PostAsync("/?handler=Draw", content);

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        RotationCooldownHtmlAssertions.AssertRotationEmptyAlert(html, "避免最近重複", "降低");
        Assert.Equal(before, await ReadHistoryCountAsync());
    }

    [Fact]
    public async Task PostDraw_WithInvalidRecentDrawCount_DoesNotChangeSuccessfulDrawCount()
    {
        await _factory.WriteLibraryDocumentAsync(DocumentWithAllBentoCandidatesRecent(), JsonOptions);
        var before = await ReadHistoryCountAsync();
        var client = _factory.CreateClient();
        using var content = await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Random),
            mealType: null,
            avoidRecentRepeats: true,
            recentDrawCount: "11");

        var response = await client.PostAsync("/?handler=Draw", content);

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        RotationCooldownHtmlAssertions.AssertLocalizedValidation(html, "0 到 10");
        Assert.Equal(before, await ReadHistoryCountAsync());
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<int> ReadHistoryCountAsync()
    {
        var document = JsonSerializer.Deserialize<CardLibraryDocument>(
            await File.ReadAllTextAsync(_factory.LibraryFilePath),
            JsonOptions);
        Assert.NotNull(document);
        return document.DrawHistory.Count;
    }

    private static object DocumentWithAllBentoCandidatesRecent()
    {
        var lowPriceLunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var vegetarianLunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222223");
        var timestamp = new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero);

        return new
        {
            schemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            cards = new[]
            {
                Card(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Breakfast", "鮪魚蛋餅", "Tuna Egg Crepe", null),
                Card(lowPriceLunchCardId, "Lunch", "滷肉飯便當", "Braised Pork Rice Bento", Metadata(new[] { "便當" })),
                Card(vegetarianLunchCardId, "Lunch", "菇菇蔬食便當", "Mushroom Vegetable Bento", Metadata(new[] { "蔬食", "便當" })),
                Card(Guid.Parse("33333333-3333-3333-3333-333333333331"), "Dinner", "番茄燉飯", "Tomato Risotto", null)
            },
            drawHistory = new[]
            {
                History(Guid.Parse("77777777-7777-7777-7777-777777777775"), Guid.Parse("88888888-8888-8888-8888-888888888885"), lowPriceLunchCardId, timestamp),
                History(Guid.Parse("77777777-7777-7777-7777-777777777776"), Guid.Parse("88888888-8888-8888-8888-888888888886"), vegetarianLunchCardId, timestamp.AddMinutes(1))
            }
        };
    }

    private static object Card(Guid id, string mealType, string zhTwName, string enUsName, object? metadata)
    {
        return new
        {
            id,
            mealType,
            status = "Active",
            deletedAtUtc = (DateTimeOffset?)null,
            localizations = new Dictionary<string, object>
            {
                [SupportedLanguage.ZhTw.CultureName] = new { name = zhTwName, description = $"{zhTwName} 描述" },
                [SupportedLanguage.EnUs.CultureName] = new { name = enUsName, description = $"{enUsName} description" }
            },
            decisionMetadata = metadata
        };
    }

    private static object Metadata(IReadOnlyList<string> tags)
    {
        return new
        {
            tags,
            priceRange = "Low",
            preparationTimeRange = "Quick",
            dietaryPreferences = Array.Empty<string>(),
            spiceLevel = "None"
        };
    }

    private static object History(Guid id, Guid operationId, Guid cardId, DateTimeOffset succeededAtUtc)
    {
        return new
        {
            id,
            operationId,
            drawMode = "Normal",
            cardId,
            mealTypeAtDraw = "Lunch",
            succeededAtUtc,
            rotationSnapshot = new
            {
                avoidRecentRepeats = true,
                recentDrawCount = 3,
                preRotationCandidateCount = 2,
                excludedCandidateCount = 1,
                postRotationCandidateCount = 1
            }
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
