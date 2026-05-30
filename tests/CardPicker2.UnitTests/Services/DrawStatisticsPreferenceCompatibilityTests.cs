using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class DrawStatisticsPreferenceCompatibilityTests
{
    [Fact]
    public void FavoriteState_DoesNotChangeCandidatePoolMembershipOrProbability()
    {
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
        var pool = new DrawCandidatePoolBuilder().Build(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            CoinInserted = true
        }, cards);

        Assert.Equal(cards.Count, pool.Cards.Count);
        Assert.Equal(1m / cards.Count, pool.NominalProbability);
    }
}
