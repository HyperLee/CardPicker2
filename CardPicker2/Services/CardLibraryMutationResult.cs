using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Describes the outcome of a create, edit, or delete card operation.
/// </summary>
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
