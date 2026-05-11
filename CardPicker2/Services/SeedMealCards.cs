using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Provides the default local meal-card library for first run.
/// </summary>
public static class SeedMealCards
{
    /// <summary>
    /// Gets the seed cards used when the persisted JSON file is missing.
    /// </summary>
    public static IReadOnlyList<MealCard> All { get; } = new[]
    {
        new MealCard(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "鮪魚蛋餅",
            MealType.Breakfast,
            "附近早餐店的鮪魚蛋餅，加一杯無糖豆漿。"),
        new MealCard(
            Guid.Parse("11111111-1111-1111-1111-111111111112"),
            "鹹粥小菜",
            MealType.Breakfast,
            "溫熱鹹粥搭配滷蛋與青菜，適合想吃清爽早餐時。"),
        new MealCard(
            Guid.Parse("11111111-1111-1111-1111-111111111113"),
            "花生厚片",
            MealType.Breakfast,
            "烤到邊緣酥脆的花生厚片，搭配熱紅茶。"),
        new MealCard(
            Guid.Parse("22222222-2222-2222-2222-222222222221"),
            "清燉牛肉麵",
            MealType.Lunch,
            "湯頭清爽的牛肉麵，午餐想吃熱食時很穩定。"),
        new MealCard(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "雞腿便當",
            MealType.Lunch,
            "烤雞腿便當搭配三樣配菜，適合需要吃飽的中午。"),
        new MealCard(
            Guid.Parse("22222222-2222-2222-2222-222222222223"),
            "番茄義大利麵",
            MealType.Lunch,
            "酸甜番茄醬汁與蔬菜，適合想換口味的午餐。"),
        new MealCard(
            Guid.Parse("33333333-3333-3333-3333-333333333331"),
            "壽喜燒鍋",
            MealType.Dinner,
            "晚餐吃暖鍋，搭配青菜、豆腐與薄切牛肉。"),
        new MealCard(
            Guid.Parse("33333333-3333-3333-3333-333333333332"),
            "蒜香雞肉飯",
            MealType.Dinner,
            "蒜香煎雞腿配白飯與燙青菜，晚餐簡單但有份量。"),
        new MealCard(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "蔬菜咖哩",
            MealType.Dinner,
            "馬鈴薯、紅蘿蔔與菇類的蔬菜咖哩，適合不想吃太油時。")
    };

    /// <summary>
    /// Creates a new seed document.
    /// </summary>
    /// <returns>A card-library document containing the default cards.</returns>
    public static CardLibraryDocument CreateDocument()
    {
        return new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = All
        };
    }
}