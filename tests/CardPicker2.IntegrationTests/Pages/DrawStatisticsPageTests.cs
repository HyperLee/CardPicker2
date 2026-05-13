using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class DrawStatisticsPageTests : IDisposable
{
    private static readonly Guid BreakfastCardId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondBreakfastCardId = Guid.Parse("11111111-1111-1111-1111-111111111112");
    private static readonly Guid LunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222221");
    private static readonly Guid DinnerCardId = Guid.Parse("33333333-3333-3333-3333-333333333331");
    private static readonly Guid DeletedCardId = Guid.Parse("44444444-4444-4444-4444-444444444441");
    private static readonly DateTimeOffset KnownTimestamp = new(2026, 5, 13, 8, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task GetHome_WithKnownHistory_RendersTotalAndStatisticsTable()
    {
        await _factory.WriteLibraryDocumentAsync(CreateDocumentWithKnownHistory(), JsonOptions);
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("總成功抽取次數", html);
        Assert.Contains("50", html);
        Assert.Contains("卡牌名稱", html);
        Assert.Contains("歷史機率", html);
        Assert.Contains("鮪魚蛋餅", html);
        Assert.Contains("40%", html);
        Assert.Contains("炙燒飯糰", html);
        Assert.Contains("已刪除", html);
    }

    [Fact]
    public async Task GetHome_WithZeroHistory_RendersEmptyStateWithoutPerCardZeroPercent()
    {
        await _factory.WriteLibraryDocumentAsync(new
        {
            schemaVersion = 3,
            cards = CreateActiveCardObjects(),
            drawHistory = Array.Empty<object>()
        }, JsonOptions);
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("尚未有成功抽卡紀錄", html);
        Assert.DoesNotContain("<td>0%</td>", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static CardLibraryDocument CreateDocumentWithKnownHistory()
    {
        var cards = ActiveLocalizedCards()
            .Concat(new[]
            {
                new MealCard(
                    DeletedCardId,
                    MealType.Dinner,
                    new Dictionary<string, MealCardLocalizedContent>
                    {
                        [SupportedLanguage.ZhTw.CultureName] = new("炙燒飯糰", "保留歷史"),
                        [SupportedLanguage.EnUs.CultureName] = new("Seared Rice Ball", "Retained history")
                    },
                    CardStatus.Deleted,
                    KnownTimestamp)
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
        Add(records, BreakfastCardId, MealType.Breakfast, 20);
        Add(records, LunchCardId, MealType.Lunch, 15);
        Add(records, DeletedCardId, MealType.Dinner, 10);
        Add(records, DinnerCardId, MealType.Dinner, 5);
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
                SucceededAtUtc = KnownTimestamp.AddMinutes(records.Count)
            });
        }
    }

    private static IReadOnlyList<MealCard> ActiveLocalizedCards()
    {
        return new[]
        {
            CreateCard(BreakfastCardId, MealType.Breakfast, "鮪魚蛋餅", "Tuna Egg Crepe"),
            CreateCard(SecondBreakfastCardId, MealType.Breakfast, "鹹粥小菜", "Savory Congee"),
            CreateCard(LunchCardId, MealType.Lunch, "牛肉麵", "Beef Noodle Soup"),
            CreateCard(DinnerCardId, MealType.Dinner, "番茄燉飯", "Tomato Risotto")
        };
    }

    private static object[] CreateActiveCardObjects()
    {
        return ActiveLocalizedCards()
            .Select(card => new
            {
                id = card.Id,
                mealType = card.MealType,
                status = card.Status,
                deletedAtUtc = card.DeletedAtUtc,
                localizations = card.Localizations
            })
            .Cast<object>()
            .ToArray();
    }

    private static MealCard CreateCard(Guid id, MealType mealType, string zhTwName, string enUsName)
    {
        return new MealCard(
            id,
            mealType,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new(zhTwName, $"{zhTwName} 描述"),
                [SupportedLanguage.EnUs.CultureName] = new(enUsName, $"{enUsName} description")
            });
    }
}
