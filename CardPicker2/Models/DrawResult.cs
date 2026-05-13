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
/// <param name="UserMessage">A user-facing message suitable for UI feedback.</param>
/// <param name="LocalizedCard">The selected card projected for the current culture.</param>
/// <param name="StatusKey">The stable message key for localization-aware UI.</param>
public sealed record DrawResult(
    bool Succeeded,
    MealType SelectedMealType,
    Guid? CardId,
    string? Name,
    MealType? MealType,
    string? Description,
    string UserMessage,
    LocalizedMealCardView? LocalizedCard = null,
    string StatusKey = "")
{
    /// <summary>
    /// Gets the display name of the selected card meal type.
    /// </summary>
    public string MealTypeDisplayName => LocalizedCard?.MealTypeDisplayName ?? MealType?.ToDisplayName() ?? string.Empty;

    /// <summary>
    /// Creates a successful draw result from a meal card.
    /// </summary>
    /// <param name="selectedMealType">The meal type submitted by the user.</param>
    /// <param name="card">The selected card.</param>
    /// <returns>A successful draw result.</returns>
    public static DrawResult Success(MealType selectedMealType, MealCard card)
    {
        var content = card.GetContent(SupportedLanguage.ZhTw);
        var localizedCard = new LocalizedMealCardView(
            card.Id,
            card.MealType,
            card.MealType.ToDisplayName(SupportedLanguage.ZhTw),
            content.Name,
            content.Description,
            SupportedLanguage.ZhTw,
            false,
            card.GetMissingTranslationCultures());
        return Success(selectedMealType, card, localizedCard, "已抽出餐點卡牌。", "Draw.Success");
    }

    /// <summary>
    /// Creates a successful draw result from a localized card projection.
    /// </summary>
    /// <param name="selectedMealType">The meal type submitted by the user.</param>
    /// <param name="card">The selected card.</param>
    /// <param name="localizedCard">The selected card projection.</param>
    /// <param name="message">The user-facing success message.</param>
    /// <param name="statusKey">The stable message key.</param>
    /// <returns>A successful draw result.</returns>
    public static DrawResult Success(
        MealType selectedMealType,
        MealCard card,
        LocalizedMealCardView localizedCard,
        string message,
        string statusKey)
    {
        return new DrawResult(
            true,
            selectedMealType,
            card.Id,
            localizedCard.DisplayName,
            card.MealType,
            localizedCard.DisplayDescription,
            message,
            localizedCard,
            statusKey);
    }

    /// <summary>
    /// Creates a failed draw result.
    /// </summary>
    /// <param name="selectedMealType">The meal type submitted by the user.</param>
    /// <param name="message">The failure message.</param>
    /// <param name="statusKey">The stable message key.</param>
    /// <returns>A failed draw result.</returns>
    public static DrawResult Failure(MealType selectedMealType, string message, string statusKey = "Draw.Failure")
    {
        return new DrawResult(false, selectedMealType, null, null, null, null, message, null, statusKey);
    }
}
