using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Projects persisted meal cards into the current request language.
/// </summary>
public sealed class MealCardLocalizationService
{
    /// <summary>
    /// Projects a card into a localized view model.
    /// </summary>
    /// <param name="card">The card to project.</param>
    /// <param name="language">The requested language.</param>
    /// <returns>The localized card projection.</returns>
    public LocalizedMealCardView Project(MealCard card, SupportedLanguage language)
    {
        var normalized = card.Normalize();
        var isFallback = !normalized.HasCompleteContent(language);
        var content = normalized.GetContent(language);

        return new LocalizedMealCardView(
            normalized.Id,
            normalized.MealType,
            normalized.MealType.ToDisplayName(language),
            content.Name,
            content.Description,
            language,
            isFallback,
            normalized.GetMissingTranslationCultures(),
            normalized.Status,
            normalized.DeletedAtUtc);
    }

    /// <summary>
    /// Projects multiple cards into a localized view model sequence.
    /// </summary>
    /// <param name="cards">The cards to project.</param>
    /// <param name="language">The requested language.</param>
    /// <returns>The localized card projections.</returns>
    public IReadOnlyList<LocalizedMealCardView> ProjectMany(IEnumerable<MealCard> cards, SupportedLanguage language)
    {
        return cards.Select(card => Project(card, language)).ToList();
    }
}
