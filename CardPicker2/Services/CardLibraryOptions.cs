namespace CardPicker2.Services;

/// <summary>
/// Provides runtime options for the local card-library store.
/// </summary>
public sealed class CardLibraryOptions
{
    /// <summary>
    /// Gets or sets the full path to the persisted card-library JSON file.
    /// </summary>
    public string LibraryFilePath { get; set; } = Path.Combine("data", "cards.json");
}