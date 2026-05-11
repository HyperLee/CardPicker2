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
