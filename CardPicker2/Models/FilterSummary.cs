namespace CardPicker2.Models;

/// <summary>
/// Represents localized filter summary text for a rendered page or result.
/// </summary>
/// <example>
/// <code>
/// var summary = new FilterSummary(new[] { "低價位", "快速" });
/// </code>
/// </example>
/// <param name="Items">The individual summary items.</param>
public sealed record FilterSummary(IReadOnlyList<string> Items)
{
    /// <summary>
    /// Gets an empty summary.
    /// </summary>
    public static FilterSummary Empty { get; } = new(Array.Empty<string>());

    /// <summary>
    /// Gets a value indicating whether this summary has no items.
    /// </summary>
    public bool IsEmpty => Items.Count == 0;
}
