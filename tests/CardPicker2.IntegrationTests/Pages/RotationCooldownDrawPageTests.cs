using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class RotationCooldownDrawPageTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task GetHome_ShowsDefaultCooldownControls()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        RotationCooldownHtmlAssertions.AssertCooldownControls(html);
        RotationCooldownHtmlAssertions.AssertDefaultCooldownState(html);
        RotationCooldownHtmlAssertions.AssertNoUntranslatedRotationKeys(html);
    }

    [Fact]
    public async Task GetHome_AfterPriorSubmittedCooldownState_DoesNotPersistAsPreference()
    {
        var client = _factory.CreateClient();
        using var content = await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Random),
            mealType: null,
            avoidRecentRepeats: false,
            recentDrawCount: "7");

        await client.PostAsync("/?handler=Draw", content);

        var html = await client.GetStringAsync("/");
        RotationCooldownHtmlAssertions.AssertDefaultCooldownState(html);
    }

    [Fact]
    public async Task PostDraw_WithNormalCooldownFields_RendersCountOnlyRotationSummary()
    {
        await _factory.WriteLibraryDocumentAsync(RotationDocumentWithRecentLunchHistory(), JsonOptions);
        var client = _factory.CreateClient();
        using var content = await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch),
            avoidRecentRepeats: true,
            recentDrawCount: "3");

        var response = await client.PostAsync("/?handler=Draw", content);

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("抽卡結果", html);
        RotationCooldownHtmlAssertions.AssertRotationSuccessSummary(html, "輪替前候選池", "近期排除", "輪替後候選池");
        Assert.DoesNotContain("牛肉麵</span>", html);
    }

    [Fact]
    public async Task PostDraw_WithRandomCooldownFields_DoesNotRequireMealType()
    {
        await _factory.WriteLibraryDocumentAsync(RotationDocumentWithRecentLunchHistory(), JsonOptions);
        var client = _factory.CreateClient();
        using var content = await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Random),
            mealType: null,
            avoidRecentRepeats: true,
            recentDrawCount: "3");

        var response = await client.PostAsync("/?handler=Draw", content);

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("抽卡結果", html);
        RotationCooldownHtmlAssertions.AssertRotationSuccessSummary(html, "輪替前候選池", "輪替後候選池");
    }

    [Fact]
    public async Task PostDraw_WithCooldownFieldsWithoutAntiForgery_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(
            DrawFeatureWebApplicationFactory.CreateFilteredDrawPayload(
                    antiForgeryToken: string.Empty,
                    drawMode: nameof(DrawMode.Random),
                    mealType: null,
                    avoidRecentRepeats: true,
                    recentDrawCount: "3")
                .Where(pair => pair.Key != "__RequestVerificationToken")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDraw_WhenRotationEmptiesPool_ShowsRelaxationPromptInsteadOfResult()
    {
        await _factory.WriteLibraryDocumentAsync(RotationDocumentWithAllBentoCandidatesRecent(), JsonOptions);
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
        RotationCooldownHtmlAssertions.AssertRotationEmptyAlert(html, "降低", "關閉避免最近重複");
        Assert.DoesNotContain("抽卡結果", html);
    }

    [Fact]
    public async Task PostDraw_WithInvalidRecentDrawCount_ShowsLocalizedValidationMessage()
    {
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
    }

    [Fact]
    public async Task GetHome_WhenLibraryBlocked_DisablesDrawWithCooldownControlsPresent()
    {
        await _factory.WriteLibraryJsonAsync("{");
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        RotationCooldownHtmlAssertions.AssertCooldownControls(html);
        Assert.Contains("disabled", html);
        Assert.Contains("卡牌庫檔案", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static object RotationDocumentWithRecentLunchHistory()
    {
        return new
        {
            schemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            cards = new[]
            {
                Card(BreakfastCardId, "Breakfast", "鮪魚蛋餅", "Tuna Egg Crepe", null),
                Card(LunchCardId, "Lunch", "牛肉麵", "Beef Noodle Soup", null),
                Card(LowPriceLunchCardId, "Lunch", "滷肉飯便當", "Braised Pork Rice Bento", Metadata(new[] { "便當" })),
                Card(VegetarianLunchCardId, "Lunch", "菇菇蔬食便當", "Mushroom Vegetable Bento", Metadata(new[] { "蔬食", "便當" })),
                Card(DinnerCardId, "Dinner", "番茄燉飯", "Tomato Risotto", null)
            },
            drawHistory = new[]
            {
                History(
                    Guid.Parse("77777777-7777-7777-7777-777777777771"),
                    Guid.Parse("88888888-8888-8888-8888-888888888881"),
                    LunchCardId,
                    KnownTimestamp.AddMinutes(1)),
                History(
                    Guid.Parse("77777777-7777-7777-7777-777777777772"),
                    Guid.Parse("88888888-8888-8888-8888-888888888882"),
                    LowPriceLunchCardId,
                    KnownTimestamp.AddMinutes(2))
            }
        };
    }

    private static object RotationDocumentWithAllBentoCandidatesRecent()
    {
        return new
        {
            schemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            cards = new[]
            {
                Card(BreakfastCardId, "Breakfast", "鮪魚蛋餅", "Tuna Egg Crepe", null),
                Card(LowPriceLunchCardId, "Lunch", "滷肉飯便當", "Braised Pork Rice Bento", Metadata(new[] { "便當" })),
                Card(VegetarianLunchCardId, "Lunch", "菇菇蔬食便當", "Mushroom Vegetable Bento", Metadata(new[] { "蔬食", "便當" })),
                Card(DinnerCardId, "Dinner", "番茄燉飯", "Tomato Risotto", null)
            },
            drawHistory = new[]
            {
                History(
                    Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Guid.Parse("88888888-8888-8888-8888-888888888887"),
                    LowPriceLunchCardId,
                    KnownTimestamp.AddMinutes(1)),
                History(
                    Guid.Parse("77777777-7777-7777-7777-777777777778"),
                    Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    VegetarianLunchCardId,
                    KnownTimestamp.AddMinutes(2))
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
                [SupportedLanguage.ZhTw.CultureName] = new
                {
                    name = zhTwName,
                    description = $"{zhTwName} 描述"
                },
                [SupportedLanguage.EnUs.CultureName] = new
                {
                    name = enUsName,
                    description = $"{enUsName} description"
                }
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
                preRotationCandidateCount = 3,
                excludedCandidateCount = 1,
                postRotationCandidateCount = 2
            }
        };
    }

    private static readonly Guid BreakfastCardId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222221");
    private static readonly Guid LowPriceLunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid VegetarianLunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222223");
    private static readonly Guid DinnerCardId = Guid.Parse("33333333-3333-3333-3333-333333333331");
    private static readonly DateTimeOffset KnownTimestamp = new(2026, 5, 13, 8, 0, 0, TimeSpan.Zero);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
