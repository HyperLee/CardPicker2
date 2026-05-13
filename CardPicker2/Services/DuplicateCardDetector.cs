using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Detects duplicate meal cards using the normalized product rules.
/// </summary>
public sealed class DuplicateCardDetector
{
    /// <summary>
    /// Determines whether the input duplicates an existing card in any supported language.
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

        var inputCandidates = CreateCandidates(Guid.Empty, mealType, input.ToLocalizations()).ToList();
        if (inputCandidates.Count == 0)
        {
            return false;
        }

        return cards
            .Where(card => card.Id != ignoredCardId && card.MealType == mealType)
            .SelectMany(CreateCandidates)
            .Any(existing => inputCandidates.Any(inputCandidate => IsSamePair(existing, inputCandidate)));
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
        return cards
            .Where(existing => existing.Id != (ignoredCardId ?? card.Id) && existing.MealType == card.MealType)
            .SelectMany(CreateCandidates)
            .Any(existing => CreateCandidates(card).Any(candidate => IsSamePair(existing, candidate)));
    }

    private static IEnumerable<DuplicateCandidate> CreateCandidates(MealCard card)
    {
        return CreateCandidates(card.Id, card.MealType, card.Localizations);
    }

    private static IEnumerable<DuplicateCandidate> CreateCandidates(
        Guid cardId,
        MealType mealType,
        IReadOnlyDictionary<string, MealCardLocalizedContent> localizations)
    {
        var candidates = new List<DuplicateCandidate>();
        foreach (var language in SupportedLanguage.All)
        {
            var content = localizations.TryGetValue(language.CultureName, out var exactContent) && exactContent.IsComplete
                ? exactContent
                : localizations.TryGetValue(SupportedLanguage.ZhTw.CultureName, out var fallbackContent)
                    ? fallbackContent
                    : null;

            if (content is null)
            {
                continue;
            }

            var normalizedName = Normalize(content.Name);
            var normalizedDescription = Normalize(content.Description);
            if (string.IsNullOrEmpty(normalizedName) || string.IsNullOrEmpty(normalizedDescription))
            {
                continue;
            }

            candidates.Add(new DuplicateCandidate(cardId, mealType, normalizedName, normalizedDescription));
        }

        return candidates;
    }

    private static bool IsSamePair(DuplicateCandidate left, DuplicateCandidate right)
    {
        return left.MealType == right.MealType &&
            string.Equals(left.NormalizedName, right.NormalizedName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(left.NormalizedDescription, right.NormalizedDescription, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private sealed record DuplicateCandidate(
        Guid CardId,
        MealType MealType,
        string NormalizedName,
        string NormalizedDescription);
}
