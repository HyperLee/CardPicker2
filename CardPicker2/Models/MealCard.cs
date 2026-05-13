using System.Text.Json.Serialization;

namespace CardPicker2.Models;

/// <summary>
/// Represents one persisted meal option that can be browsed, searched, edited, deleted, or drawn.
/// </summary>
/// <example>
/// <code>
/// var card = new MealCard(id, MealType.Lunch, localizations)
/// {
///     Status = CardStatus.Active
/// };
/// </code>
/// </example>
public sealed class MealCard
{
    /// <summary>
    /// Initializes a new empty instance for JSON deserialization.
    /// </summary>
    public MealCard()
    {
    }

    /// <summary>
    /// Initializes a card from legacy single-language content.
    /// </summary>
    /// <param name="id">The immutable system-generated card identifier.</param>
    /// <param name="name">The Traditional Chinese meal name.</param>
    /// <param name="mealType">The meal period this card belongs to.</param>
    /// <param name="description">The Traditional Chinese meal description.</param>
    public MealCard(Guid id, string name, MealType mealType, string description)
        : this(
            id,
            mealType,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new(name, description)
            })
    {
    }

    /// <summary>
    /// Initializes a card with explicit localized content.
    /// </summary>
    /// <param name="id">The immutable system-generated card identifier.</param>
    /// <param name="mealType">The meal period this card belongs to.</param>
    /// <param name="localizations">The localized card content keyed by supported culture name.</param>
    public MealCard(Guid id, MealType mealType, IDictionary<string, MealCardLocalizedContent> localizations)
        : this(id, mealType, localizations, CardStatus.Active, deletedAtUtc: null)
    {
    }

    /// <summary>
    /// Initializes a card with explicit localized content and lifecycle state.
    /// </summary>
    /// <param name="id">The immutable system-generated card identifier.</param>
    /// <param name="mealType">The meal period this card belongs to.</param>
    /// <param name="localizations">The localized card content keyed by supported culture name.</param>
    /// <param name="status">The lifecycle status.</param>
    /// <param name="deletedAtUtc">The UTC deletion time when the card is retained as deleted.</param>
    public MealCard(
        Guid id,
        MealType mealType,
        IDictionary<string, MealCardLocalizedContent> localizations,
        CardStatus status,
        DateTimeOffset? deletedAtUtc)
    {
        Id = id;
        MealType = mealType;
        Localizations = new Dictionary<string, MealCardLocalizedContent>(localizations, StringComparer.OrdinalIgnoreCase);
        Status = status;
        DeletedAtUtc = deletedAtUtc;
    }

    /// <summary>
    /// Gets or initializes the immutable system-generated card identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or initializes the meal period this card belongs to.
    /// </summary>
    public MealType MealType { get; init; }

    /// <summary>
    /// Gets or initializes localized content keyed by supported culture name.
    /// </summary>
    public Dictionary<string, MealCardLocalizedContent> Localizations { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or initializes whether the card is active or retained only for history.
    /// </summary>
    public CardStatus Status { get; init; } = CardStatus.Active;

    /// <summary>
    /// Gets or initializes the UTC deletion time for retained deleted cards.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the card can be browsed, edited, deleted, and drawn.
    /// </summary>
    [JsonIgnore]
    public bool IsActive => Status == CardStatus.Active;

    /// <summary>
    /// Gets a value indicating whether the card is retained only for draw history.
    /// </summary>
    [JsonIgnore]
    public bool IsDeleted => Status == CardStatus.Deleted;

    /// <summary>
    /// Gets the Traditional Chinese meal name for legacy callers.
    /// </summary>
    [JsonIgnore]
    public string Name => GetContent(SupportedLanguage.ZhTw).Name;

    /// <summary>
    /// Gets the Traditional Chinese meal description for legacy callers.
    /// </summary>
    [JsonIgnore]
    public string Description => GetContent(SupportedLanguage.ZhTw).Description;

    /// <summary>
    /// Gets the card translation status.
    /// </summary>
    [JsonIgnore]
    public MealCardTranslationStatus TranslationStatus =>
        HasCompleteContent(SupportedLanguage.EnUs)
            ? MealCardTranslationStatus.Complete
            : MealCardTranslationStatus.MissingEnglish;

    /// <summary>
    /// Gets content for a language, falling back to Traditional Chinese when the requested language is missing.
    /// </summary>
    /// <param name="language">The requested language.</param>
    /// <returns>The localized or fallback content.</returns>
    public MealCardLocalizedContent GetContent(SupportedLanguage language)
    {
        if (HasCompleteContent(language))
        {
            return Localizations[language.CultureName].Normalize();
        }

        if (HasCompleteContent(SupportedLanguage.ZhTw))
        {
            return Localizations[SupportedLanguage.ZhTw.CultureName].Normalize();
        }

        return new MealCardLocalizedContent();
    }

    /// <summary>
    /// Reports whether the card has complete content for a language.
    /// </summary>
    /// <param name="language">The language to inspect.</param>
    /// <returns><see langword="true"/> when name and description are present.</returns>
    public bool HasCompleteContent(SupportedLanguage language)
    {
        return Localizations.TryGetValue(language.CultureName, out var content) && content.IsComplete;
    }

    /// <summary>
    /// Gets all supported culture names that are missing complete content.
    /// </summary>
    /// <returns>A list of missing culture names.</returns>
    public IReadOnlyList<string> GetMissingTranslationCultures()
    {
        return SupportedLanguage.All
            .Where(language => !HasCompleteContent(language))
            .Select(language => language.CultureName)
            .ToList();
    }

    /// <summary>
    /// Creates a normalized copy of this card.
    /// </summary>
    /// <returns>A card with trimmed localized content.</returns>
    public MealCard Normalize()
    {
        return new MealCard(
            Id,
            MealType,
            Localizations.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Normalize(),
                StringComparer.OrdinalIgnoreCase),
            Status,
            DeletedAtUtc);
    }
}

/// <summary>
/// Enumerates whether a card has complete bilingual content.
/// </summary>
public enum MealCardTranslationStatus
{
    /// <summary>
    /// Both Traditional Chinese and English content are complete.
    /// </summary>
    Complete,

    /// <summary>
    /// English content is missing and should fall back to Traditional Chinese.
    /// </summary>
    MissingEnglish
}
