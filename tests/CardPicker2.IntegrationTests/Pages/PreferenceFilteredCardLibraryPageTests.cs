using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class PreferenceFilteredCardLibraryPageTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task CardsPage_RendersFavoriteAndDrawEligibilityFilters()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document(
            favoriteCardId: MetadataFilterTestData.VegetarianLunchCardId,
            excludedCardId: MetadataFilterTestData.VegetarianLunchCardId));
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Cards"));

        PreferenceHtmlAssertions.AssertCardLibraryPreferenceFilters(html);
        PreferenceHtmlAssertions.AssertPreferenceBadges(html, "已收藏", "已排除抽卡");
    }

    [Fact]
    public async Task CardsPage_FavoriteFilterIntersectsWithExistingCriteria()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document(
            favoriteCardId: MetadataFilterTestData.VegetarianLunchCardId));
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Cards?favoriteFilter=FavoritesOnly&mealType=Lunch&tags=蔬食"));

        Assert.Contains("菇菇蔬食便當", html, StringComparison.Ordinal);
        Assert.DoesNotContain("雞腿便當", html, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
