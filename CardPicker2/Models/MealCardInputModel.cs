using System.ComponentModel.DataAnnotations;

namespace CardPicker2.Models;

/// <summary>
/// Captures user input for creating or editing a meal card.
/// </summary>
public sealed class MealCardInputModel : IValidatableObject
{
    /// <summary>
    /// Gets or sets the meal name.
    /// </summary>
    [Display(Name = "餐點名稱")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the selected meal type.
    /// </summary>
    [Display(Name = "餐別")]
    public MealType? MealType { get; set; }

    /// <summary>
    /// Gets or sets the full meal description.
    /// </summary>
    [Display(Name = "描述")]
    public string? Description { get; set; }

    /// <summary>
    /// Returns a copy whose text fields are trimmed.
    /// </summary>
    /// <returns>A normalized input model.</returns>
    public MealCardInputModel Normalize()
    {
        return new MealCardInputModel
        {
            Name = Name?.Trim(),
            MealType = MealType,
            Description = Description?.Trim()
        };
    }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("請輸入餐點名稱。", new[] { nameof(Name) });
        }

        if (MealType is null)
        {
            yield return new ValidationResult("請選擇早餐、午餐或晚餐。", new[] { nameof(MealType) });
        }
        else if (!Enum.IsDefined(typeof(MealType), MealType.Value))
        {
            yield return new ValidationResult("餐別必須是早餐、午餐或晚餐。", new[] { nameof(MealType) });
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult("請輸入餐點描述。", new[] { nameof(Description) });
        }
    }
}