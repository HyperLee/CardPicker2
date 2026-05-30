namespace CardPicker2.Models;

/// <summary>
/// Identifies how the card library filters cards by future draw eligibility.
/// </summary>
/// <example>
/// <code>
/// var filter = DrawEligibilityFilter.DrawableOnly;
/// </code>
/// </example>
public enum DrawEligibilityFilter
{
    /// <summary>
    /// Shows active cards regardless of manual exclusion state.
    /// </summary>
    All,

    /// <summary>
    /// Shows only active cards that can enter future draw candidate pools.
    /// </summary>
    DrawableOnly,

    /// <summary>
    /// Shows only active cards manually excluded from future draws.
    /// </summary>
    ExcludedOnly
}
