using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Applies shared metadata filter semantics to active meal cards.
/// </summary>
/// <example>
/// <code>
/// var matching = filterService.Apply(cards, criteria);
/// </code>
/// </example>
public sealed class MealCardFilterService
{
    /// <summary>
    /// Applies active-card and metadata filter criteria.
    /// </summary>
    /// <param name="cards">The source cards.</param>
    /// <param name="criteria">The filter criteria.</param>
    /// <returns>Cards matching all active criteria.</returns>
    public IReadOnlyList<MealCard> Apply(IEnumerable<MealCard> cards, CardFilterCriteria? criteria)
    {
        var normalized = (criteria ?? new CardFilterCriteria()).Normalize();
        return cards
            .Where(card => Matches(card, normalized))
            .ToList();
    }

    /// <summary>
    /// Reports whether one card matches all active criteria.
    /// </summary>
    /// <param name="card">The card to inspect.</param>
    /// <param name="criteria">The normalized criteria.</param>
    /// <returns><see langword="true"/> when the card matches.</returns>
    public bool Matches(MealCard card, CardFilterCriteria? criteria)
    {
        var normalized = (criteria ?? new CardFilterCriteria()).Normalize();
        if (!card.IsActive)
        {
            return false;
        }

        if (normalized.MealType is MealType mealType && card.MealType != mealType)
        {
            return false;
        }

        if (!normalized.HasActiveMetadataFilters)
        {
            return true;
        }

        var metadata = card.DecisionMetadata?.Normalize();
        if (metadata is null)
        {
            return false;
        }

        if (normalized.PriceRange is PriceRange priceRange && metadata.PriceRange != priceRange)
        {
            return false;
        }

        if (normalized.PreparationTimeRange is PreparationTimeRange preparationTimeRange &&
            metadata.PreparationTimeRange != preparationTimeRange)
        {
            return false;
        }

        if (normalized.MaxSpiceLevel is SpiceLevel maxSpiceLevel &&
            (metadata.SpiceLevel is null || metadata.SpiceLevel > maxSpiceLevel))
        {
            return false;
        }

        if (normalized.DietaryPreferences.Count > 0)
        {
            var cardPreferences = metadata.DietaryPreferences.ToHashSet();
            if (!normalized.DietaryPreferences.All(cardPreferences.Contains))
            {
                return false;
            }
        }

        if (normalized.Tags.Count > 0)
        {
            var cardTags = metadata.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!normalized.Tags.All(cardTags.Contains))
            {
                return false;
            }
        }

        return true;
    }
}
