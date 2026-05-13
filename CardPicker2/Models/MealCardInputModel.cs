using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace CardPicker2.Models;

/// <summary>
/// Captures user input for creating or editing a bilingual meal card.
/// </summary>
public sealed class MealCardInputModel : IValidatableObject
{
    /// <summary>
    /// Gets or sets the legacy meal name alias. Setting this value fills both language names for backward compatibility.
    /// </summary>
    [Display(Name = "餐點名稱")]
    public string? Name
    {
        get => NameZhTw;
        set
        {
            NameZhTw = value;
            NameEnUs ??= value;
        }
    }

    /// <summary>
    /// Gets or sets the Traditional Chinese meal name.
    /// </summary>
    [Display(Name = "繁體中文餐點名稱")]
    public string? NameZhTw { get; set; }

    /// <summary>
    /// Gets or sets the Traditional Chinese meal description.
    /// </summary>
    [Display(Name = "繁體中文餐點描述")]
    public string? DescriptionZhTw { get; set; }

    /// <summary>
    /// Gets or sets the English meal name.
    /// </summary>
    [Display(Name = "英文餐點名稱")]
    public string? NameEnUs { get; set; }

    /// <summary>
    /// Gets or sets the English meal description.
    /// </summary>
    [Display(Name = "英文餐點描述")]
    public string? DescriptionEnUs { get; set; }

    /// <summary>
    /// Gets or sets the selected meal type.
    /// </summary>
    [Display(Name = "餐別")]
    public MealType? MealType { get; set; }

    /// <summary>
    /// Gets or sets comma-separated decision tags.
    /// </summary>
    [Display(Name = "標籤")]
    public string? TagsInput { get; set; }

    /// <summary>
    /// Gets or sets the optional price range.
    /// </summary>
    [Display(Name = "價格區間")]
    public PriceRange? PriceRange { get; set; }

    /// <summary>
    /// Gets or sets the optional preparation time range.
    /// </summary>
    [Display(Name = "準備時間")]
    public PreparationTimeRange? PreparationTimeRange { get; set; }

    /// <summary>
    /// Gets or sets the selected dietary preferences.
    /// </summary>
    [Display(Name = "飲食偏好")]
    public List<DietaryPreference> DietaryPreferences { get; set; } = new();

    /// <summary>
    /// Gets or sets the optional spice level.
    /// </summary>
    [Display(Name = "辣度")]
    public SpiceLevel? SpiceLevel { get; set; }

    /// <summary>
    /// Gets or sets the legacy description alias. Setting this value fills both language descriptions for backward compatibility.
    /// </summary>
    [Display(Name = "描述")]
    public string? Description
    {
        get => DescriptionZhTw;
        set
        {
            DescriptionZhTw = value;
            DescriptionEnUs ??= value;
        }
    }

    /// <summary>
    /// Returns a copy whose text fields are trimmed.
    /// </summary>
    /// <returns>A normalized input model.</returns>
    public MealCardInputModel Normalize()
    {
        return new MealCardInputModel
        {
            NameZhTw = NameZhTw?.Trim(),
            DescriptionZhTw = DescriptionZhTw?.Trim(),
            NameEnUs = NameEnUs?.Trim(),
            DescriptionEnUs = DescriptionEnUs?.Trim(),
            MealType = MealType,
            TagsInput = TagsInput?.Trim(),
            PriceRange = PriceRange,
            PreparationTimeRange = PreparationTimeRange,
            DietaryPreferences = DietaryPreferences
                .Distinct()
                .OrderBy(preference => preference)
                .ToList(),
            SpiceLevel = SpiceLevel
        };
    }

    /// <summary>
    /// Converts the normalized input into localized content.
    /// </summary>
    /// <returns>A dictionary keyed by supported culture name.</returns>
    public Dictionary<string, MealCardLocalizedContent> ToLocalizations()
    {
        var normalized = Normalize();
        return new Dictionary<string, MealCardLocalizedContent>(StringComparer.OrdinalIgnoreCase)
        {
            [SupportedLanguage.ZhTw.CultureName] = new(normalized.NameZhTw ?? string.Empty, normalized.DescriptionZhTw ?? string.Empty),
            [SupportedLanguage.EnUs.CultureName] = new(normalized.NameEnUs ?? string.Empty, normalized.DescriptionEnUs ?? string.Empty)
        };
    }

    /// <summary>
    /// Converts optional form metadata into a normalized decision metadata value.
    /// </summary>
    /// <returns>Normalized metadata, or <see langword="null"/> when no metadata was supplied.</returns>
    public MealCardDecisionMetadata? ToDecisionMetadata()
    {
        var normalized = Normalize();
        var metadata = new MealCardDecisionMetadata
        {
            Tags = SplitTags(normalized.TagsInput),
            PriceRange = normalized.PriceRange,
            PreparationTimeRange = normalized.PreparationTimeRange,
            DietaryPreferences = normalized.DietaryPreferences,
            SpiceLevel = normalized.SpiceLevel
        }.Normalize();

        return metadata.Tags.Count == 0 &&
            metadata.PriceRange is null &&
            metadata.PreparationTimeRange is null &&
            metadata.DietaryPreferences.Count == 0 &&
            metadata.SpiceLevel is null
                ? null
                : metadata;
    }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NameZhTw))
        {
            yield return new ValidationResult(Localize("請輸入餐點名稱。", "Enter the meal name."), new[] { nameof(NameZhTw), nameof(Name) });
        }

        if (string.IsNullOrWhiteSpace(DescriptionZhTw))
        {
            yield return new ValidationResult(Localize("請輸入餐點描述。", "Enter the meal description."), new[] { nameof(DescriptionZhTw), nameof(Description) });
        }

        if (string.IsNullOrWhiteSpace(NameEnUs))
        {
            yield return new ValidationResult(Localize("請輸入英文餐點名稱。", "Enter the English meal name."), new[] { nameof(NameEnUs) });
        }

        if (string.IsNullOrWhiteSpace(DescriptionEnUs))
        {
            yield return new ValidationResult(Localize("請輸入英文餐點描述。", "Enter the English meal description."), new[] { nameof(DescriptionEnUs) });
        }

        if (MealType is null)
        {
            yield return new ValidationResult(Localize("請選擇早餐、午餐或晚餐。", "Choose breakfast, lunch, or dinner."), new[] { nameof(MealType) });
        }
        else if (!Enum.IsDefined(typeof(MealType), MealType.Value))
        {
            yield return new ValidationResult(Localize("餐別必須是早餐、午餐或晚餐。", "Meal type must be breakfast, lunch, or dinner."), new[] { nameof(MealType) });
        }

        if (PriceRange is PriceRange priceRange && !Enum.IsDefined(typeof(PriceRange), priceRange))
        {
            yield return new ValidationResult(Localize("決策資訊選項不支援。", "That decision metadata option is not supported."), new[] { nameof(PriceRange) });
        }

        if (PreparationTimeRange is PreparationTimeRange preparationTimeRange &&
            !Enum.IsDefined(typeof(PreparationTimeRange), preparationTimeRange))
        {
            yield return new ValidationResult(Localize("決策資訊選項不支援。", "That decision metadata option is not supported."), new[] { nameof(PreparationTimeRange) });
        }

        if (SpiceLevel is SpiceLevel spiceLevel && !Enum.IsDefined(typeof(SpiceLevel), spiceLevel))
        {
            yield return new ValidationResult(Localize("決策資訊選項不支援。", "That decision metadata option is not supported."), new[] { nameof(SpiceLevel) });
        }

        if (DietaryPreferences.Any(preference => !Enum.IsDefined(typeof(DietaryPreference), preference)))
        {
            yield return new ValidationResult(Localize("決策資訊選項不支援。", "That decision metadata option is not supported."), new[] { nameof(DietaryPreferences) });
        }
    }

    private static string Localize(string zhTw, string enUs)
    {
        return string.Equals(CultureInfo.CurrentUICulture.Name, SupportedLanguage.EnUs.CultureName, StringComparison.OrdinalIgnoreCase)
            ? enUs
            : zhTw;
    }

    private static IReadOnlyList<string> SplitTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToList();
    }
}
