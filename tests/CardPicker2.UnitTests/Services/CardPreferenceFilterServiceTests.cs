using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class CardPreferenceFilterServiceTests
{
    [Fact]
    public void Apply_CombinesFavoriteAndDrawEligibilityFilters()
    {
        var service = new CardPreferenceFilterService();
        var cards = CreateCards();

        var favorites = service.Apply(cards, new CardPreferenceCriteria
        {
            FavoriteFilter = FavoriteFilter.FavoritesOnly,
            DrawEligibilityFilter = DrawEligibilityFilter.All
        });
        var drawableFavorites = service.Apply(cards, new CardPreferenceCriteria
        {
            FavoriteFilter = FavoriteFilter.FavoritesOnly,
            DrawEligibilityFilter = DrawEligibilityFilter.DrawableOnly
        });
        var excluded = service.Apply(cards, new CardPreferenceCriteria
        {
            FavoriteFilter = FavoriteFilter.All,
            DrawEligibilityFilter = DrawEligibilityFilter.ExcludedOnly
        });

        Assert.Single(favorites);
        Assert.Single(drawableFavorites);
        Assert.Single(excluded);
        Assert.All(excluded, card => Assert.True(card.Preferences.IsExcludedFromDraw));
    }

    private static IReadOnlyList<MealCard> CreateCards()
    {
        return DrawFeatureTestData.SchemaV5Cards()
            .Select(cardJson => System.Text.Json.JsonSerializer.Deserialize<MealCard>(
                DrawFeatureTestData.Serialize(cardJson),
                DrawFeatureTestData.JsonOptions)!)
            .ToList();
    }
}
