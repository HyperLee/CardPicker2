using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class CardFilterCriteriaTests
{
    [Fact]
    public void Normalize_TrimsTagsDeduplicatesFiltersAndPreservesLanguage()
    {
        var criteria = new CardFilterCriteria
        {
            MealType = MealType.Lunch,
            PriceRange = PriceRange.Low,
            PreparationTimeRange = PreparationTimeRange.Quick,
            DietaryPreferences = new[]
            {
                DietaryPreference.TakeoutFriendly,
                DietaryPreference.TakeoutFriendly,
                DietaryPreference.Light
            },
            MaxSpiceLevel = SpiceLevel.Mild,
            Tags = new[] { "  便當 ", "便當", "Quick" },
            CurrentLanguage = SupportedLanguage.EnUs
        };

        var normalized = criteria.Normalize();

        Assert.Equal(MealType.Lunch, normalized.MealType);
        Assert.Equal(PriceRange.Low, normalized.PriceRange);
        Assert.Equal(PreparationTimeRange.Quick, normalized.PreparationTimeRange);
        Assert.Equal(new[] { DietaryPreference.Light, DietaryPreference.TakeoutFriendly }, normalized.DietaryPreferences);
        Assert.Equal(SpiceLevel.Mild, normalized.MaxSpiceLevel);
        Assert.Equal(new[] { "便當", "Quick" }, normalized.Tags);
        Assert.Equal(SupportedLanguage.EnUs, normalized.CurrentLanguage);
        Assert.True(normalized.HasActiveMetadataFilters);
    }

    [Fact]
    public void ForDrawMode_IgnoresMealTypeForRandomMode()
    {
        var criteria = new CardFilterCriteria
        {
            MealType = MealType.Dinner,
            PriceRange = PriceRange.Medium
        };

        var projected = criteria.ForDrawMode(DrawMode.Random);

        Assert.Null(projected.MealType);
        Assert.Equal(PriceRange.Medium, projected.PriceRange);
    }
}
