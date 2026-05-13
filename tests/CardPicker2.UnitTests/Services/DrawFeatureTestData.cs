using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.Models;

namespace CardPicker2.UnitTests.Services;

public static class DrawFeatureTestData
{
    public static readonly Guid BreakfastCardId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SecondBreakfastCardId = Guid.Parse("11111111-1111-1111-1111-111111111112");
    public static readonly Guid LunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222221");
    public static readonly Guid LowPriceLunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid VegetarianLunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222223");
    public static readonly Guid DinnerCardId = Guid.Parse("33333333-3333-3333-3333-333333333331");
    public static readonly Guid SpicyDinnerCardId = Guid.Parse("33333333-3333-3333-3333-333333333332");
    public static readonly Guid MetadataMissingDinnerCardId = Guid.Parse("33333333-3333-3333-3333-333333333333");
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

    public static object SchemaV4Document(
        IReadOnlyList<object>? cards = null,
        IReadOnlyList<object>? drawHistory = null)
    {
        return new
        {
            schemaVersion = 4,
            cards = cards ?? SchemaV4Cards(),
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

    public static IReadOnlyList<object> SchemaV4Cards()
    {
        return new[]
        {
            SchemaV4Card(
                BreakfastCardId,
                "Breakfast",
                "Active",
                "鮪魚蛋餅",
                "Tuna Egg Crepe",
                CompleteDecisionMetadata(
                    tags: new[] { "早餐", "蛋餅", "快速" },
                    priceRange: "Low",
                    preparationTimeRange: "Quick",
                    dietaryPreferences: new[] { "TakeoutFriendly" },
                    spiceLevel: "None")),
            SchemaV4Card(
                SecondBreakfastCardId,
                "Breakfast",
                "Active",
                "鹹粥小菜",
                "Savory Congee",
                PartialDecisionMetadata(tags: new[] { "清淡", "粥品" }, preparationTimeRange: "Standard")),
            SchemaV4Card(
                LunchCardId,
                "Lunch",
                "Active",
                "牛肉麵",
                "Beef Noodle Soup",
                CompleteDecisionMetadata(
                    tags: new[] { "麵食", "熱湯" },
                    priceRange: "Medium",
                    preparationTimeRange: "Standard",
                    dietaryPreferences: new[] { "HeavyFlavor" },
                    spiceLevel: "Mild")),
            SchemaV4Card(
                LowPriceLunchCardId,
                "Lunch",
                "Active",
                "滷肉飯便當",
                "Braised Pork Rice Bento",
                CompleteDecisionMetadata(
                    tags: new[] { "便當", "快速", "外帶" },
                    priceRange: "Low",
                    preparationTimeRange: "Quick",
                    dietaryPreferences: new[] { "TakeoutFriendly", "HeavyFlavor" },
                    spiceLevel: "None")),
            SchemaV4Card(
                VegetarianLunchCardId,
                "Lunch",
                "Active",
                "菇菇蔬食便當",
                "Mushroom Vegetable Bento",
                CompleteDecisionMetadata(
                    tags: new[] { "蔬食", "便當", "清淡" },
                    priceRange: "Medium",
                    preparationTimeRange: "Quick",
                    dietaryPreferences: new[] { "Vegetarian", "Light", "TakeoutFriendly" },
                    spiceLevel: "None")),
            SchemaV4Card(
                DinnerCardId,
                "Dinner",
                "Active",
                "番茄燉飯",
                "Tomato Risotto",
                PartialDecisionMetadata(priceRange: "High", dietaryPreferences: new[] { "Vegetarian" })),
            SchemaV4Card(
                SpicyDinnerCardId,
                "Dinner",
                "Active",
                "麻辣乾拌麵",
                "Spicy Tossed Noodles",
                CompleteDecisionMetadata(
                    tags: new[] { "麵食", "重口味", "辣" },
                    priceRange: "Low",
                    preparationTimeRange: "Quick",
                    dietaryPreferences: new[] { "HeavyFlavor" },
                    spiceLevel: "Hot")),
            SchemaV4Card(
                MetadataMissingDinnerCardId,
                "Dinner",
                "Active",
                "家常炒飯",
                "Home-Style Fried Rice",
                decisionMetadata: null),
            SchemaV4Card(
                DeletedCardId,
                "Dinner",
                "Deleted",
                "炙燒飯糰",
                "Seared Rice Ball",
                CompleteDecisionMetadata(
                    tags: new[] { "飯糰", "外帶" },
                    priceRange: "Low",
                    preparationTimeRange: "Quick",
                    dietaryPreferences: new[] { "TakeoutFriendly" },
                    spiceLevel: "None"),
                deletedAtUtc: KnownTimestamp())
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

    public static object SchemaV4Card(
        Guid id,
        string mealType,
        string status,
        string zhTwName,
        string enUsName,
        object? decisionMetadata,
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
            },
            decisionMetadata
        };
    }

    public static object CompleteDecisionMetadata(
        IReadOnlyList<string>? tags = null,
        string priceRange = "Low",
        string preparationTimeRange = "Quick",
        IReadOnlyList<string>? dietaryPreferences = null,
        string spiceLevel = "None")
    {
        return new
        {
            tags = tags ?? new[] { "快速", "外帶" },
            priceRange,
            preparationTimeRange,
            dietaryPreferences = dietaryPreferences ?? new[] { "TakeoutFriendly" },
            spiceLevel
        };
    }

    public static object PartialDecisionMetadata(
        IReadOnlyList<string>? tags = null,
        string? priceRange = null,
        string? preparationTimeRange = null,
        IReadOnlyList<string>? dietaryPreferences = null,
        string? spiceLevel = null)
    {
        return new
        {
            tags = tags ?? Array.Empty<string>(),
            priceRange,
            preparationTimeRange,
            dietaryPreferences = dietaryPreferences ?? Array.Empty<string>(),
            spiceLevel
        };
    }

    public static object DecisionMetadataWithDuplicateAndBlankTags()
    {
        return CompleteDecisionMetadata(tags: new[] { "  便當 ", "便當", "Bento", "bento", "   " });
    }

    public static object DecisionMetadataWithInvalidEnum()
    {
        return CompleteDecisionMetadata(priceRange: "Premium", spiceLevel: "Extreme");
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
