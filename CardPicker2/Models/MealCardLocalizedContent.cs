namespace CardPicker2.Models;

/// <summary>
/// Represents one localized name and description for a meal card.
/// </summary>
public sealed class MealCardLocalizedContent
{
    /// <summary>
    /// Initializes a new empty instance for JSON deserialization.
    /// </summary>
    public MealCardLocalizedContent()
    {
    }

    /// <summary>
    /// Initializes a new localized content instance.
    /// </summary>
    /// <param name="name">The localized meal name.</param>
    /// <param name="description">The localized meal description.</param>
    public MealCardLocalizedContent(string name, string description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Gets or initializes the localized meal name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the localized meal description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Returns a copy with trimmed name and description values.
    /// </summary>
    /// <returns>A normalized localized content instance.</returns>
    public MealCardLocalizedContent Normalize()
    {
        return new MealCardLocalizedContent(Name.Trim(), Description.Trim());
    }

    /// <summary>
    /// Gets a value indicating whether both localized fields contain text.
    /// </summary>
    public bool IsComplete => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Description);
}
