using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

using Microsoft.AspNetCore.Mvc.Testing;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class CardPreferenceCreateEditTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task CreatePost_PersistsDefaultPreferenceState()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await _factory.GetAntiForgeryTokenAsync(client, "/Cards/Create");

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.NameZhTw"] = "偏好新卡",
            ["Input.DescriptionZhTw"] = "偏好新卡描述",
            ["Input.NameEnUs"] = "Preference New Card",
            ["Input.DescriptionEnUs"] = "Preference new card description",
            ["Input.MealType"] = nameof(MealType.Lunch)
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var document = await ReadDocumentAsync();
        var created = Assert.Single(document.Cards, card => card.GetContent(SupportedLanguage.ZhTw).Name == "偏好新卡");
        Assert.False(created.Preferences.IsFavorite);
        Assert.False(created.Preferences.IsExcludedFromDraw);
    }

    [Fact]
    public async Task EditPost_PreservesExistingPreferenceState()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document(
            favoriteCardId: MetadataFilterTestData.VegetarianLunchCardId,
            excludedCardId: MetadataFilterTestData.VegetarianLunchCardId));
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await _factory.GetAntiForgeryTokenAsync(client, $"/Cards/Edit/{MetadataFilterTestData.VegetarianLunchCardId}");

        var response = await client.PostAsync($"/Cards/Edit/{MetadataFilterTestData.VegetarianLunchCardId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.NameZhTw"] = "更新蔬食便當",
            ["Input.DescriptionZhTw"] = "更新描述",
            ["Input.NameEnUs"] = "Updated Vegetable Bento",
            ["Input.DescriptionEnUs"] = "Updated description",
            ["Input.MealType"] = nameof(MealType.Lunch),
            ["Input.TagsInput"] = "蔬食"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var document = await ReadDocumentAsync();
        var updated = Assert.Single(document.Cards, card => card.Id == MetadataFilterTestData.VegetarianLunchCardId);
        Assert.True(updated.Preferences.IsFavorite);
        Assert.True(updated.Preferences.IsExcludedFromDraw);
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
