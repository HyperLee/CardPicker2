namespace CardPicker2.Models;

/// <summary>
/// Represents the root JSON document for the local card library.
/// </summary>
/// <example>
/// <code>
/// var document = new CardLibraryDocument
/// {
///     Cards = cards,
///     DrawHistory = history
/// };
/// </code>
/// </example>
public sealed class CardLibraryDocument
{
    /// <summary>
    /// Gets the supported schema version for draw history and statistics persistence.
    /// </summary>
    public const int CurrentSchemaVersion = 3;

    /// <summary>
    /// Gets the legacy single-language schema version.
    /// </summary>
    public const int LegacySchemaVersion = 1;

    /// <summary>
    /// Gets the bilingual card-library schema version.
    /// </summary>
    public const int BilingualSchemaVersion = 2;

    /// <summary>
    /// Gets or initializes the persisted schema version.
    /// </summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets or initializes the cards contained by the library.
    /// </summary>
    public IReadOnlyList<MealCard> Cards { get; init; } = Array.Empty<MealCard>();

    /// <summary>
    /// Gets or initializes successful draw history records.
    /// </summary>
    public IReadOnlyList<DrawHistoryRecord> DrawHistory { get; init; } = Array.Empty<DrawHistoryRecord>();
}
