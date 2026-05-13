namespace CardPicker2.Models;

/// <summary>
/// Represents the server-validated input for one submitted draw operation.
/// </summary>
/// <example>
/// <code>
/// var operation = new DrawOperation
/// {
///     OperationId = Guid.NewGuid(),
///     Mode = DrawMode.Normal,
///     MealType = MealType.Lunch,
///     CoinInserted = true,
///     RequestedLanguage = SupportedLanguage.ZhTw
/// };
/// </code>
/// </example>
public sealed class DrawOperation
{
    /// <summary>
    /// Gets or initializes the non-secret operation identifier used for idempotent replay.
    /// </summary>
    public Guid OperationId { get; init; }

    /// <summary>
    /// Gets or initializes the candidate-pool mode requested by the user.
    /// </summary>
    public DrawMode Mode { get; init; } = DrawMode.Normal;

    /// <summary>
    /// Gets or initializes the requested meal type for normal-mode draws.
    /// </summary>
    public MealType? MealType { get; init; }

    /// <summary>
    /// Gets or initializes a value indicating whether the user completed the coin confirmation.
    /// </summary>
    public bool CoinInserted { get; init; }

    /// <summary>
    /// Gets or initializes the language used for result projection.
    /// </summary>
    public SupportedLanguage RequestedLanguage { get; init; } = SupportedLanguage.ZhTw;

    /// <summary>
    /// Gets a value indicating whether <see cref="OperationId"/> can be persisted.
    /// </summary>
    public bool HasValidOperationId => OperationId != Guid.Empty;

    /// <summary>
    /// Gets a value indicating whether the submitted mode is defined.
    /// </summary>
    public bool HasValidMode => Enum.IsDefined(typeof(DrawMode), Mode);

    /// <summary>
    /// Gets a value indicating whether this operation needs a meal type.
    /// </summary>
    public bool RequiresMealType => Mode == DrawMode.Normal;

    /// <summary>
    /// Gets a value indicating whether the meal-type requirement is satisfied.
    /// </summary>
    public bool HasValidMealType =>
        Mode == DrawMode.Random ||
        (MealType is MealType mealType && Enum.IsDefined(typeof(MealType), mealType));

    /// <summary>
    /// Gets a value indicating whether the operation has enough valid input to reach the candidate pool.
    /// </summary>
    public bool CanAttemptDraw => HasValidOperationId && HasValidMode && HasValidMealType && CoinInserted;
}
