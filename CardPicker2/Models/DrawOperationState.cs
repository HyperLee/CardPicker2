namespace CardPicker2.Models;

/// <summary>
/// Represents the user-visible state of the slot-machine draw flow.
/// </summary>
public enum DrawOperationState
{
    /// <summary>
    /// No meal type has been selected or no coin has been inserted.
    /// </summary>
    Idle,

    /// <summary>
    /// A meal type has been selected.
    /// </summary>
    MealSelected,

    /// <summary>
    /// The user confirmed the coin-in step.
    /// </summary>
    CoinInserted,

    /// <summary>
    /// The draw is in progress.
    /// </summary>
    Spinning,

    /// <summary>
    /// A result has been revealed.
    /// </summary>
    Revealed,

    /// <summary>
    /// The draw is blocked by validation or recovery state.
    /// </summary>
    Blocked
}

/// <summary>
/// Provides state transition helpers for the draw flow.
/// </summary>
public static class DrawOperationStateTransitions
{
    /// <summary>
    /// Maps the selected meal type to the initial visible state.
    /// </summary>
    /// <param name="mealType">The selected meal type, if any.</param>
    /// <returns><see cref="DrawOperationState.MealSelected"/> when a meal is selected; otherwise <see cref="DrawOperationState.Idle"/>.</returns>
    public static DrawOperationState FromSelection(MealType? mealType)
    {
        return mealType.HasValue && Enum.IsDefined(typeof(MealType), mealType.Value)
            ? DrawOperationState.MealSelected
            : DrawOperationState.Idle;
    }

    /// <summary>
    /// Returns whether the current state allows starting a server-side draw.
    /// </summary>
    /// <param name="state">The current draw state.</param>
    /// <returns><see langword="true"/> only when the coin-in step has been completed.</returns>
    public static bool CanStartDraw(DrawOperationState state)
    {
        return state == DrawOperationState.CoinInserted;
    }
}
