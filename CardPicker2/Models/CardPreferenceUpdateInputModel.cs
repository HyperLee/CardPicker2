using System.ComponentModel.DataAnnotations;

namespace CardPicker2.Models;

/// <summary>
/// Represents a target-state preference update submitted from a Razor Page form.
/// </summary>
/// <example>
/// <code>
/// var input = new CardPreferenceUpdateInputModel
/// {
///     CardId = cardId,
///     TargetIsExcludedFromDraw = true
/// };
/// </code>
/// </example>
public sealed class CardPreferenceUpdateInputModel : IValidatableObject
{
    /// <summary>
    /// Gets or sets the card to update.
    /// </summary>
    public Guid CardId { get; set; }

    /// <summary>
    /// Gets or sets the favorite target state when the request changes favorite state.
    /// </summary>
    public bool? TargetIsFavorite { get; set; }

    /// <summary>
    /// Gets or sets the draw exclusion target state when the request changes draw eligibility.
    /// </summary>
    public bool? TargetIsExcludedFromDraw { get; set; }

    /// <summary>
    /// Gets or sets the local return URL used by Razor Pages after a mutation.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the draw operation ID used to restore a revealed result.
    /// </summary>
    public Guid? DrawOperationId { get; set; }

    /// <summary>
    /// Gets or sets the result card ID used to restore a revealed result.
    /// </summary>
    public Guid? ResultCardId { get; set; }

    /// <summary>
    /// Validates that the update targets one card and at least one final state.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>Validation failures when the input cannot be applied safely.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CardId == Guid.Empty)
        {
            yield return new ValidationResult("Card ID is required.", new[] { nameof(CardId) });
        }

        if (TargetIsFavorite is null && TargetIsExcludedFromDraw is null)
        {
            yield return new ValidationResult(
                "At least one preference target state is required.",
                new[] { nameof(TargetIsFavorite), nameof(TargetIsExcludedFromDraw) });
        }
    }
}
