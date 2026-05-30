using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class CardPreferenceCriteriaTests
{
    [Fact]
    public void Defaults_DoNotFilterFavoritesOrDrawEligibility()
    {
        var criteria = new CardPreferenceCriteria();

        Assert.Equal(FavoriteFilter.All, criteria.FavoriteFilter);
        Assert.Equal(DrawEligibilityFilter.All, criteria.DrawEligibilityFilter);
    }

    [Fact]
    public void Normalize_ReplacesUnsupportedValuesWithAll()
    {
        var criteria = new CardPreferenceCriteria
        {
            FavoriteFilter = (FavoriteFilter)999,
            DrawEligibilityFilter = (DrawEligibilityFilter)999
        };

        var normalized = criteria.Normalize();

        Assert.Equal(FavoriteFilter.All, normalized.FavoriteFilter);
        Assert.Equal(DrawEligibilityFilter.All, normalized.DrawEligibilityFilter);
    }

    [Fact]
    public void SearchCriteria_DefaultsPreferenceCriteria()
    {
        var criteria = new SearchCriteria();

        Assert.Equal(FavoriteFilter.All, criteria.Preferences.FavoriteFilter);
        Assert.Equal(DrawEligibilityFilter.All, criteria.Preferences.DrawEligibilityFilter);
    }
}
