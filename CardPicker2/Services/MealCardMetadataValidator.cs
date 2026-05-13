using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Validates and normalizes meal-card decision metadata before filtering or persistence.
/// </summary>
/// <example>
/// <code>
/// var result = validator.ValidateAndNormalize(card.DecisionMetadata);
/// if (result.Succeeded)
/// {
///     var metadata = result.Metadata;
/// }
/// </code>
/// </example>
public sealed class MealCardMetadataValidator
{
    /// <summary>
    /// Validates and normalizes optional decision metadata.
    /// </summary>
    /// <param name="metadata">The metadata to inspect, or <see langword="null"/> when unset.</param>
    /// <returns>The validation result.</returns>
    public MetadataValidationResult ValidateAndNormalize(MealCardDecisionMetadata? metadata)
    {
        if (metadata is null)
        {
            return MetadataValidationResult.Success(null);
        }

        if (metadata.PriceRange is PriceRange priceRange && !Enum.IsDefined(typeof(PriceRange), priceRange))
        {
            return MetadataValidationResult.Failure("Metadata.InvalidEnum");
        }

        if (metadata.PreparationTimeRange is PreparationTimeRange preparationTimeRange &&
            !Enum.IsDefined(typeof(PreparationTimeRange), preparationTimeRange))
        {
            return MetadataValidationResult.Failure("Metadata.InvalidEnum");
        }

        if (metadata.SpiceLevel is SpiceLevel spiceLevel && !Enum.IsDefined(typeof(SpiceLevel), spiceLevel))
        {
            return MetadataValidationResult.Failure("Metadata.InvalidEnum");
        }

        if (metadata.DietaryPreferences.Any(preference => !Enum.IsDefined(typeof(DietaryPreference), preference)))
        {
            return MetadataValidationResult.Failure("Metadata.InvalidEnum");
        }

        if (metadata.Tags.Any(tag => string.IsNullOrWhiteSpace(tag)))
        {
            return MetadataValidationResult.Failure("Metadata.InvalidTag");
        }

        return MetadataValidationResult.Success(metadata.Normalize());
    }
}

/// <summary>
/// Represents the result of metadata validation.
/// </summary>
/// <example>
/// <code>
/// if (!result.Succeeded)
/// {
///     var key = result.MessageKey;
/// }
/// </code>
/// </example>
/// <param name="Succeeded">Whether validation succeeded.</param>
/// <param name="Metadata">The normalized metadata when validation succeeded.</param>
/// <param name="MessageKey">The stable message key when validation failed.</param>
public sealed record MetadataValidationResult(
    bool Succeeded,
    MealCardDecisionMetadata? Metadata,
    string MessageKey)
{
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <param name="metadata">The normalized metadata.</param>
    /// <returns>A successful result.</returns>
    public static MetadataValidationResult Success(MealCardDecisionMetadata? metadata)
    {
        return new MetadataValidationResult(true, metadata, string.Empty);
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="messageKey">The stable message key.</param>
    /// <returns>A failed result.</returns>
    public static MetadataValidationResult Failure(string messageKey)
    {
        return new MetadataValidationResult(false, null, messageKey);
    }
}
