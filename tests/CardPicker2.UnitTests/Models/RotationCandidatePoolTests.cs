using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class RotationCandidatePoolTests
{
    [Fact]
    public void IsEmptyAfterRotation_WhenPreRotationHasCardsAndPostRotationIsEmpty_ReturnsTrue()
    {
        var card = CreateCard();
        var pool = new RotationCandidatePool(
            new[] { card },
            Array.Empty<MealCard>(),
            new HashSet<Guid> { card.Id },
            RotationCooldownSettings.Default,
            RotationSnapshot.Create(RotationCooldownSettings.Default, 1, 1));

        Assert.Same(card, pool.PreRotationCards.Single());
        Assert.True(pool.IsEmptyAfterRotation);
        Assert.Null(pool.NominalProbability);
    }

    [Fact]
    public void IsEmptyAfterRotation_WhenPreRotationIsEmpty_ReturnsFalse()
    {
        var pool = new RotationCandidatePool(
            Array.Empty<MealCard>(),
            Array.Empty<MealCard>(),
            new HashSet<Guid>(),
            RotationCooldownSettings.Default,
            RotationSnapshot.Create(RotationCooldownSettings.Default, 0, 0));

        Assert.False(pool.IsEmptyAfterRotation);
        Assert.Empty(pool.PreRotationCards);
    }

    private static MealCard CreateCard()
    {
        return new MealCard(
            Guid.NewGuid(),
            MealType.Lunch,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new("Rotation lunch", "Rotation lunch description"),
                [SupportedLanguage.EnUs.CultureName] = new("Rotation lunch", "Rotation lunch description")
            });
    }
}
