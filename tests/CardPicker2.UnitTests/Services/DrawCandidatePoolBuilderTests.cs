using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class DrawCandidatePoolBuilderTests
{
    [Fact]
    public void Build_WithNormalMode_ContainsOnlySelectedMealTypeActiveCards()
    {
        var builder = new DrawCandidatePoolBuilder();
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true
        };

        var pool = builder.Build(operation, CreateCardsWithDeleted());

        var card = Assert.Single(pool.Cards);
        Assert.Equal(DrawFeatureTestData.LunchCardId, card.Id);
        Assert.Equal(1m, pool.NominalProbability);
    }

    [Fact]
    public void Build_WithRandomMode_ContainsAllActiveCardsAndIgnoresMealType()
    {
        var builder = new DrawCandidatePoolBuilder();
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            MealType = MealType.Breakfast,
            CoinInserted = true
        };

        var pool = builder.Build(operation, CreateCardsWithDeleted());

        Assert.Equal(4, pool.Cards.Count);
        Assert.DoesNotContain(pool.Cards, card => card.Id == DrawFeatureTestData.DeletedCardId);
        Assert.Contains(pool.Cards, card => card.MealType == MealType.Lunch);
        Assert.Contains(pool.Cards, card => card.MealType == MealType.Dinner);
    }

    [Fact]
    public void Build_ExposesEqualNominalProbabilityForEveryCandidate()
    {
        var builder = new DrawCandidatePoolBuilder();
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            CoinInserted = true
        };

        var pool = builder.Build(operation, CreateCardsWithDeleted());

        Assert.Equal(0.25m, pool.NominalProbability);
    }

    private static IReadOnlyList<MealCard> CreateCardsWithDeleted()
    {
        return DrawFeatureTestData.ActiveLocalizedCards()
            .Concat(new[]
            {
                new MealCard(
                    DrawFeatureTestData.DeletedCardId,
                    MealType.Lunch,
                    new Dictionary<string, MealCardLocalizedContent>
                    {
                        [SupportedLanguage.ZhTw.CultureName] = new("已刪除午餐", "不應出現"),
                        [SupportedLanguage.EnUs.CultureName] = new("Deleted Lunch", "Should not appear")
                    },
                    CardStatus.Deleted,
                    DrawFeatureTestData.KnownTimestamp())
            })
            .ToList();
    }
}
