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
/// <param name="OperationId">The operation identifier submitted with the draw.</param>
/// <param name="DrawMode">The mode used to build the candidate pool.</param>
/// <param name="RequestedMealType">The submitted meal type for normal mode; random mode uses <see langword="null"/>.</param>
/// <param name="IsReplay">Whether this result replays an existing successful operation.</param>
/// <param name="AppliedFilters">The filters applied to the candidate pool.</param>
/// <param name="FilterSummary">A localized summary of the applied filters.</param>
/// <param name="FilteredPoolSize">The number of cards in the filtered candidate pool.</param>
/// <example>
/// <code>
/// var result = DrawResult.Failure(operation, "Coin required.", "Draw.CoinRequired");
/// </code>
/// </example>
public sealed record DrawResult(
    bool Succeeded,
    MealType SelectedMealType,
    Guid? CardId,
    string? Name,
    MealType? MealType,
    string? Description,
    string UserMessage,
    LocalizedMealCardView? LocalizedCard = null,
    string StatusKey = "",
    Guid OperationId = default,
    DrawMode DrawMode = DrawMode.Normal,
    MealType? RequestedMealType = null,
    bool IsReplay = false,
    CardFilterCriteria? AppliedFilters = null,
    FilterSummary? FilterSummary = null,
    int? FilteredPoolSize = null)
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
        string statusKey,
        int? filteredPoolSize = null,
        CardFilterCriteria? appliedFilters = null,
        FilterSummary? filterSummary = null)
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
            statusKey,
            RequestedMealType: selectedMealType,
            AppliedFilters: appliedFilters,
            FilterSummary: filterSummary,
            FilteredPoolSize: filteredPoolSize);
    }

    /// <summary>
    /// Creates a successful draw result from an idempotent operation.
    /// </summary>
    /// <param name="operation">The submitted draw operation.</param>
    /// <param name="card">The selected card.</param>
    /// <param name="localizedCard">The selected card projection.</param>
    /// <param name="message">The user-facing success message.</param>
    /// <param name="statusKey">The stable message key.</param>
    /// <param name="isReplay">Whether the result replays an existing history record.</param>
    /// <returns>A successful draw result.</returns>
    public static DrawResult Success(
        DrawOperation operation,
        MealCard card,
        LocalizedMealCardView localizedCard,
        string message,
        string statusKey,
        bool isReplay = false,
        int? filteredPoolSize = null,
        CardFilterCriteria? appliedFilters = null,
        FilterSummary? filterSummary = null)
    {
        var requestedMealType = operation.Mode == DrawMode.Normal ? operation.MealType : null;
        return new DrawResult(
            true,
            requestedMealType ?? card.MealType,
            card.Id,
            localizedCard.DisplayName,
            card.MealType,
            localizedCard.DisplayDescription,
            message,
            localizedCard,
            statusKey,
            operation.OperationId,
            operation.Mode,
            requestedMealType,
            isReplay,
            appliedFilters,
            filterSummary,
            filteredPoolSize);
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
        return new DrawResult(
            false,
            selectedMealType,
            null,
            null,
            null,
            null,
            message,
            null,
            statusKey,
            RequestedMealType: selectedMealType);
    }

    /// <summary>
    /// Creates a failed draw result for an idempotent operation.
    /// </summary>
    /// <param name="operation">The submitted operation.</param>
    /// <param name="message">The failure message.</param>
    /// <param name="statusKey">The stable message key.</param>
    /// <returns>A failed draw result.</returns>
    public static DrawResult Failure(DrawOperation operation, string message, string statusKey = "Draw.Failure")
    {
        var requestedMealType = operation.Mode == DrawMode.Normal ? operation.MealType : null;
        return new DrawResult(
            false,
            requestedMealType ?? default,
            null,
            null,
            null,
            null,
            message,
            null,
            statusKey,
            operation.OperationId,
            operation.Mode,
            requestedMealType,
            AppliedFilters: operation.Filters?.ForDrawMode(operation.Mode));
    }
}
