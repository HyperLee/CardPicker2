namespace CardPicker2.Models;

/// <summary>
/// Stores optional decision information used to filter meal cards before drawing or browsing.
/// </summary>
/// <example>
/// <code>
/// var metadata = new MealCardDecisionMetadata
/// {
///     Tags = new[] { "便當", "外帶" },
///     PriceRange = PriceRange.Low,
///     PreparationTimeRange = PreparationTimeRange.Quick,
///     DietaryPreferences = new[] { DietaryPreference.TakeoutFriendly },
///     SpiceLevel = SpiceLevel.None
/// }.Normalize();
/// </code>
/// </example>
public sealed class MealCardDecisionMetadata
{
    /// <summary>
    /// Gets or initializes custom card tags. Tags are matched case-insensitively after trimming.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or initializes the approximate cost range.
    /// </summary>
    public PriceRange? PriceRange { get; init; }

    /// <summary>
    /// Gets or initializes the preparation, wait, or pickup time range.
    /// </summary>
    public PreparationTimeRange? PreparationTimeRange { get; init; }

    /// <summary>
    /// Gets or initializes dietary preferences or meal traits.
    /// </summary>
    public IReadOnlyList<DietaryPreference> DietaryPreferences { get; init; } = Array.Empty<DietaryPreference>();

    /// <summary>
    /// Gets or initializes the spice level.
    /// </summary>
    public SpiceLevel? SpiceLevel { get; init; }

    /// <summary>
    /// Creates a normalized copy with trimmed tags and stable de-duplicated collections.
    /// </summary>
    /// <returns>A normalized metadata value.</returns>
    public MealCardDecisionMetadata Normalize()
    {
        return new MealCardDecisionMetadata
        {
            Tags = NormalizeTags(Tags),
            PriceRange = PriceRange,
            PreparationTimeRange = PreparationTimeRange,
            DietaryPreferences = DietaryPreferences
                .Distinct()
                .OrderBy(preference => preference)
                .ToList(),
            SpiceLevel = SpiceLevel
        };
    }

    /// <summary>
    /// Normalizes tag values by trimming, dropping blanks, and removing duplicates.
    /// </summary>
    /// <param name="tags">The raw tags.</param>
    /// <returns>The normalized tags, preserving the first display text.</returns>
    public static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            var trimmed = tag.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized;
    }

    /// <summary>
    /// Reports whether all metadata fields are empty.
    /// </summary>
    /// <returns><see langword="true"/> when no decision fields are set.</returns>
    public bool IsEmpty()
    {
        return Tags.Count == 0 &&
            PriceRange is null &&
            PreparationTimeRange is null &&
            DietaryPreferences.Count == 0 &&
            SpiceLevel is null;
    }
}
