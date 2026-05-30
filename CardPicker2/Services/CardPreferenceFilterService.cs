using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Applies favorite and draw-eligibility filters for card-library browsing.
/// </summary>
/// <example>
/// <code>
/// var visible = service.Apply(cards, new CardPreferenceCriteria
/// {
///     FavoriteFilter = FavoriteFilter.FavoritesOnly
/// });
/// </code>
/// </example>
public sealed class CardPreferenceFilterService
{
    /// <summary>
    /// Applies preference criteria to active cards.
    /// </summary>
    /// <param name="cards">The active cards to inspect.</param>
    /// <param name="criteria">The preference criteria.</param>
    /// <returns>Cards matching all preference criteria.</returns>
    public IReadOnlyList<MealCard> Apply(IEnumerable<MealCard> cards, CardPreferenceCriteria? criteria)
    {
        var normalized = (criteria ?? new CardPreferenceCriteria()).Normalize();
        return cards
            .Where(card => Matches(card, normalized))
            .ToList();
    }

    /// <summary>
    /// Reports whether one card matches the supplied preference criteria.
    /// </summary>
    /// <param name="card">The card to inspect.</param>
    /// <param name="criteria">The preference criteria.</param>
    /// <returns><see langword="true"/> when the card matches.</returns>
    public bool Matches(MealCard card, CardPreferenceCriteria? criteria)
    {
        var normalized = (criteria ?? new CardPreferenceCriteria()).Normalize();
        if (!card.IsActive)
        {
            return false;
        }

        if (normalized.FavoriteFilter == FavoriteFilter.FavoritesOnly && !card.Preferences.IsFavorite)
        {
            return false;
        }

        if (normalized.FavoriteFilter == FavoriteFilter.NotFavoritesOnly && card.Preferences.IsFavorite)
        {
            return false;
        }

        if (normalized.DrawEligibilityFilter == DrawEligibilityFilter.DrawableOnly && !card.IsDrawable)
        {
            return false;
        }

        if (normalized.DrawEligibilityFilter == DrawEligibilityFilter.ExcludedOnly && card.IsDrawable)
        {
            return false;
        }

        return true;
    }
}
