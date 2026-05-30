using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class DrawCandidatePoolPreferenceTests
{
    [Fact]
    public void Build_RemovesExcludedCardsBeforeFairRandomization()
    {
        var builder = new DrawCandidatePoolBuilder();
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true
        };

        var pool = builder.Build(operation, CreatePreferenceCards());

        Assert.DoesNotContain(pool.Cards, card => card.Id == DrawFeatureTestData.LunchCardId);
        Assert.All(pool.Cards, card => Assert.True(card.IsDrawable));
        Assert.Equal(1, pool.PreferenceExcludedCount);
    }

    [Fact]
    public void Build_WhenOnlyMatchingCardsAreExcluded_ReportsPreferenceExcludedCount()
    {
        var builder = new DrawCandidatePoolBuilder();
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            Filters = new CardFilterCriteria
            {
                Tags = new[] { "麵食" },
                CurrentLanguage = SupportedLanguage.ZhTw
            }
        };

        var pool = builder.Build(operation, CreatePreferenceCards());

        Assert.Empty(pool.Cards);
        Assert.Equal(1, pool.PreferenceExcludedCount);
        Assert.Equal(1, pool.PrePreferenceCandidateCount);
    }

    [Fact]
    public void Build_DoesNotRemoveFavoriteCards()
    {
        var builder = new DrawCandidatePoolBuilder();
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            CoinInserted = true
        };
        var cards = DrawFeatureTestData.ActiveLocalizedCards()
            .Select((card, index) => new MealCard(
                card.Id,
                card.MealType,
                card.Localizations,
                card.Status,
                card.DeletedAtUtc,
                card.DecisionMetadata,
                new CardPreferenceState { IsFavorite = index == 0 }))
            .ToList();

        var pool = builder.Build(operation, cards);

        Assert.Equal(cards.Count, pool.Cards.Count);
        Assert.Contains(pool.Cards, card => card.Preferences.IsFavorite);
    }

    private static IReadOnlyList<MealCard> CreatePreferenceCards()
    {
        return DrawFeatureTestData.SchemaV5Cards()
            .Select(cardJson => System.Text.Json.JsonSerializer.Deserialize<MealCard>(
                DrawFeatureTestData.Serialize(cardJson),
                DrawFeatureTestData.JsonOptions)!)
            .ToList();
    }
}
