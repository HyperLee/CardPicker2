using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class MealCardMetadataValidatorTests
{
    [Fact]
    public void ValidateAndNormalize_AllowsMissingMetadata()
    {
        var validator = new MealCardMetadataValidator();

        var result = validator.ValidateAndNormalize(null);

        Assert.True(result.Succeeded);
        Assert.Null(result.Metadata);
    }

    [Fact]
    public void ValidateAndNormalize_RejectsUnsupportedEnumValues()
    {
        var validator = new MealCardMetadataValidator();

        var result = validator.ValidateAndNormalize(new MealCardDecisionMetadata
        {
            PriceRange = (PriceRange)999
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Metadata.InvalidEnum", result.MessageKey);
    }

    [Theory]
    [InlineData("preparation")]
    [InlineData("spice")]
    [InlineData("dietary")]
    public void ValidateAndNormalize_RejectsUnsupportedEnumValuesForEveryMetadataField(string field)
    {
        var validator = new MealCardMetadataValidator();
        var metadata = field switch
        {
            "preparation" => new MealCardDecisionMetadata
            {
                PreparationTimeRange = (PreparationTimeRange)999
            },
            "spice" => new MealCardDecisionMetadata
            {
                SpiceLevel = (SpiceLevel)999
            },
            _ => new MealCardDecisionMetadata
            {
                DietaryPreferences = new[] { (DietaryPreference)999 }
            }
        };

        var result = validator.ValidateAndNormalize(metadata);

        Assert.False(result.Succeeded);
        Assert.Equal("Metadata.InvalidEnum", result.MessageKey);
    }

    [Fact]
    public void ValidateAndNormalize_RejectsBlankTagsBeforePersistence()
    {
        var validator = new MealCardMetadataValidator();

        var result = validator.ValidateAndNormalize(new MealCardDecisionMetadata
        {
            Tags = new[] { "便當", "   " }
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Metadata.InvalidTag", result.MessageKey);
    }

    [Fact]
    public void ValidateAndNormalize_NormalizesDuplicateTagsAndDietaryPreferences()
    {
        var validator = new MealCardMetadataValidator();

        var result = validator.ValidateAndNormalize(new MealCardDecisionMetadata
        {
            Tags = new[] { "  Bento ", "bento" },
            DietaryPreferences = new[] { DietaryPreference.Light, DietaryPreference.Light }
        });

        Assert.True(result.Succeeded);
        Assert.Equal(new[] { "Bento" }, result.Metadata!.Tags);
        Assert.Equal(new[] { DietaryPreference.Light }, result.Metadata.DietaryPreferences);
    }

    [Fact]
    public void ValidateAndNormalize_PreservesSupportedEnumDecisionFields()
    {
        var validator = new MealCardMetadataValidator();

        var result = validator.ValidateAndNormalize(new MealCardDecisionMetadata
        {
            Tags = new[] { "快速" },
            PriceRange = PriceRange.Medium,
            PreparationTimeRange = PreparationTimeRange.Standard,
            DietaryPreferences = new[] { DietaryPreference.TakeoutFriendly },
            SpiceLevel = SpiceLevel.Mild
        });

        Assert.True(result.Succeeded);
        Assert.Equal(PriceRange.Medium, result.Metadata!.PriceRange);
        Assert.Equal(PreparationTimeRange.Standard, result.Metadata.PreparationTimeRange);
        Assert.Equal(new[] { DietaryPreference.TakeoutFriendly }, result.Metadata.DietaryPreferences);
        Assert.Equal(SpiceLevel.Mild, result.Metadata.SpiceLevel);
    }
}
