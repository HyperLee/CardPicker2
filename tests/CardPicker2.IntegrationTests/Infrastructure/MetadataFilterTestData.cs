using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Infrastructure;

public static class MetadataFilterTestData
{
    public static readonly Guid VegetarianLunchCardId = Guid.Parse("22222222-2222-2222-2222-222222222223");
    public static readonly Guid MissingMetadataDinnerCardId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static object SchemaV4Document()
    {
        return new
        {
            schemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            cards = new[]
            {
                Card(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Breakfast",
                    "鮪魚蛋餅",
                    "Tuna Egg Crepe",
                    Metadata(
                        new[] { "早餐", "蛋餅" },
                        "Low",
                        "Quick",
                        new[] { "TakeoutFriendly" },
                        "None")),
                Card(
                    VegetarianLunchCardId,
                    "Lunch",
                    "菇菇蔬食便當",
                    "Mushroom Vegetable Bento",
                    Metadata(
                        new[] { "蔬食", "便當", "Bento" },
                        "Medium",
                        "Quick",
                        new[] { "Vegetarian", "TakeoutFriendly", "Light" },
                        "None")),
                Card(
                    Guid.Parse("33333333-3333-3333-3333-333333333332"),
                    "Dinner",
                    "麻辣乾拌麵",
                    "Spicy Tossed Noodles",
                    Metadata(
                        new[] { "麵食", "辣" },
                        "Low",
                        "Quick",
                        new[] { "HeavyFlavor" },
                        "Hot")),
                Card(
                    MissingMetadataDinnerCardId,
                    "Dinner",
                    "家常炒飯",
                    "Home-Style Fried Rice",
                    metadata: null)
            },
            drawHistory = Array.Empty<object>()
        };
    }

    private static object Card(Guid id, string mealType, string zhTwName, string enUsName, object? metadata)
    {
        return new
        {
            id,
            mealType,
            status = "Active",
            deletedAtUtc = (DateTimeOffset?)null,
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
            decisionMetadata = metadata
        };
    }

    private static object Metadata(
        IReadOnlyList<string> tags,
        string priceRange,
        string preparationTimeRange,
        IReadOnlyList<string> dietaryPreferences,
        string spiceLevel)
    {
        return new
        {
            tags,
            priceRange,
            preparationTimeRange,
            dietaryPreferences,
            spiceLevel
        };
    }
}
