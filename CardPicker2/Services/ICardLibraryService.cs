using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Coordinates card-library loading, lookup, draw, and mutation operations.
/// </summary>
public interface ICardLibraryService
{
    /// <summary>
    /// Loads and validates the card library.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The load result.</returns>
    Task<CardLibraryLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports whether the library is in blocking recovery state.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns><see langword="true"/> when operations must be disabled.</returns>
    Task<bool> IsBlockedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches cards using optional criteria.
    /// </summary>
    /// <param name="criteria">The search criteria.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The matching cards.</returns>
    Task<IReadOnlyList<MealCard>> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds one card by immutable ID.
    /// </summary>
    /// <param name="id">The card ID.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The card, or <see langword="null"/> when not found.</returns>
    Task<MealCard?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Draws a card for the selected meal type.
    /// </summary>
    /// <param name="mealType">The selected meal type.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The draw result.</returns>
    Task<DrawResult> DrawAsync(MealType mealType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new card.
    /// </summary>
    /// <param name="input">The submitted card input.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The mutation result.</returns>
    Task<CardLibraryMutationResult> CreateAsync(MealCardInputModel input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing card.
    /// </summary>
    /// <param name="id">The existing card ID.</param>
    /// <param name="input">The submitted card input.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The mutation result.</returns>
    Task<CardLibraryMutationResult> UpdateAsync(Guid id, MealCardInputModel input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing card.
    /// </summary>
    /// <param name="id">The existing card ID.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The mutation result.</returns>
    Task<CardLibraryMutationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
