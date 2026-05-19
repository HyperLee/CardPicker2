using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class MealCardDecisionMetadataTests
{
    [Fact]
    public void Normalize_TrimsTagsRemovesBlanksAndDeduplicatesCaseInsensitively()
    {
        var metadata = new MealCardDecisionMetadata
        {
            Tags = new[] { "  便當 ", "便當", "Bento", "bento", "   ", "快速" }
        };

        var normalized = metadata.Normalize();

        Assert.Equal(new[] { "便當", "Bento", "快速" }, normalized.Tags);
    }

    [Fact]
    public void Normalize_DeduplicatesDietaryPreferencesAndKeepsStableEnumOrder()
    {
        var metadata = new MealCardDecisionMetadata
        {
            DietaryPreferences = new[]
            {
                DietaryPreference.TakeoutFriendly,
                DietaryPreference.Vegetarian,
                DietaryPreference.TakeoutFriendly,
                DietaryPreference.Light
            }
        };

        var normalized = metadata.Normalize();

        Assert.Equal(
            new[]
            {
                DietaryPreference.Vegetarian,
                DietaryPreference.Light,
                DietaryPreference.TakeoutFriendly
            },
            normalized.DietaryPreferences);
    }

    [Fact]
    public void NormalizeTags_ReturnsEmptyCollectionForNullInput()
    {
        var normalized = MealCardDecisionMetadata.NormalizeTags(null);

        Assert.Empty(normalized);
    }

    [Fact]
    public void IsEmpty_ReturnsTrueOnlyWhenAllDecisionFieldsAreUnset()
    {
        var empty = new MealCardDecisionMetadata();
        var populated = new MealCardDecisionMetadata
        {
            Tags = new[] { "快速" },
            PriceRange = PriceRange.Low,
            PreparationTimeRange = PreparationTimeRange.Quick,
            DietaryPreferences = new[] { DietaryPreference.Light },
            SpiceLevel = SpiceLevel.None
        };

        Assert.True(empty.IsEmpty());
        Assert.False(populated.IsEmpty());
    }
}
