namespace CardPicker2.Models;

/// <summary>
/// Represents one persisted successful draw fact.
/// </summary>
/// <example>
/// <code>
/// var record = new DrawHistoryRecord
/// {
///     Id = Guid.NewGuid(),
///     OperationId = operation.OperationId,
///     DrawMode = DrawMode.Normal,
///     CardId = selectedCard.Id,
///     MealTypeAtDraw = selectedCard.MealType,
///     SucceededAtUtc = DateTimeOffset.UtcNow
/// };
/// </code>
/// </example>
public sealed class DrawHistoryRecord
{
    /// <summary>
    /// Gets or initializes the immutable history identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or initializes the idempotency operation identifier.
    /// </summary>
    public Guid OperationId { get; init; }

    /// <summary>
    /// Gets or initializes the draw mode used when the card was selected.
    /// </summary>
    public DrawMode DrawMode { get; init; }

    /// <summary>
    /// Gets or initializes the immutable card identifier selected by the draw.
    /// </summary>
    public Guid CardId { get; init; }

    /// <summary>
    /// Gets or initializes the card meal type at the time of the successful draw.
    /// </summary>
    public MealType MealTypeAtDraw { get; init; }

    /// <summary>
    /// Gets or initializes the UTC time when the successful draw was persisted.
    /// </summary>
    public DateTimeOffset SucceededAtUtc { get; init; }
}
