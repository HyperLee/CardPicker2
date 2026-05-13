using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Projects persisted meal cards into the current request language.
/// </summary>
/// <example>
/// <code>
/// var view = localizationService.Project(card, SupportedLanguage.ZhTw);
/// </code>
/// </example>
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
            normalized.DeletedAtUtc,
            CreateMetadataBadges(normalized.DecisionMetadata, language));
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

    private static IReadOnlyList<string> CreateMetadataBadges(MealCardDecisionMetadata? metadata, SupportedLanguage language)
    {
        var normalized = metadata?.Normalize();
        if (normalized is null)
        {
            return Array.Empty<string>();
        }

        var badges = new List<string>();
        badges.AddRange(normalized.Tags);

        if (normalized.PriceRange is PriceRange priceRange)
        {
            badges.Add(Display(priceRange, language));
        }

        if (normalized.PreparationTimeRange is PreparationTimeRange preparationTimeRange)
        {
            badges.Add(Display(preparationTimeRange, language));
        }

        badges.AddRange(normalized.DietaryPreferences.Select(preference => Display(preference, language)));

        if (normalized.SpiceLevel is SpiceLevel spiceLevel)
        {
            badges.Add(Display(spiceLevel, language));
        }

        return badges;
    }

    private static string Display(PriceRange value, SupportedLanguage language)
    {
        return language == SupportedLanguage.EnUs
            ? value switch
            {
                PriceRange.Low => "Low",
                PriceRange.Medium => "Medium",
                PriceRange.High => "High",
                _ => value.ToString()
            }
            : value switch
            {
                PriceRange.Low => "低價位",
                PriceRange.Medium => "中價位",
                PriceRange.High => "高價位",
                _ => value.ToString()
            };
    }

    private static string Display(PreparationTimeRange value, SupportedLanguage language)
    {
        return language == SupportedLanguage.EnUs
            ? value switch
            {
                PreparationTimeRange.Quick => "Quick",
                PreparationTimeRange.Standard => "Standard",
                PreparationTimeRange.Long => "Long",
                _ => value.ToString()
            }
            : value switch
            {
                PreparationTimeRange.Quick => "快速",
                PreparationTimeRange.Standard => "一般",
                PreparationTimeRange.Long => "較久",
                _ => value.ToString()
            };
    }

    private static string Display(DietaryPreference value, SupportedLanguage language)
    {
        return language == SupportedLanguage.EnUs
            ? value switch
            {
                DietaryPreference.Vegetarian => "Vegetarian",
                DietaryPreference.Light => "Light",
                DietaryPreference.HeavyFlavor => "Heavy flavor",
                DietaryPreference.TakeoutFriendly => "Takeout friendly",
                _ => value.ToString()
            }
            : value switch
            {
                DietaryPreference.Vegetarian => "蔬食",
                DietaryPreference.Light => "清淡",
                DietaryPreference.HeavyFlavor => "重口味",
                DietaryPreference.TakeoutFriendly => "適合外帶",
                _ => value.ToString()
            };
    }

    private static string Display(SpiceLevel value, SupportedLanguage language)
    {
        return language == SupportedLanguage.EnUs
            ? value switch
            {
                SpiceLevel.None => "None",
                SpiceLevel.Mild => "Mild",
                SpiceLevel.Medium => "Medium",
                SpiceLevel.Hot => "Hot",
                _ => value.ToString()
            }
            : value switch
            {
                SpiceLevel.None => "不辣",
                SpiceLevel.Mild => "小辣",
                SpiceLevel.Medium => "中辣",
                SpiceLevel.Hot => "重辣",
                _ => value.ToString()
            };
    }
}
