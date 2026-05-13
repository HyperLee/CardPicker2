namespace CardPicker2.Models;

/// <summary>
/// Represents a meal card as visible in the current request culture.
/// </summary>
/// <param name="CardId">The immutable card identifier.</param>
/// <param name="MealType">The card's meal type.</param>
/// <param name="MealTypeDisplayName">The localized meal type label.</param>
/// <param name="DisplayName">The visible card name.</param>
/// <param name="DisplayDescription">The visible card description.</param>
/// <param name="CultureName">The requested language for this projection.</param>
/// <param name="IsFallback">Whether the visible content uses a fallback locale.</param>
/// <param name="MissingTranslationCultures">The cultures missing complete localized content.</param>
public sealed record LocalizedMealCardView(
    Guid CardId,
    MealType MealType,
    string MealTypeDisplayName,
    string DisplayName,
    string DisplayDescription,
    SupportedLanguage CultureName,
    bool IsFallback,
    IReadOnlyList<string> MissingTranslationCultures);
