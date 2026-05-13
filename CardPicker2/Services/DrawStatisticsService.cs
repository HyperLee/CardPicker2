using System.Globalization;

using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Projects persisted successful draw history into user-visible statistics.
/// </summary>
/// <example>
/// <code>
/// var summary = statisticsService.CreateSummary(document, SupportedLanguage.ZhTw);
/// </code>
/// </example>
public sealed class DrawStatisticsService
{
    private readonly MealCardLocalizationService _localizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawStatisticsService"/> class.
    /// </summary>
    /// <param name="localizationService">The localized card projection service.</param>
    public DrawStatisticsService(MealCardLocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    /// <summary>
    /// Creates a statistics summary from cards and successful draw history.
    /// </summary>
    /// <param name="document">The loaded card-library document.</param>
    /// <param name="language">The projection language.</param>
    /// <returns>The statistics summary.</returns>
    public DrawStatisticsSummary CreateSummary(CardLibraryDocument document, SupportedLanguage language)
    {
        var total = document.DrawHistory.Count;
        if (total == 0)
        {
            return new DrawStatisticsSummary(0, Array.Empty<CardDrawStatistic>(), "Statistics.Empty");
        }

        var counts = document.DrawHistory
            .GroupBy(history => history.CardId)
            .ToDictionary(group => group.Key, group => group.Count());

        var rows = document.Cards
            .Where(card => card.IsActive || (card.IsDeleted && counts.ContainsKey(card.Id)))
            .Select(card => CreateRow(card, counts.GetValueOrDefault(card.Id), total, language))
            .OrderBy(row => row.Status == CardStatus.Deleted)
            .ThenBy(row => row.MealTypeDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DrawStatisticsSummary(total, rows, "Statistics.Ready");
    }

    private CardDrawStatistic CreateRow(MealCard card, int count, int total, SupportedLanguage language)
    {
        var localized = _localizationService.Project(card, language);
        var probability = total > 0 ? count / (decimal)total : (decimal?)null;
        return new CardDrawStatistic(
            card.Id,
            localized.DisplayName,
            localized.MealTypeDisplayName,
            card.Status,
            count,
            probability,
            FormatProbability(probability));
    }

    private static string FormatProbability(decimal? probability)
    {
        if (probability is null)
        {
            return string.Empty;
        }

        var percentage = probability.Value * 100m;
        var format = percentage % 1m == 0m ? "0" : "0.##";
        return $"{percentage.ToString(format, CultureInfo.InvariantCulture)}%";
    }
}
