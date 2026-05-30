using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class PreferenceFilteredDrawPageTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory =
        DrawFeatureWebApplicationFactory.CreateWithDeterministicRandomizer(0);

    [Fact]
    public async Task PostDraw_DoesNotRevealExcludedCard()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document(
            excludedCardId: MetadataFilterTestData.VegetarianLunchCardId));
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/", await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch)));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.DoesNotContain("<h2 id=\"draw-result-title\">菇菇蔬食便當</h2>", html, StringComparison.Ordinal);
        Assert.Contains("雞腿便當", html, StringComparison.Ordinal);
        Assert.Contains("抽卡結果", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostDraw_WhenPreferenceExclusionEmptiesPool_ShowsPreferencePrompt()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document(
            excludedCardId: MetadataFilterTestData.VegetarianLunchCardId));
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/", await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch),
            dietaryPreferences: new[] { nameof(DietaryPreference.Vegetarian) }));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains("取消排除", html, StringComparison.Ordinal);
        Assert.DoesNotContain("抽卡結果", html, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
