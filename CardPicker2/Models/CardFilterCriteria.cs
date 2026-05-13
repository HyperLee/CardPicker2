namespace CardPicker2.Models;

/// <summary>
/// Represents metadata filters shared by home-page drawing and card-library search.
/// </summary>
/// <example>
/// <code>
/// var criteria = new CardFilterCriteria
/// {
///     MealType = MealType.Lunch,
///     PriceRange = PriceRange.Low,
///     Tags = new[] { "便當" }
/// }.Normalize();
/// </code>
/// </example>
public sealed class CardFilterCriteria
{
    /// <summary>
    /// Gets or initializes an optional meal type. Random mode ignores this value.
    /// </summary>
    public MealType? MealType { get; init; }

    /// <summary>
    /// Gets or initializes an optional cost range filter.
    /// </summary>
    public PriceRange? PriceRange { get; init; }

    /// <summary>
    /// Gets or initializes an optional preparation, wait, or pickup time filter.
    /// </summary>
    public PreparationTimeRange? PreparationTimeRange { get; init; }

    /// <summary>
    /// Gets or initializes dietary preferences that must all be present on a matching card.
    /// </summary>
    public IReadOnlyList<DietaryPreference> DietaryPreferences { get; init; } = Array.Empty<DietaryPreference>();

    /// <summary>
    /// Gets or initializes the maximum acceptable spice level.
    /// </summary>
    public SpiceLevel? MaxSpiceLevel { get; init; }

    /// <summary>
    /// Gets or initializes tags that must all be present on a matching card.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or initializes the current UI language for summaries and visible-name projection.
    /// </summary>
    public SupportedLanguage CurrentLanguage { get; init; } = SupportedLanguage.ZhTw;

    /// <summary>
    /// Gets a value indicating whether any metadata-only filter is active.
    /// </summary>
    public bool HasActiveMetadataFilters =>
        PriceRange is not null ||
        PreparationTimeRange is not null ||
        DietaryPreferences.Count > 0 ||
        MaxSpiceLevel is not null ||
        Tags.Count > 0;

    /// <summary>
    /// Gets a value indicating whether any filter, including meal type, is active.
    /// </summary>
    public bool HasAnyFilter => MealType is not null || HasActiveMetadataFilters;

    /// <summary>
    /// Creates a normalized copy with stable de-duplicated collection fields.
    /// </summary>
    /// <returns>A normalized criteria value.</returns>
    public CardFilterCriteria Normalize()
    {
        return new CardFilterCriteria
        {
            MealType = MealType,
            PriceRange = PriceRange,
            PreparationTimeRange = PreparationTimeRange,
            DietaryPreferences = DietaryPreferences
                .Distinct()
                .OrderBy(preference => preference)
                .ToList(),
            MaxSpiceLevel = MaxSpiceLevel,
            Tags = MealCardDecisionMetadata.NormalizeTags(Tags),
            CurrentLanguage = CurrentLanguage
        };
    }

    /// <summary>
    /// Projects criteria for a draw mode, dropping meal type for random mode.
    /// </summary>
    /// <param name="mode">The draw mode.</param>
    /// <returns>Criteria with mode-specific meal-type semantics.</returns>
    public CardFilterCriteria ForDrawMode(DrawMode mode)
    {
        var normalized = Normalize();
        if (mode != DrawMode.Random)
        {
            return normalized;
        }

        return new CardFilterCriteria
        {
            MealType = null,
            PriceRange = normalized.PriceRange,
            PreparationTimeRange = normalized.PreparationTimeRange,
            DietaryPreferences = normalized.DietaryPreferences,
            MaxSpiceLevel = normalized.MaxSpiceLevel,
            Tags = normalized.Tags,
            CurrentLanguage = normalized.CurrentLanguage
        };
    }
}
