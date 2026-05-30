namespace CardPicker2.Models;

/// <summary>
/// Stores long-lived user preference flags for one meal card.
/// </summary>
/// <example>
/// <code>
/// var state = new CardPreferenceState
/// {
///     IsFavorite = true,
///     IsExcludedFromDraw = false
/// };
/// </code>
/// </example>
public sealed class CardPreferenceState
{
    /// <summary>
    /// Gets the safe preference default for new or migrated cards.
    /// </summary>
    public static CardPreferenceState Default => new();

    /// <summary>
    /// Gets or initializes whether the card is marked as a favorite.
    /// </summary>
    public bool IsFavorite { get; init; }

    /// <summary>
    /// Gets or initializes whether the card is manually excluded from future draws.
    /// </summary>
    public bool IsExcludedFromDraw { get; init; }

    /// <summary>
    /// Creates a normalized preference state.
    /// </summary>
    /// <returns>A preference state with explicit boolean values.</returns>
    public CardPreferenceState Normalize()
    {
        return new CardPreferenceState
        {
            IsFavorite = IsFavorite,
            IsExcludedFromDraw = IsExcludedFromDraw
        };
    }
}
