namespace CardPicker2.Models;

/// <summary>
/// Defines the supported meal periods for a meal card.
/// </summary>
public enum MealType
{
    /// <summary>
    /// Breakfast cards.
    /// </summary>
    Breakfast = 0,

    /// <summary>
    /// Lunch cards.
    /// </summary>
    Lunch = 1,

    /// <summary>
    /// Dinner cards.
    /// </summary>
    Dinner = 2
}

/// <summary>
/// Provides display helpers for <see cref="MealType"/>.
/// </summary>
public static class MealTypeExtensions
{
    /// <summary>
    /// Returns the Traditional Chinese display name for the meal type.
    /// </summary>
    /// <param name="mealType">The meal type to display.</param>
    /// <returns>A Traditional Chinese label.</returns>
    public static string ToDisplayName(this MealType mealType)
    {
        return mealType switch
        {
            MealType.Breakfast => "早餐",
            MealType.Lunch => "午餐",
            MealType.Dinner => "晚餐",
            _ => "未知餐別"
        };
    }
}
