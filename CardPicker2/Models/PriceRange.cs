namespace CardPicker2.Models;

/// <summary>
/// Describes the manually maintained approximate cost range for a meal card.
/// </summary>
/// <example>
/// <code>
/// var affordable = PriceRange.Low;
/// </code>
/// </example>
public enum PriceRange
{
    /// <summary>
    /// Low-cost meal option.
    /// </summary>
    Low,

    /// <summary>
    /// Medium-cost meal option.
    /// </summary>
    Medium,

    /// <summary>
    /// Higher-cost meal option.
    /// </summary>
    High
}
