namespace CardPicker2.Models;

/// <summary>
/// Describes the manually maintained preparation, wait, or pickup time range for a meal card.
/// </summary>
/// <example>
/// <code>
/// var quickMeal = PreparationTimeRange.Quick;
/// </code>
/// </example>
public enum PreparationTimeRange
{
    /// <summary>
    /// A quick meal option.
    /// </summary>
    Quick,

    /// <summary>
    /// A standard preparation or wait time.
    /// </summary>
    Standard,

    /// <summary>
    /// A longer preparation or wait time.
    /// </summary>
    Long
}
