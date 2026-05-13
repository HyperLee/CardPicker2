namespace CardPicker2.Models;

/// <summary>
/// Represents the full draw statistics projection shown on the home page.
/// </summary>
/// <example>
/// <code>
/// var summary = new DrawStatisticsSummary(
///     totalSuccessfulDraws: 10,
///     rows: rows,
///     statusKey: "Statistics.Ready");
/// </code>
/// </example>
/// <param name="TotalSuccessfulDraws">The total number of persisted successful draw records.</param>
/// <param name="Rows">The localized statistics rows.</param>
/// <param name="StatusKey">The stable status resource key.</param>
public sealed record DrawStatisticsSummary(
    int TotalSuccessfulDraws,
    IReadOnlyList<CardDrawStatistic> Rows,
    string StatusKey)
{
    /// <summary>
    /// Gets a value indicating whether successful draw history exists.
    /// </summary>
    public bool HasHistory => TotalSuccessfulDraws > 0;
}
