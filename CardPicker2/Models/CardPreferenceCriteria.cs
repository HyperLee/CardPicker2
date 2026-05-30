namespace CardPicker2.Models;

/// <summary>
/// Represents card-library filters based on favorite and draw eligibility state.
/// </summary>
/// <example>
/// <code>
/// var criteria = new CardPreferenceCriteria
/// {
///     FavoriteFilter = FavoriteFilter.FavoritesOnly,
///     DrawEligibilityFilter = DrawEligibilityFilter.DrawableOnly
/// };
/// </code>
/// </example>
public sealed class CardPreferenceCriteria
{
    /// <summary>
    /// Gets or sets the favorite-state filter.
    /// </summary>
    public FavoriteFilter FavoriteFilter { get; set; } = FavoriteFilter.All;

    /// <summary>
    /// Gets or sets the draw-eligibility filter.
    /// </summary>
    public DrawEligibilityFilter DrawEligibilityFilter { get; set; } = DrawEligibilityFilter.All;

    /// <summary>
    /// Creates a normalized criteria object, replacing unsupported enum values with defaults.
    /// </summary>
    /// <returns>A criteria object safe for service-layer filtering.</returns>
    public CardPreferenceCriteria Normalize()
    {
        return new CardPreferenceCriteria
        {
            FavoriteFilter = Enum.IsDefined(typeof(FavoriteFilter), FavoriteFilter)
                ? FavoriteFilter
                : FavoriteFilter.All,
            DrawEligibilityFilter = Enum.IsDefined(typeof(DrawEligibilityFilter), DrawEligibilityFilter)
                ? DrawEligibilityFilter
                : DrawEligibilityFilter.All
        };
    }
}
