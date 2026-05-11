namespace CardPicker2.Models;

/// <summary>
/// Represents the result of one attempted meal draw.
/// </summary>
/// <param name="Succeeded">Whether a valid card was selected.</param>
/// <param name="SelectedMealType">The meal type requested by the user.</param>
/// <param name="CardId">The selected card identifier when the draw succeeds.</param>
/// <param name="Name">The selected meal name when the draw succeeds.</param>
/// <param name="MealType">The selected card meal type when the draw succeeds.</param>
/// <param name="Description">The selected card description when the draw succeeds.</param>
/// <param name="UserMessage">A Traditional Chinese message suitable for UI feedback.</param>
public sealed record DrawResult(
    bool Succeeded,
    MealType SelectedMealType,
    Guid? CardId,
    string? Name,
    MealType? MealType,
    string? Description,
    string UserMessage)
{
    /// <summary>
    /// Gets the Traditional Chinese display name of the selected card meal type.
    /// </summary>
    public string MealTypeDisplayName => MealType?.ToDisplayName() ?? string.Empty;

    /// <summary>
    /// Creates a successful draw result from a meal card.
    /// </summary>
    /// <param name="selectedMealType">The meal type submitted by the user.</param>
    /// <param name="card">The selected card.</param>
    /// <returns>A successful draw result.</returns>
    public static DrawResult Success(MealType selectedMealType, MealCard card)
    {
        return new DrawResult(
            true,
            selectedMealType,
            card.Id,
            card.Name,
            card.MealType,
            card.Description,
            "已抽出餐點卡牌。");
    }

    /// <summary>
    /// Creates a failed draw result.
    /// </summary>
    /// <param name="selectedMealType">The meal type submitted by the user.</param>
    /// <param name="message">The Traditional Chinese failure message.</param>
    /// <returns>A failed draw result.</returns>
    public static DrawResult Failure(MealType selectedMealType, string message)
    {
        return new DrawResult(false, selectedMealType, null, null, null, null, message);
    }
}
