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
}
