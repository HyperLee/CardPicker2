using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class DrawStatisticsServiceTests
{
    [Fact]
    public void CreateSummary_WithKnownHistory_ProjectsCountsAndHistoricalProbability()
    {
        var service = new DrawStatisticsService(new MealCardLocalizationService());
        var document = CreateDocumentWithKnownHistory();

        var summary = service.CreateSummary(document, SupportedLanguage.ZhTw);

        Assert.True(summary.HasHistory);
        Assert.Equal(50, summary.TotalSuccessfulDraws);

        var breakfast = summary.Rows.Single(row => row.CardId == DrawFeatureTestData.BreakfastCardId);
        Assert.Equal(20, breakfast.DrawCount);
        Assert.Equal(0.4m, breakfast.HistoricalProbability);
        Assert.Equal("40%", breakfast.HistoricalProbabilityDisplay);

        var activeZero = summary.Rows.Single(row => row.CardId == DrawFeatureTestData.SecondBreakfastCardId);
        Assert.Equal(CardStatus.Active, activeZero.Status);
        Assert.Equal(0, activeZero.DrawCount);
        Assert.Equal(0m, activeZero.HistoricalProbability);
        Assert.Equal("0%", activeZero.HistoricalProbabilityDisplay);
    }

    [Fact]
    public void CreateSummary_WithZeroHistory_ReturnsEmptyStateWithoutMisleadingProbabilities()
    {
        var service = new DrawStatisticsService(new MealCardLocalizationService());
        var document = new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = DrawFeatureTestData.ActiveLocalizedCards(),
            DrawHistory = Array.Empty<DrawHistoryRecord>()
        };

        var summary = service.CreateSummary(document, SupportedLanguage.ZhTw);

        Assert.False(summary.HasHistory);
        Assert.Equal(0, summary.TotalSuccessfulDraws);
        Assert.Empty(summary.Rows);
        Assert.Equal("Statistics.Empty", summary.StatusKey);
    }

    [Fact]
    public void CreateSummary_IncludesDeletedCardsOnlyWhenTheyHaveHistory()
    {
        var service = new DrawStatisticsService(new MealCardLocalizationService());
        var document = CreateDocumentWithKnownHistory();

        var summary = service.CreateSummary(document, SupportedLanguage.ZhTw);

        var deleted = summary.Rows.Single(row => row.CardId == DrawFeatureTestData.DeletedCardId);
        Assert.Equal(CardStatus.Deleted, deleted.Status);
        Assert.Equal(10, deleted.DrawCount);
        Assert.Equal(0.2m, deleted.HistoricalProbability);
        Assert.DoesNotContain(summary.Rows, row => row.DisplayName.Contains("未抽中刪除", StringComparison.Ordinal));
    }

    private static CardLibraryDocument CreateDocumentWithKnownHistory()
    {
        var cards = DrawFeatureTestData.ActiveLocalizedCards()
            .Concat(new[]
            {
                new MealCard(
                    DrawFeatureTestData.DeletedCardId,
                    MealType.Dinner,
                    new Dictionary<string, MealCardLocalizedContent>
                    {
                        [SupportedLanguage.ZhTw.CultureName] = new("炙燒飯糰", "保留歷史"),
                        [SupportedLanguage.EnUs.CultureName] = new("Seared Rice Ball", "Retained history")
                    },
                    CardStatus.Deleted,
                    DrawFeatureTestData.KnownTimestamp()),
                new MealCard(
                    Guid.Parse("77777777-7777-7777-7777-777777777771"),
                    MealType.Lunch,
                    new Dictionary<string, MealCardLocalizedContent>
                    {
                        [SupportedLanguage.ZhTw.CultureName] = new("未抽中刪除", "不應列出"),
                        [SupportedLanguage.EnUs.CultureName] = new("Deleted Without History", "Should not list")
                    },
                    CardStatus.Deleted,
                    DrawFeatureTestData.KnownTimestamp())
            })
            .ToList();

        return new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = cards,
            DrawHistory = CreateHistory()
        };
    }

    private static IReadOnlyList<DrawHistoryRecord> CreateHistory()
    {
        var records = new List<DrawHistoryRecord>();
        Add(records, DrawFeatureTestData.BreakfastCardId, MealType.Breakfast, 20);
        Add(records, DrawFeatureTestData.LunchCardId, MealType.Lunch, 15);
        Add(records, DrawFeatureTestData.DeletedCardId, MealType.Dinner, 10);
        Add(records, DrawFeatureTestData.DinnerCardId, MealType.Dinner, 5);
        return records;
    }

    private static void Add(List<DrawHistoryRecord> records, Guid cardId, MealType mealType, int count)
    {
        for (var index = 0; index < count; index++)
        {
            records.Add(new DrawHistoryRecord
            {
                Id = Guid.NewGuid(),
                OperationId = Guid.NewGuid(),
                DrawMode = DrawMode.Random,
                CardId = cardId,
                MealTypeAtDraw = mealType,
                SucceededAtUtc = DrawFeatureTestData.KnownTimestamp().AddMinutes(records.Count)
            });
        }
    }
}
