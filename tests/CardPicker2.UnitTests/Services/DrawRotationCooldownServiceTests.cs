using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class DrawRotationCooldownServiceTests
{
    [Fact]
    public void Apply_UsesRecentSuccessfulHistoryOrderedByTimestampThenPersistenceOrder()
    {
        var service = new DrawRotationCooldownService();
        var candidates = CreateLunchCandidates();
        var timestamp = DrawFeatureTestData.KnownTimestamp();
        var history = new[]
        {
            History(DrawFeatureTestData.VegetarianLunchCardId, timestamp.AddMinutes(-5)),
            History(DrawFeatureTestData.LunchCardId, timestamp),
            History(DrawFeatureTestData.LowPriceLunchCardId, timestamp)
        };

        var pool = service.Apply(candidates, history, new RotationCooldownSettings(true, 2));

        Assert.Equal(
            new[] { DrawFeatureTestData.LunchCardId, DrawFeatureTestData.LowPriceLunchCardId },
            pool.ExcludedCardIds.OrderBy(id => id));
        Assert.Equal(new[] { DrawFeatureTestData.VegetarianLunchCardId }, pool.PostRotationCards.Select(card => card.Id));
        Assert.Equal(3, pool.Snapshot.PreRotationCandidateCount);
        Assert.Equal(2, pool.Snapshot.ExcludedCandidateCount);
        Assert.Equal(1, pool.Snapshot.PostRotationCandidateCount);
    }

    [Fact]
    public void Apply_DeduplicatesRecentCardIdsAndOnlyExcludesIdsPresentInCandidatePool()
    {
        var service = new DrawRotationCooldownService();
        var candidates = CreateLunchCandidates();
        var history = new[]
        {
            History(DrawFeatureTestData.LunchCardId, DrawFeatureTestData.KnownTimestamp().AddMinutes(3)),
            History(DrawFeatureTestData.LunchCardId, DrawFeatureTestData.KnownTimestamp().AddMinutes(2)),
            History(DrawFeatureTestData.DeletedCardId, DrawFeatureTestData.KnownTimestamp().AddMinutes(1)),
            History(Guid.Parse("99999999-9999-9999-9999-999999999999"), DrawFeatureTestData.KnownTimestamp())
        };

        var pool = service.Apply(candidates, history, new RotationCooldownSettings(true, 4));

        Assert.Equal(new[] { DrawFeatureTestData.LunchCardId }, pool.ExcludedCardIds);
        Assert.DoesNotContain(pool.PostRotationCards, card => card.Id == DrawFeatureTestData.LunchCardId);
        Assert.Equal(2, pool.PostRotationCards.Count);
    }

    [Fact]
    public void Apply_WhenSettingsInactive_DoesNotExcludeRecentHistory()
    {
        var service = new DrawRotationCooldownService();
        var candidates = CreateLunchCandidates();
        var history = new[]
        {
            History(DrawFeatureTestData.LunchCardId, DrawFeatureTestData.KnownTimestamp())
        };

        var pool = service.Apply(candidates, history, new RotationCooldownSettings(true, 0));

        Assert.Empty(pool.ExcludedCardIds);
        Assert.Equal(candidates.Select(card => card.Id), pool.PostRotationCards.Select(card => card.Id));
        Assert.Equal(3, pool.Snapshot.PreRotationCandidateCount);
        Assert.Equal(0, pool.Snapshot.ExcludedCandidateCount);
        Assert.Equal(3, pool.Snapshot.PostRotationCandidateCount);
    }

    [Fact]
    public void Apply_AfterNormalMealAndMetadataFiltering_OnlyExcludesRecentIdsInThatPool()
    {
        var service = new DrawRotationCooldownService();
        var basePool = CreateLunchCandidates()
            .Where(card => card.Id == DrawFeatureTestData.LowPriceLunchCardId || card.Id == DrawFeatureTestData.VegetarianLunchCardId)
            .ToList();
        var history = new[]
        {
            History(DrawFeatureTestData.LunchCardId, DrawFeatureTestData.KnownTimestamp().AddMinutes(3)),
            History(DrawFeatureTestData.LowPriceLunchCardId, DrawFeatureTestData.KnownTimestamp().AddMinutes(2)),
            History(DrawFeatureTestData.LowPriceLunchCardId, DrawFeatureTestData.KnownTimestamp().AddMinutes(1))
        };

        var pool = service.Apply(basePool, history, RotationCooldownSettings.Default);

        Assert.Equal(new[] { DrawFeatureTestData.LowPriceLunchCardId }, pool.ExcludedCardIds);
        Assert.Equal(new[] { DrawFeatureTestData.VegetarianLunchCardId }, pool.PostRotationCards.Select(card => card.Id));
        Assert.Equal(1m, pool.NominalProbability);
    }

    [Fact]
    public void Apply_ExcludesEditedActiveCardByStableIdAndIgnoresDeletedRecentCard()
    {
        var service = new DrawRotationCooldownService();
        var editedActiveCard = CreateCard(DrawFeatureTestData.LunchCardId);
        var otherActiveCard = CreateCard(DrawFeatureTestData.LowPriceLunchCardId);
        var history = new[]
        {
            History(DrawFeatureTestData.LunchCardId, DrawFeatureTestData.KnownTimestamp().AddMinutes(2)),
            History(DrawFeatureTestData.DeletedCardId, DrawFeatureTestData.KnownTimestamp().AddMinutes(1))
        };

        var pool = service.Apply(
            new[] { editedActiveCard, otherActiveCard },
            history,
            RotationCooldownSettings.Default);

        Assert.Equal(new[] { DrawFeatureTestData.LunchCardId }, pool.ExcludedCardIds);
        Assert.Equal(new[] { DrawFeatureTestData.LowPriceLunchCardId }, pool.PostRotationCards.Select(card => card.Id));
    }

    private static IReadOnlyList<MealCard> CreateLunchCandidates()
    {
        return new[]
        {
            CreateCard(DrawFeatureTestData.LunchCardId),
            CreateCard(DrawFeatureTestData.LowPriceLunchCardId),
            CreateCard(DrawFeatureTestData.VegetarianLunchCardId)
        };
    }

    private static MealCard CreateCard(Guid id)
    {
        return new MealCard(
            id,
            MealType.Lunch,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new(id.ToString(), "描述"),
                [SupportedLanguage.EnUs.CultureName] = new(id.ToString(), "description")
            });
    }

    private static DrawHistoryRecord History(Guid cardId, DateTimeOffset succeededAtUtc)
    {
        return new DrawHistoryRecord
        {
            Id = Guid.NewGuid(),
            OperationId = Guid.NewGuid(),
            DrawMode = DrawMode.Normal,
            CardId = cardId,
            MealTypeAtDraw = MealType.Lunch,
            SucceededAtUtc = succeededAtUtc
        };
    }
}
