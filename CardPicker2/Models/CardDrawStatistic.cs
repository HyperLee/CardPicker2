namespace CardPicker2.Models;

/// <summary>
/// Represents one localized card row in the draw statistics table.
/// </summary>
/// <example>
/// <code>
/// var row = new CardDrawStatistic(
///     card.Id,
///     "牛肉麵",
///     "午餐",
///     CardStatus.Active,
///     2,
///     0.2m,
///     "20%");
/// </code>
/// </example>
/// <param name="CardId">The immutable card identifier.</param>
/// <param name="DisplayName">The localized card display name.</param>
/// <param name="MealTypeDisplayName">The localized meal type display name.</param>
/// <param name="Status">The current card lifecycle status.</param>
/// <param name="DrawCount">The number of successful history records for this card.</param>
/// <param name="HistoricalProbability">The historical probability, or <see langword="null"/> when no history exists.</param>
/// <param name="HistoricalProbabilityDisplay">The localized probability display text.</param>
public sealed record CardDrawStatistic(
    Guid CardId,
    string DisplayName,
    string MealTypeDisplayName,
    CardStatus Status,
    int DrawCount,
    decimal? HistoricalProbability,
    string HistoricalProbabilityDisplay);
