using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class FilteredDrawPageTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task GetHome_ShowsMetadataFilterControls()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        MetadataFilterHtmlAssertions.AssertHomeFilterControls(html);
        MetadataFilterHtmlAssertions.AssertNoUntranslatedMetadataKeys(html);
    }

    [Fact]
    public async Task PostDraw_WithNormalMetadataFilters_RevealsOnlyMatchingResult()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.SchemaV4Document(), JsonOptions);
        var client = _factory.CreateClient();
        using var content = await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch),
            dietaryPreferences: new[] { nameof(DietaryPreference.Vegetarian) },
            maxSpiceLevel: nameof(SpiceLevel.Mild),
            tags: new[] { "蔬食", "便當" });

        var response = await client.PostAsync("/?handler=Draw", content);

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("菇菇蔬食便當", html);
        Assert.Contains("蔬食", html);
        Assert.Contains("便當", html);
        Assert.Contains("抽卡結果", html);
    }

    [Fact]
    public async Task PostDraw_WithEmptyFilteredPool_ShowsEmptyMessageAndDoesNotAppendHistory()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.SchemaV4Document(), JsonOptions);
        var client = _factory.CreateClient();
        using var content = await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Breakfast),
            priceRange: nameof(PriceRange.High),
            tags: new[] { "不存在" });

        var response = await client.PostAsync("/?handler=Draw", content);

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("目前沒有符合條件的餐點", html);
        var document = JsonSerializer.Deserialize<CardLibraryDocument>(
            await File.ReadAllTextAsync(_factory.LibraryFilePath),
            JsonOptions);
        Assert.NotNull(document);
        Assert.Empty(document.DrawHistory);
    }

    [Fact]
    public async Task PostDraw_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(
            DrawFeatureWebApplicationFactory.CreateFilteredDrawPayload(
                antiForgeryToken: string.Empty,
                drawMode: nameof(DrawMode.Random),
                mealType: null,
                tags: new[] { "便當" })
                .Where(pair => pair.Key != "__RequestVerificationToken")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHome_WhenLibraryIsBlocked_DisablesDraw()
    {
        await _factory.WriteLibraryJsonAsync("{");
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("卡牌庫檔案", html);
        Assert.Contains("disabled", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
