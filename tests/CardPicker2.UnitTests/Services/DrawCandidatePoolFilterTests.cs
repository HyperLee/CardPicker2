using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class DrawCandidatePoolFilterTests
{
    [Fact]
    public void Build_WithNormalMode_AppliesMealTypeBeforeMetadataFilters()
    {
        var builder = new DrawCandidatePoolBuilder(new MealCardFilterService());
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            Filters = new CardFilterCriteria
            {
                Tags = new[] { "便當" },
                DietaryPreferences = new[] { DietaryPreference.Vegetarian }
            }
        };

        var pool = builder.Build(operation, CreateCards());

        var card = Assert.Single(pool.Cards);
        Assert.Equal(DrawFeatureTestData.VegetarianLunchCardId, card.Id);
        Assert.Equal(1m, pool.NominalProbability);
    }

    [Fact]
    public void Build_WithRandomMode_IgnoresMealTypeButAppliesMetadataFilters()
    {
        var builder = new DrawCandidatePoolBuilder(new MealCardFilterService());
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            MealType = MealType.Breakfast,
            CoinInserted = true,
            Filters = new CardFilterCriteria
            {
                PriceRange = PriceRange.Low,
                MaxSpiceLevel = SpiceLevel.Mild
            }
        };

        var pool = builder.Build(operation, CreateCards());

        Assert.Null(pool.SelectedMealType);
        Assert.Equal(new[] { DrawFeatureTestData.LowPriceLunchCardId }, pool.Cards.Select(card => card.Id));
        Assert.Equal(1m, pool.NominalProbability);
    }

    [Fact]
    public void Build_WithSelectedMetadataFilters_ExcludesCardsMissingMetadata()
    {
        var builder = new DrawCandidatePoolBuilder(new MealCardFilterService());
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            CoinInserted = true,
            Filters = new CardFilterCriteria { PriceRange = PriceRange.Medium }
        };

        var pool = builder.Build(operation, CreateCards());

        Assert.DoesNotContain(pool.Cards, card => card.Id == DrawFeatureTestData.MetadataMissingDinnerCardId);
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
                    PreparationTimeRange = PreparationTimeRange.Quick,
                    DietaryPreferences = new[] { DietaryPreference.Vegetarian, DietaryPreference.TakeoutFriendly },
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
                    PreparationTimeRange = PreparationTimeRange.Quick,
                    DietaryPreferences = new[] { DietaryPreference.HeavyFlavor },
                    SpiceLevel = SpiceLevel.Hot
                })
        };
    }

    private static MealCard Create(Guid id, MealType mealType, MealCardDecisionMetadata? metadata)
    {
        return new MealCard(
            id,
            mealType,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new(id.ToString(), "描述"),
                [SupportedLanguage.EnUs.CultureName] = new(id.ToString(), "description")
            })
        {
            DecisionMetadata = metadata
        };
    }
}
