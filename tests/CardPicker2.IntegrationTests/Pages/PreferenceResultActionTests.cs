using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class PreferenceResultActionTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory =
        DrawFeatureWebApplicationFactory.CreateWithDeterministicRandomizer(0);

    [Fact]
    public async Task ResultPreferencePost_FavoritesResultAndRestoresSameCard()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document());
        var client = _factory.CreateClient();
        var operationId = Guid.NewGuid();
        var drawResponse = await client.PostAsync("/", await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch),
            drawOperationId: operationId));
        var drawHtml = WebUtility.HtmlDecode(await drawResponse.Content.ReadAsStringAsync());
        var cardId = ExtractResultCardId(drawHtml);

        var preferenceResponse = await client.PostAsync("/?handler=Preference", await _factory.CreatePreferenceContentAsync(
            client,
            cardId,
            targetIsFavorite: true,
            drawOperationId: operationId,
            resultCardId: cardId,
            tokenPath: "/"));
        var html = WebUtility.HtmlDecode(await preferenceResponse.Content.ReadAsStringAsync());

        PreferenceHtmlAssertions.AssertResultPreferenceActionState(html, cardId, "已收藏");
        Assert.Contains(operationId.ToString(), html, StringComparison.OrdinalIgnoreCase);
        var document = await ReadDocumentAsync();
        Assert.Single(document.DrawHistory);
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

    private static Guid ExtractResultCardId(string html)
    {
        const string marker = "data-result-card-id=\"";
        var start = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        Assert.True(start >= 0, "Expected result card id marker.");
        start += marker.Length;
        var end = html.IndexOf('"', start);
        return Guid.Parse(html[start..end]);
    }
}
