namespace CardPicker2.Models;

/// <summary>
/// Identifies how the card library filters cards by favorite state.
/// </summary>
/// <example>
/// <code>
/// var filter = FavoriteFilter.FavoritesOnly;
/// </code>
/// </example>
public enum FavoriteFilter
{
    /// <summary>
    /// Shows cards regardless of favorite state.
    /// </summary>
    All,

    /// <summary>
    /// Shows only cards marked as favorite.
    /// </summary>
    FavoritesOnly,

    /// <summary>
    /// Shows only cards not marked as favorite.
    /// </summary>
    NotFavoritesOnly
}
