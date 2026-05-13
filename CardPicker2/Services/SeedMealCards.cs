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
        Create(
            "11111111-1111-1111-1111-111111111111",
            MealType.Breakfast,
            "鮪魚蛋餅",
            "附近早餐店的鮪魚蛋餅，加一杯無糖豆漿。",
            "Tuna Egg Pancake",
            "A tuna egg pancake from the nearby breakfast shop with unsweetened soy milk."),
        Create(
            "11111111-1111-1111-1111-111111111112",
            MealType.Breakfast,
            "鹹粥小菜",
            "溫熱鹹粥搭配滷蛋與青菜，適合想吃清爽早餐時。",
            "Savory Rice Porridge",
            "Warm savory rice porridge with a braised egg and greens for a lighter breakfast."),
        Create(
            "11111111-1111-1111-1111-111111111113",
            MealType.Breakfast,
            "花生厚片",
            "烤到邊緣酥脆的花生厚片，搭配熱紅茶。",
            "Peanut Thick Toast",
            "Crisp-edged thick toast with peanut spread and hot black tea."),
        Create(
            "22222222-2222-2222-2222-222222222221",
            MealType.Lunch,
            "清燉牛肉麵",
            "湯頭清爽的牛肉麵，午餐想吃熱食時很穩定。",
            "Clear Broth Beef Noodle Soup",
            "A steady hot lunch with clear broth, beef, and noodles."),
        Create(
            "22222222-2222-2222-2222-222222222222",
            MealType.Lunch,
            "雞腿便當",
            "烤雞腿便當搭配三樣配菜，適合需要吃飽的中午。",
            "Chicken Leg Bento",
            "Roasted chicken leg bento with three sides for a filling lunch."),
        Create(
            "22222222-2222-2222-2222-222222222223",
            MealType.Lunch,
            "番茄義大利麵",
            "酸甜番茄醬汁與蔬菜，適合想換口味的午餐。",
            "Tomato Pasta",
            "Sweet-tart tomato sauce and vegetables when lunch needs a change of pace."),
        Create(
            "33333333-3333-3333-3333-333333333331",
            MealType.Dinner,
            "壽喜燒鍋",
            "晚餐吃暖鍋，搭配青菜、豆腐與薄切牛肉。",
            "Sukiyaki Hot Pot",
            "A warm dinner pot with greens, tofu, and thinly sliced beef."),
        Create(
            "33333333-3333-3333-3333-333333333332",
            MealType.Dinner,
            "蒜香雞肉飯",
            "蒜香煎雞腿配白飯與燙青菜，晚餐簡單但有份量。",
            "Garlic Chicken Rice",
            "Garlic pan-seared chicken with rice and blanched greens for a simple dinner."),
        Create(
            "33333333-3333-3333-3333-333333333333",
            MealType.Dinner,
            "蔬菜咖哩",
            "馬鈴薯、紅蘿蔔與菇類的蔬菜咖哩，適合不想吃太油時。",
            "Vegetable Curry",
            "A lighter curry with potatoes, carrots, and mushrooms.")
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

    private static MealCard Create(
        string id,
        MealType mealType,
        string zhName,
        string zhDescription,
        string enName,
        string enDescription)
    {
        return new MealCard(
            Guid.Parse(id),
            mealType,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new(zhName, zhDescription),
                [SupportedLanguage.EnUs.CultureName] = new(enName, enDescription)
            });
    }
}
