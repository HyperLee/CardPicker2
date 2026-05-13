using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class FilteredCardLibraryPageTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task GetCards_ShowsMetadataFilterControlsAndResultCount()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.SchemaV4Document());
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/Cards");

        MetadataFilterHtmlAssertions.AssertCardLibraryFilterControls(html);
        Assert.Contains("共 4 張", WebUtility.HtmlDecode(html));
        MetadataFilterHtmlAssertions.AssertNoUntranslatedMetadataKeys(html);
    }

    [Fact]
    public async Task GetCards_WithMetadataFilters_ShowsOnlyMatchingCardsAndSummary()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.SchemaV4Document());
        var client = _factory.CreateClient();
        var path = DrawFeatureWebApplicationFactory.CreateCardsPathWithFilters(
            mealType: nameof(MealType.Lunch),
            dietaryPreferences: new[] { nameof(DietaryPreference.Vegetarian) },
            maxSpiceLevel: nameof(SpiceLevel.Mild),
            tags: new[] { "蔬食", "便當" });

        var html = WebUtility.HtmlDecode(await client.GetStringAsync(path));

        Assert.Contains("菇菇蔬食便當", html);
        Assert.DoesNotContain("鮪魚蛋餅", html);
        Assert.DoesNotContain("麻辣乾拌麵", html);
        MetadataFilterHtmlAssertions.AssertFilterSummary(html, "蔬食", "便當", "蔬食");
        MetadataFilterHtmlAssertions.AssertMetadataBadges(html, "便當", "蔬食");
    }

    [Fact]
    public async Task GetCards_WithEmptyFilteredResult_ShowsLocalizedEmptyState()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.SchemaV4Document());
        var client = _factory.CreateClient();
        var path = DrawFeatureWebApplicationFactory.CreateCardsPathWithFilters(
            priceRange: nameof(PriceRange.High),
            tags: new[] { "不存在" });

        var html = WebUtility.HtmlDecode(await client.GetStringAsync(path));

        MetadataFilterHtmlAssertions.AssertLocalizedEmptyState(html, "沒有符合這些條件的餐點卡牌。");
    }

    [Fact]
    public async Task GetCards_WhenLibraryIsBlocked_DisablesFilterSearch()
    {
        await _factory.WriteLibraryJsonAsync("{");
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Cards"));

        Assert.Contains("卡牌庫檔案", html);
        Assert.Contains("disabled", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
