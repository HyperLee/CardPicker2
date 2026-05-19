using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class DrawStatisticsRotationCompatibilityTests
{
    [Fact]
    public void CreateSummary_IncludesLegacyHistoryWithoutRotationSnapshot()
    {
        var card = CreateCard(DrawFeatureTestData.LunchCardId, CardStatus.Active);
        var document = new CardLibraryDocument
        {
            Cards = new[] { card },
            DrawHistory = new[]
            {
                new DrawHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    OperationId = Guid.NewGuid(),
                    DrawMode = DrawMode.Normal,
                    CardId = card.Id,
                    MealTypeAtDraw = card.MealType,
                    SucceededAtUtc = DrawFeatureTestData.KnownTimestamp()
                }
            }
        };
        var service = new DrawStatisticsService(new MealCardLocalizationService());

        var summary = service.CreateSummary(document, SupportedLanguage.ZhTw);

        Assert.Equal(1, summary.TotalSuccessfulDraws);
        var row = Assert.Single(summary.Rows);
        Assert.Equal(1, row.DrawCount);
        Assert.Equal("100%", row.HistoricalProbabilityDisplay);
    }

    [Fact]
    public void RotationService_UsesLegacyMissingSnapshotHistoryForRecentExclusion()
    {
        var card = CreateCard(DrawFeatureTestData.LunchCardId, CardStatus.Active);
        var service = new DrawRotationCooldownService();

        var pool = service.Apply(
            new[] { card },
            new[]
            {
                new DrawHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    OperationId = Guid.NewGuid(),
                    DrawMode = DrawMode.Normal,
                    CardId = card.Id,
                    MealTypeAtDraw = card.MealType,
                    SucceededAtUtc = DrawFeatureTestData.KnownTimestamp()
                }
            },
            RotationCooldownSettings.Default);

        Assert.Empty(pool.PostRotationCards);
        Assert.Equal(new[] { card.Id }, pool.ExcludedCardIds);
    }

    private static MealCard CreateCard(Guid id, CardStatus status)
    {
        return new MealCard(
            id,
            MealType.Lunch,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new("午餐", "描述"),
                [SupportedLanguage.EnUs.CultureName] = new("Lunch", "description")
            },
            status,
            deletedAtUtc: null);
    }
}
