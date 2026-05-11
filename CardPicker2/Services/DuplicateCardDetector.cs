using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Detects duplicate meal cards using the normalized product rules.
/// </summary>
public sealed class DuplicateCardDetector
{
    /// <summary>
    /// Determines whether the input duplicates an existing card.
    /// </summary>
    /// <param name="cards">The existing cards to inspect.</param>
    /// <param name="input">The proposed card input.</param>
    /// <param name="ignoredCardId">An optional card ID to ignore during edit checks.</param>
    /// <returns><see langword="true"/> when another card has the same normalized name, meal type, and description.</returns>
    public bool HasDuplicate(IEnumerable<MealCard> cards, MealCardInputModel input, Guid? ignoredCardId = null)
    {
        if (input.MealType is not MealType mealType || !Enum.IsDefined(typeof(MealType), mealType))
        {
            return false;
        }

        var normalizedName = Normalize(input.Name);
        var normalizedDescription = Normalize(input.Description);
        if (string.IsNullOrEmpty(normalizedName) || string.IsNullOrEmpty(normalizedDescription))
        {
            return false;
        }

        return cards.Any(card =>
            card.Id != ignoredCardId &&
            card.MealType == mealType &&
            string.Equals(Normalize(card.Name), normalizedName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalize(card.Description), normalizedDescription, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether the supplied card duplicates any other card.
    /// </summary>
    /// <param name="cards">The existing cards to inspect.</param>
    /// <param name="card">The proposed card.</param>
    /// <param name="ignoredCardId">An optional card ID to ignore during edit checks.</param>
    /// <returns><see langword="true"/> when another card has the same normalized name, meal type, and description.</returns>
    public bool HasDuplicate(IEnumerable<MealCard> cards, MealCard card, Guid? ignoredCardId = null)
    {
        return HasDuplicate(cards, new MealCardInputModel
        {
            Name = card.Name,
            MealType = card.MealType,
            Description = card.Description
        }, ignoredCardId ?? card.Id);
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
