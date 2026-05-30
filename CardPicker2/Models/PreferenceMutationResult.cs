namespace CardPicker2.Models;

/// <summary>
/// Represents the outcome of a target-state preference mutation.
/// </summary>
/// <example>
/// <code>
/// var result = PreferenceMutationResult.Success(card.Id, card.Preferences, "Preference.Update.Succeeded");
/// </code>
/// </example>
public sealed record PreferenceMutationResult(
    bool Succeeded,
    PreferenceMutationStatus Status,
    Guid? CardId,
    CardPreferenceState? Preferences,
    string UserMessage,
    string MessageKey)
{
    /// <summary>
    /// Creates a successful preference mutation result.
    /// </summary>
    /// <param name="cardId">The updated card ID.</param>
    /// <param name="preferences">The persisted preference state.</param>
    /// <param name="userMessage">The safe user-facing message.</param>
    /// <param name="messageKey">The stable localization key.</param>
    /// <returns>A successful mutation result.</returns>
    public static PreferenceMutationResult Success(
        Guid cardId,
        CardPreferenceState preferences,
        string userMessage,
        string messageKey)
    {
        return new PreferenceMutationResult(
            true,
            PreferenceMutationStatus.Succeeded,
            cardId,
            preferences.Normalize(),
            userMessage,
            messageKey);
    }

    /// <summary>
    /// Creates a failed preference mutation result.
    /// </summary>
    /// <param name="status">The failure status.</param>
    /// <param name="userMessage">The safe user-facing message.</param>
    /// <param name="messageKey">The stable localization key.</param>
    /// <param name="cardId">The known target card ID, when available.</param>
    /// <returns>A failed mutation result.</returns>
    public static PreferenceMutationResult Failure(
        PreferenceMutationStatus status,
        string userMessage,
        string messageKey,
        Guid? cardId = null)
    {
        return new PreferenceMutationResult(false, status, cardId, null, userMessage, messageKey);
    }
}

/// <summary>
/// Identifies the result status for a preference mutation.
/// </summary>
/// <example>
/// <code>
/// var blocked = PreferenceMutationStatus.Blocked;
/// </code>
/// </example>
public enum PreferenceMutationStatus
{
    /// <summary>
    /// The mutation was saved successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The target card does not exist.
    /// </summary>
    NotFound,

    /// <summary>
    /// The target card is retained as deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// The library is in a blocking recovery state.
    /// </summary>
    Blocked,

    /// <summary>
    /// The submitted target state is invalid.
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// The updated document could not be written atomically.
    /// </summary>
    WriteFailed
}
