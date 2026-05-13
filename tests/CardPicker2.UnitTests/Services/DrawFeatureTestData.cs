using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.Models;

namespace CardPicker2.UnitTests.Services;

public static class DrawFeatureTestData
{
    public static readonly Guid BreakfastCardId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SecondBreakfastCardId = Guid.Parse("11111111-1111-1111-1111-111111111112");
    public static readonly Guid LunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222221");
    public static readonly Guid DinnerCardId = Guid.Parse("33333333-3333-3333-3333-333333333331");
    public static readonly Guid DeletedCardId = Guid.Parse("44444444-4444-4444-4444-444444444441");
    public static readonly Guid FirstOperationId = Guid.Parse("55555555-5555-5555-5555-555555555551");
    public static readonly Guid ReplayOperationId = Guid.Parse("55555555-5555-5555-5555-555555555552");
    public static readonly Guid HistoryRecordId = Guid.Parse("66666666-6666-6666-6666-666666666661");

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<MealCard> ActiveLocalizedCards()
    {
        return new[]
        {
            CreateCard(BreakfastCardId, MealType.Breakfast, "鮪魚蛋餅", "Tuna Egg Crepe"),
            CreateCard(SecondBreakfastCardId, MealType.Breakfast, "鹹粥小菜", "Savory Congee"),
            CreateCard(LunchCardId, MealType.Lunch, "牛肉麵", "Beef Noodle Soup"),
            CreateCard(DinnerCardId, MealType.Dinner, "番茄燉飯", "Tomato Risotto")
        };
    }

    public static CardLibraryDocument SchemaV2Document()
    {
        return new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.BilingualSchemaVersion,
            Cards = ActiveLocalizedCards()
        };
    }

    public static object SchemaV3Document(
        IReadOnlyList<object>? cards = null,
        IReadOnlyList<object>? drawHistory = null)
    {
        return new
        {
            schemaVersion = 3,
            cards = cards ?? SchemaV3Cards(),
            drawHistory = drawHistory ?? Array.Empty<object>()
        };
    }

    public static IReadOnlyList<object> SchemaV3Cards()
    {
        return new[]
        {
            SchemaV3Card(BreakfastCardId, "Breakfast", "Active", "鮪魚蛋餅", "Tuna Egg Crepe"),
            SchemaV3Card(SecondBreakfastCardId, "Breakfast", "Active", "鹹粥小菜", "Savory Congee"),
            SchemaV3Card(LunchCardId, "Lunch", "Active", "牛肉麵", "Beef Noodle Soup"),
            SchemaV3Card(DinnerCardId, "Dinner", "Active", "番茄燉飯", "Tomato Risotto"),
            SchemaV3Card(DeletedCardId, "Dinner", "Deleted", "炙燒飯糰", "Seared Rice Ball", deletedAtUtc: KnownTimestamp())
        };
    }

    public static object SchemaV3Card(
        Guid id,
        string mealType,
        string status,
        string zhTwName,
        string enUsName,
        DateTimeOffset? deletedAtUtc = null)
    {
        return new
        {
            id,
            mealType,
            status,
            deletedAtUtc,
            localizations = new Dictionary<string, object>
            {
                [SupportedLanguage.ZhTw.CultureName] = new
                {
                    name = zhTwName,
                    description = $"{zhTwName} 描述"
                },
                [SupportedLanguage.EnUs.CultureName] = new
                {
                    name = enUsName,
                    description = $"{enUsName} description"
                }
            }
        };
    }

    public static object DrawHistory(
        Guid? id = null,
        Guid? operationId = null,
        string drawMode = "Normal",
        Guid? cardId = null,
        string mealTypeAtDraw = "Breakfast",
        DateTimeOffset? succeededAtUtc = null)
    {
        return new
        {
            id = id ?? HistoryRecordId,
            operationId = operationId ?? FirstOperationId,
            drawMode,
            cardId = cardId ?? BreakfastCardId,
            mealTypeAtDraw,
            succeededAtUtc = succeededAtUtc ?? KnownTimestamp()
        };
    }

    public static string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    public static DateTimeOffset KnownTimestamp()
    {
        return new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero);
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
