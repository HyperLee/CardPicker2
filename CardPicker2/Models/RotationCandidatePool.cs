namespace CardPicker2.Models;

/// <summary>
/// Represents the candidate pool before and after recent-repeat rotation cooldown is applied.
/// </summary>
/// <example>
/// <code>
/// var pool = rotationService.Apply(baseCandidates, history, settings);
/// var candidates = pool.PostRotationCards;
/// </code>
/// </example>
/// <param name="PreRotationCards">The candidates produced by draw mode and metadata filtering.</param>
/// <param name="PostRotationCards">The candidates remaining after recent-repeat exclusion.</param>
/// <param name="ExcludedCardIds">The active candidate IDs removed by rotation cooldown.</param>
/// <param name="Settings">The validated settings used to build the pool.</param>
/// <param name="Snapshot">The persisted summary for a successful draw from this pool.</param>
public sealed record RotationCandidatePool(
    IReadOnlyList<MealCard> PreRotationCards,
    IReadOnlyList<MealCard> PostRotationCards,
    IReadOnlySet<Guid> ExcludedCardIds,
    RotationCooldownSettings Settings,
    RotationSnapshot Snapshot)
{
    /// <summary>
    /// Gets each post-rotation candidate card's nominal probability when the pool is non-empty.
    /// </summary>
    public decimal? NominalProbability => PostRotationCards.Count > 0 ? 1m / PostRotationCards.Count : null;

    /// <summary>
    /// Gets a value indicating whether rotation exclusion emptied a non-empty base pool.
    /// </summary>
    public bool IsEmptyAfterRotation => PreRotationCards.Count > 0 && PostRotationCards.Count == 0;
}
