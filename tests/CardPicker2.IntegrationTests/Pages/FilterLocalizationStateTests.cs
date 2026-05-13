using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class FilterLocalizationStateTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task GetHome_WithEnglishCulture_RendersMetadataFiltersInEnglish()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.SchemaV4Document());
        var client = _factory.CreateClientForCulture(SupportedLanguage.EnUs.CultureName);
        var path = "/?" + DrawFeatureWebApplicationFactory.CreateFilterQuery(
            priceRange: nameof(PriceRange.Low),
            dietaryPreferences: new[] { nameof(DietaryPreference.TakeoutFriendly) },
            maxSpiceLevel: nameof(SpiceLevel.Mild),
            tags: new[] { "Bento" });

        var html = WebUtility.HtmlDecode(await client.GetStringAsync(path));

        Assert.Contains("Price range", html);
        Assert.Contains("Takeout friendly", html);
        Assert.Contains("Bento", html);
        MetadataFilterHtmlAssertions.AssertNoUntranslatedMetadataKeys(html);
    }

    [Fact]
    public async Task PostDraw_ThenGetRestore_PreservesResultCardAndFilterState()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.SchemaV4Document());
        var client = _factory.CreateClient();
        var operationId = Guid.NewGuid();
        using var content = await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch),
            drawOperationId: operationId,
            dietaryPreferences: new[] { nameof(DietaryPreference.Vegetarian) },
            tags: new[] { "蔬食", "便當" });
        var response = await client.PostAsync("/?handler=Draw", content);
        var postHtml = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains(MetadataFilterTestData.VegetarianLunchCardId.ToString(), postHtml);

        var restorePath = "/?drawMode=Normal&mealType=Lunch&resultCardId=" +
            $"{MetadataFilterTestData.VegetarianLunchCardId}&drawOperationId={operationId}&tags=蔬食&tags=便當&dietaryPreferences=Vegetarian";
        var restoredHtml = WebUtility.HtmlDecode(await client.GetStringAsync(restorePath));

        Assert.Contains("菇菇蔬食便當", restoredHtml);
        Assert.Contains("蔬食", restoredHtml);
        Assert.Contains("便當", restoredHtml);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
