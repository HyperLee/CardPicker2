namespace CardPicker2.Models;

/// <summary>
/// Describes manually maintained dietary preferences or meal traits.
/// </summary>
/// <example>
/// <code>
/// var preferences = new[] { DietaryPreference.Vegetarian, DietaryPreference.Light };
/// </code>
/// </example>
public enum DietaryPreference
{
    /// <summary>
    /// Vegetarian-friendly meal option.
    /// </summary>
    Vegetarian,

    /// <summary>
    /// Lighter-flavor meal option.
    /// </summary>
    Light,

    /// <summary>
    /// Stronger-flavor meal option.
    /// </summary>
    HeavyFlavor,

    /// <summary>
    /// Meal option that is practical for takeout.
    /// </summary>
    TakeoutFriendly
}
