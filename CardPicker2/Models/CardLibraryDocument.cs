namespace CardPicker2.Models;

/// <summary>
/// Represents the root JSON document for the local card library.
/// </summary>
public sealed class CardLibraryDocument
{
    /// <summary>
    /// Gets the supported schema version for bilingual card-library persistence.
    /// </summary>
    public const int CurrentSchemaVersion = 2;

    /// <summary>
    /// Gets the legacy single-language schema version.
    /// </summary>
    public const int LegacySchemaVersion = 1;

    /// <summary>
    /// Gets or initializes the persisted schema version.
    /// </summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets or initializes the cards contained by the library.
    /// </summary>
    public IReadOnlyList<MealCard> Cards { get; init; } = Array.Empty<MealCard>();
}
