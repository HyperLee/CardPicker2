using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class PreferenceResultActionLocalizationTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory =
        DrawFeatureWebApplicationFactory.CreateWithDeterministicRandomizer(0);

    [Fact]
    public async Task ResultPreferencePost_IsTargetStateAndLocalizesCurrentCulture()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document());
        var client = _factory.CreateClientForCulture(SupportedLanguage.EnUs.CultureName);
        var operationId = Guid.NewGuid();
        var drawResponse = await client.PostAsync("/", await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch),
            drawOperationId: operationId));
        var cardId = ExtractResultCardId(WebUtility.HtmlDecode(await drawResponse.Content.ReadAsStringAsync()));

        await client.PostAsync("/?handler=Preference", await _factory.CreatePreferenceContentAsync(
            client,
            cardId,
            targetIsFavorite: true,
            drawOperationId: operationId,
            resultCardId: cardId,
            tokenPath: "/"));
        var secondResponse = await client.PostAsync("/?handler=Preference", await _factory.CreatePreferenceContentAsync(
            client,
            cardId,
            targetIsFavorite: true,
            drawOperationId: operationId,
            resultCardId: cardId,
            tokenPath: "/"));
        var html = WebUtility.HtmlDecode(await secondResponse.Content.ReadAsStringAsync());
        var document = await ReadDocumentAsync();
        var card = document.Cards.Single(card => card.Id == cardId);

        Assert.True(card.Preferences.IsFavorite);
        Assert.Contains("Favorite", html, StringComparison.Ordinal);
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
