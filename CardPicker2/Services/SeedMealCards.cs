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
            "11111111-1111-1111-1111-111111111114",
            MealType.Breakfast,
            "蔬菜起司蛋餅",
            "軟嫩蛋餅包入高麗菜、玉米與起司，早餐吃起來清爽有飽足感。",
            "Vegetable Cheese Egg Pancake",
            "A soft egg pancake filled with cabbage, corn, and cheese for a fresh but filling breakfast."),
        Create(
            "11111111-1111-1111-1111-111111111115",
            MealType.Breakfast,
            "火腿蛋三明治",
            "烤吐司夾火腿、煎蛋與生菜，適合趕時間時外帶。",
            "Ham and Egg Sandwich",
            "Toasted bread with ham, fried egg, and lettuce for an easy breakfast to go."),
        Create(
            "11111111-1111-1111-1111-111111111116",
            MealType.Breakfast,
            "飯糰豆漿",
            "紫米飯糰包油條、肉鬆與酸菜，搭配一杯溫豆漿。",
            "Rice Roll with Soy Milk",
            "A purple rice roll with cruller, pork floss, and pickled greens served with warm soy milk."),
        Create(
            "11111111-1111-1111-1111-111111111117",
            MealType.Breakfast,
            "蘿蔔糕加蛋",
            "煎到表面微酥的蘿蔔糕加蛋，搭配蒜蓉醬油。",
            "Turnip Cake with Egg",
            "Pan-fried turnip cake with egg and a garlic soy dipping sauce."),
        Create(
            "11111111-1111-1111-1111-111111111118",
            MealType.Breakfast,
            "燒餅油條",
            "酥香燒餅夾油條，搭配熱豆漿或米漿都合適。",
            "Shaobing with Cruller",
            "Flaky sesame flatbread wrapped around a crisp cruller, good with hot soy or rice milk."),
        Create(
            "11111111-1111-1111-1111-111111111119",
            MealType.Breakfast,
            "地瓜優格碗",
            "蒸地瓜、原味優格、堅果與水果，適合想吃輕盈早餐時。",
            "Sweet Potato Yogurt Bowl",
            "Steamed sweet potato with plain yogurt, nuts, and fruit for a lighter breakfast."),
        Create(
            "11111111-1111-1111-1111-111111111120",
            MealType.Breakfast,
            "雞肉蔬菜粥",
            "雞肉絲與蔬菜熬成的溫粥，早上想吃熱食時很舒服。",
            "Chicken Vegetable Congee",
            "Warm congee with shredded chicken and vegetables for a gentle hot breakfast."),
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
            "22222222-2222-2222-2222-222222222224",
            MealType.Lunch,
            "日式豬排飯",
            "酥炸豬排鋪在白飯上，搭配高麗菜絲與味噌湯。",
            "Japanese Pork Cutlet Rice",
            "Crisp pork cutlet over rice with shredded cabbage and miso soup."),
        Create(
            "22222222-2222-2222-2222-222222222225",
            MealType.Lunch,
            "蝦仁炒飯",
            "粒粒分明的蛋炒飯加入蝦仁與青蔥，午餐快速又有香氣。",
            "Shrimp Fried Rice",
            "Egg fried rice with shrimp and scallions for a quick, fragrant lunch."),
        Create(
            "22222222-2222-2222-2222-222222222226",
            MealType.Lunch,
            "泰式打拋豬飯",
            "九層塔炒絞肉搭配白飯與煎蛋，口味鹹香微辣。",
            "Thai Basil Pork Rice",
            "Savory, mildly spicy basil pork over rice with a fried egg."),
        Create(
            "22222222-2222-2222-2222-222222222227",
            MealType.Lunch,
            "越南牛肉河粉",
            "清香牛骨湯、河粉與薄牛肉片，搭配豆芽和檸檬。",
            "Vietnamese Beef Pho",
            "Aromatic beef broth with rice noodles, thin beef slices, bean sprouts, and lime."),
        Create(
            "22222222-2222-2222-2222-222222222228",
            MealType.Lunch,
            "烤鮭魚定食",
            "烤鮭魚搭配白飯、味噌湯與小菜，午餐吃得均衡。",
            "Grilled Salmon Set",
            "Grilled salmon with rice, miso soup, and small sides for a balanced lunch."),
        Create(
            "22222222-2222-2222-2222-222222222229",
            MealType.Lunch,
            "三杯雞飯",
            "薑片、蒜頭與九層塔炒雞肉，搭配熱白飯。",
            "Three-Cup Chicken Rice",
            "Chicken cooked with ginger, garlic, and basil, served over hot rice."),
        Create(
            "22222222-2222-2222-2222-222222222230",
            MealType.Lunch,
            "菇菇蔬食便當",
            "杏鮑菇、青花菜與豆干搭配糙米飯，適合想吃蔬食午餐時。",
            "Mushroom Vegetable Bento",
            "King oyster mushrooms, broccoli, tofu, and brown rice for a vegetable-forward lunch."),
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
            "A lighter curry with potatoes, carrots, and mushrooms."),
        Create(
            "33333333-3333-3333-3333-333333333334",
            MealType.Dinner,
            "麻油雞麵線",
            "麻油香氣的雞湯搭配麵線，天氣涼時很適合。",
            "Sesame Oil Chicken Noodles",
            "Sesame oil chicken soup with thin wheat noodles, especially comforting on cool nights."),
        Create(
            "33333333-3333-3333-3333-333333333335",
            MealType.Dinner,
            "韓式拌飯",
            "白飯鋪上蔬菜、牛肉、泡菜與半熟蛋，拌入辣醬。",
            "Korean Bibimbap",
            "Rice topped with vegetables, beef, kimchi, and a soft egg, mixed with gochujang."),
        Create(
            "33333333-3333-3333-3333-333333333336",
            MealType.Dinner,
            "紅酒燉牛肉",
            "牛肉、紅蘿蔔與洋蔥慢燉，搭配麵包或馬鈴薯泥。",
            "Red Wine Beef Stew",
            "Slow-braised beef with carrots and onions, served with bread or mashed potatoes."),
        Create(
            "33333333-3333-3333-3333-333333333337",
            MealType.Dinner,
            "香煎鮭魚飯",
            "鮭魚煎到表皮酥香，搭配白飯、海苔與燙青菜。",
            "Pan-Seared Salmon Rice",
            "Crisp-skinned salmon with rice, nori, and blanched greens."),
        Create(
            "33333333-3333-3333-3333-333333333338",
            MealType.Dinner,
            "滷肉飯套餐",
            "滷肉飯搭配滷蛋、青菜與貢丸湯，晚餐簡單滿足。",
            "Braised Pork Rice Set",
            "Braised pork rice with a soy egg, greens, and pork ball soup for a simple dinner."),
        Create(
            "33333333-3333-3333-3333-333333333339",
            MealType.Dinner,
            "蔬菜豆腐鍋",
            "豆腐、菇類與時蔬煮成清爽鍋物，適合想吃暖胃晚餐時。",
            "Vegetable Tofu Hot Pot",
            "A light hot pot with tofu, mushrooms, and seasonal vegetables for a warming dinner."),
        Create(
            "33333333-3333-3333-3333-333333333340",
            MealType.Dinner,
            "海鮮粥",
            "白粥加入魚片、蝦仁與蛤蜊，晚餐想吃清淡時很合適。",
            "Seafood Congee",
            "Rice congee with fish slices, shrimp, and clams for a lighter dinner.")
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
            Cards = All,
            DrawHistory = Array.Empty<DrawHistoryRecord>()
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
