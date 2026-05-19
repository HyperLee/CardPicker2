using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Applies recent successful draw exclusion to an already filtered draw candidate pool.
/// </summary>
/// <example>
/// <code>
/// var rotationPool = service.Apply(filteredCandidates, document.DrawHistory, settings);
/// var candidates = rotationPool.PostRotationCards;
/// </code>
/// </example>
public sealed class DrawRotationCooldownService
{
    /// <summary>
    /// Applies recent-repeat exclusion to the supplied pre-rotation candidates.
    /// </summary>
    /// <param name="preRotationCards">The active candidates after draw mode and metadata filters.</param>
    /// <param name="drawHistory">The persisted successful draw history.</param>
    /// <param name="settings">The validated rotation settings.</param>
    /// <returns>The candidate pool and persisted snapshot projection.</returns>
    /// <example>
    /// <code>
    /// var pool = service.Apply(candidates, history, new RotationCooldownSettings(true, 3));
    /// </code>
    /// </example>
    public RotationCandidatePool Apply(
        IReadOnlyList<MealCard> preRotationCards,
        IReadOnlyList<DrawHistoryRecord> drawHistory,
        RotationCooldownSettings settings)
    {
        var normalizedSettings = settings.IsValid ? settings : RotationCooldownSettings.Default;
        var preRotation = preRotationCards.ToList();

        if (!normalizedSettings.IsActive)
        {
            return CreatePool(preRotation, preRotation, new HashSet<Guid>(), normalizedSettings);
        }

        var recentCardIds = drawHistory
            .Select((record, index) => new { record, index })
            .OrderByDescending(item => item.record.SucceededAtUtc)
            .ThenByDescending(item => item.index)
            .Take(normalizedSettings.RecentDrawCount)
            .Select(item => item.record.CardId)
            .ToHashSet();

        var excludedCardIds = preRotation
            .Where(card => recentCardIds.Contains(card.Id))
            .Select(card => card.Id)
            .ToHashSet();
        var postRotation = preRotation
            .Where(card => !excludedCardIds.Contains(card.Id))
            .ToList();

        return CreatePool(preRotation, postRotation, excludedCardIds, normalizedSettings);
    }

    private static RotationCandidatePool CreatePool(
        IReadOnlyList<MealCard> preRotation,
        IReadOnlyList<MealCard> postRotation,
        IReadOnlySet<Guid> excludedCardIds,
        RotationCooldownSettings settings)
    {
        return new RotationCandidatePool(
            preRotation,
            postRotation,
            excludedCardIds,
            settings,
            RotationSnapshot.Create(settings, preRotation.Count, excludedCardIds.Count));
    }
}
