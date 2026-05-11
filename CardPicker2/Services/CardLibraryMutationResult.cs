using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Describes the outcome of a create, edit, or delete card operation.
/// </summary>
/// <param name="Status">The mutation state.</param>
/// <param name="Card">The affected card when the mutation succeeds and one is available.</param>
/// <param name="UserMessage">A Traditional Chinese message suitable for UI feedback.</param>
public sealed record CardLibraryMutationResult(
    CardLibraryMutationStatus Status,
    MealCard? Card,
    string UserMessage)
{
    /// <summary>
    /// Gets a value indicating whether the mutation succeeded.
    /// </summary>
    public bool Succeeded => Status == CardLibraryMutationStatus.Succeeded;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="card">The affected card when available.</param>
    /// <param name="message">The Traditional Chinese success message.</param>
    /// <returns>A successful mutation result.</returns>
    public static CardLibraryMutationResult Success(MealCard? card, string message)
    {
        return new CardLibraryMutationResult(CardLibraryMutationStatus.Succeeded, card, message);
    }

    /// <summary>
    /// Creates a failed mutation result.
    /// </summary>
    /// <param name="status">The failure status.</param>
    /// <param name="message">The Traditional Chinese failure message.</param>
    /// <returns>A failed mutation result.</returns>
    public static CardLibraryMutationResult Failure(CardLibraryMutationStatus status, string message)
    {
        return new CardLibraryMutationResult(status, null, message);
    }

    /// <summary>
    /// Creates a failed result for behavior not yet available in the current implementation phase.
    /// </summary>
    /// <param name="message">The Traditional Chinese failure message.</param>
    /// <returns>A failed mutation result.</returns>
    public static CardLibraryMutationResult NotAvailable(string message)
    {
        return new CardLibraryMutationResult(CardLibraryMutationStatus.WriteFailed, null, message);
    }
}

/// <summary>
/// Enumerates create, edit, and delete result states.
/// </summary>
public enum CardLibraryMutationStatus
{
    /// <summary>
    /// The mutation succeeded.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The card library is in blocking recovery state.
    /// </summary>
    Blocked,

    /// <summary>
    /// The supplied input failed validation.
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// The supplied card would duplicate another card.
    /// </summary>
    Duplicate,

    /// <summary>
    /// The target card was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The card library could not be written.
    /// </summary>
    WriteFailed
}