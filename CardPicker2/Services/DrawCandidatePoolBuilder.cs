using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Builds the fair candidate pool for normal and random draw modes.
/// </summary>
/// <example>
/// <code>
/// var pool = builder.Build(operation, document.Cards);
/// var selected = pool.Cards[randomizer.NextIndex(pool.Cards.Count)];
/// </code>
/// </example>
public sealed class DrawCandidatePoolBuilder
{
    /// <summary>
    /// Builds a candidate pool using active cards only.
    /// </summary>
    /// <param name="operation">The draw operation.</param>
    /// <param name="cards">The cards loaded from the library.</param>
    /// <returns>The candidate pool and its nominal probability.</returns>
    public DrawCandidatePool Build(DrawOperation operation, IEnumerable<MealCard> cards)
    {
        var activeCards = cards.Where(card => card.IsActive);
        var selectedMealType = operation.Mode == DrawMode.Normal ? operation.MealType : null;

        var candidates = operation.Mode switch
        {
            DrawMode.Normal when selectedMealType is MealType mealType && Enum.IsDefined(typeof(MealType), mealType) =>
                activeCards.Where(card => card.MealType == mealType),
            DrawMode.Random => activeCards,
            _ => Array.Empty<MealCard>()
        };

        return new DrawCandidatePool(
            operation.Mode,
            selectedMealType,
            candidates.ToList());
    }
}

/// <summary>
/// Represents the cards eligible for one fair draw.
/// </summary>
/// <example>
/// <code>
/// if (pool.Cards.Count &gt; 0)
/// {
///     var probability = pool.NominalProbability;
/// }
/// </code>
/// </example>
/// <param name="Mode">The draw mode used to build the pool.</param>
/// <param name="SelectedMealType">The selected meal type for normal mode.</param>
/// <param name="Cards">The active cards eligible for selection.</param>
public sealed record DrawCandidatePool(
    DrawMode Mode,
    MealType? SelectedMealType,
    IReadOnlyList<MealCard> Cards)
{
    /// <summary>
    /// Gets each candidate card's nominal probability when the pool is non-empty.
    /// </summary>
    public decimal? NominalProbability => Cards.Count > 0 ? 1m / Cards.Count : null;
}
