using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class MealCardFilterServiceTests
{
    [Fact]
    public void Apply_WithoutMetadataFilters_ReturnsActiveCardsIncludingMissingMetadata()
    {
        var service = new MealCardFilterService();

        var results = service.Apply(CreateCards(), new CardFilterCriteria());

        Assert.Equal(4, results.Count);
        Assert.Contains(results, card => card.DecisionMetadata is null);
        Assert.DoesNotContain(results, card => card.IsDeleted);
    }

    [Fact]
    public void Apply_WithSelectedMetadataFields_ExcludesCardsWithMissingMetadata()
    {
        var service = new MealCardFilterService();

        var results = service.Apply(CreateCards(), new CardFilterCriteria
        {
            MealType = MealType.Lunch,
            PriceRange = PriceRange.Low
        });

        Assert.Equal(new[] { DrawFeatureTestData.LowPriceLunchCardId }, results.Select(card => card.Id));
    }

    [Fact]
    public void Apply_RequiresAllDietaryPreferencesAndTagsAndMaxSpiceLessThanOrEqual()
    {
        var service = new MealCardFilterService();

        var results = service.Apply(CreateCards(), new CardFilterCriteria
        {
            DietaryPreferences = new[] { DietaryPreference.Vegetarian, DietaryPreference.TakeoutFriendly },
            Tags = new[] { "便當", "蔬食" },
            MaxSpiceLevel = SpiceLevel.Mild
        });

        var card = Assert.Single(results);
        Assert.Equal(DrawFeatureTestData.VegetarianLunchCardId, card.Id);
    }

    private static IReadOnlyList<MealCard> CreateCards()
    {
        return new[]
        {
            Create(
                DrawFeatureTestData.LowPriceLunchCardId,
                MealType.Lunch,
                new MealCardDecisionMetadata
                {
                    Tags = new[] { "便當", "外帶" },
                    PriceRange = PriceRange.Low,
                    PreparationTimeRange = PreparationTimeRange.Quick,
                    DietaryPreferences = new[] { DietaryPreference.TakeoutFriendly },
                    SpiceLevel = SpiceLevel.None
                }),
            Create(
                DrawFeatureTestData.VegetarianLunchCardId,
                MealType.Lunch,
                new MealCardDecisionMetadata
                {
                    Tags = new[] { "蔬食", "便當" },
                    PriceRange = PriceRange.Medium,
                    DietaryPreferences = new[] { DietaryPreference.Vegetarian, DietaryPreference.Light, DietaryPreference.TakeoutFriendly },
                    SpiceLevel = SpiceLevel.None
                }),
            Create(
                DrawFeatureTestData.MetadataMissingDinnerCardId,
                MealType.Dinner,
                metadata: null),
            Create(
                DrawFeatureTestData.SpicyDinnerCardId,
                MealType.Dinner,
                new MealCardDecisionMetadata
                {
                    Tags = new[] { "麵食", "辣" },
                    PriceRange = PriceRange.Low,
                    DietaryPreferences = new[] { DietaryPreference.HeavyFlavor },
                    SpiceLevel = SpiceLevel.Hot
                }),
            new MealCard(
                DrawFeatureTestData.DeletedCardId,
                MealType.Lunch,
                Localizations("已刪除", "Deleted"),
                CardStatus.Deleted,
                DrawFeatureTestData.KnownTimestamp())
            {
                DecisionMetadata = new MealCardDecisionMetadata { PriceRange = PriceRange.Low }
            }
        };
    }

    private static MealCard Create(Guid id, MealType mealType, MealCardDecisionMetadata? metadata)
    {
        return new MealCard(id, mealType, Localizations(id.ToString(), id.ToString()))
        {
            DecisionMetadata = metadata
        };
    }

    private static Dictionary<string, MealCardLocalizedContent> Localizations(string zhTwName, string enUsName)
    {
        return new Dictionary<string, MealCardLocalizedContent>
        {
            [SupportedLanguage.ZhTw.CultureName] = new(zhTwName, $"{zhTwName} 描述"),
            [SupportedLanguage.EnUs.CultureName] = new(enUsName, $"{enUsName} description")
        };
    }
}
