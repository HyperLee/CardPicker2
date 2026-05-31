using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class DrawBindingValidationTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory =
        DrawFeatureWebApplicationFactory.CreateWithDeterministicRandomizer(0);

    [Fact]
    public async Task PostDraw_WithInvalidDrawModeBindingError_DoesNotAppendHistory()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetAntiForgeryTokenAsync(client);

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DrawMode"] = "NotSupported",
            ["MealType"] = nameof(MealType.Breakfast),
            ["CoinInserted"] = "true",
            ["DrawOperationId"] = Guid.NewGuid().ToString()
        }));

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("抽卡表單包含不支援的值，請重新選擇後再試。", html, StringComparison.Ordinal);
        Assert.DoesNotContain("NotSupported", html, StringComparison.Ordinal);
        Assert.DoesNotContain("抽卡結果", html, StringComparison.Ordinal);
        Assert.Empty((await ReadDocumentAsync()).DrawHistory);
    }

    [Fact]
    public async Task PostDraw_WithValidDrawSubmission_StillAppendsSingleHistory()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/?handler=Draw", await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Breakfast),
            drawOperationId: Guid.NewGuid()));

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("抽卡結果", html, StringComparison.Ordinal);
        Assert.Single((await ReadDocumentAsync()).DrawHistory);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<CardLibraryDocument> ReadDocumentAsync()
    {
        var json = await File.ReadAllTextAsync(_factory.LibraryFilePath);
        return JsonSerializer.Deserialize<CardLibraryDocument>(json, JsonOptions)!;
    }
}
