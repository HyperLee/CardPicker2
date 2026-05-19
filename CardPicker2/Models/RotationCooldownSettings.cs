namespace CardPicker2.Models;

/// <summary>
/// Represents the per-draw setting that excludes recently drawn cards from the current candidate pool.
/// </summary>
/// <example>
/// <code>
/// var settings = new RotationCooldownSettings(avoidRecentRepeats: true, recentDrawCount: 3);
/// if (settings.IsActive)
/// {
///     // Apply recent successful draw exclusion.
/// }
/// </code>
/// </example>
public sealed record RotationCooldownSettings(bool AvoidRecentRepeats = true, int RecentDrawCount = 3)
{
    /// <summary>
    /// Gets the minimum accepted recent successful draw count.
    /// </summary>
    public const int MinRecentDrawCount = 0;

    /// <summary>
    /// Gets the maximum accepted recent successful draw count.
    /// </summary>
    public const int MaxRecentDrawCount = 10;

    /// <summary>
    /// Gets the default per-draw rotation cooldown setting.
    /// </summary>
    public static RotationCooldownSettings Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether recent cards should be excluded.
    /// </summary>
    public bool IsActive => AvoidRecentRepeats && RecentDrawCount > 0 && IsValid;

    /// <summary>
    /// Gets a value indicating whether the setting is inside the supported range.
    /// </summary>
    public bool IsValid => RecentDrawCount is >= MinRecentDrawCount and <= MaxRecentDrawCount;

    /// <summary>
    /// Gets the stable validation key when the setting is invalid.
    /// </summary>
    public string? ValidationKey => IsValid ? null : "Rotation.Validation.InvalidRecentDrawCount";

    /// <summary>
    /// Creates a setting from nullable form values while preserving default behavior for omitted fields.
    /// </summary>
    /// <param name="avoidRecentRepeats">The submitted toggle value.</param>
    /// <param name="recentDrawCount">The submitted recent successful draw count.</param>
    /// <param name="settings">The normalized settings, or defaults when invalid.</param>
    /// <param name="validationKey">The stable validation key when creation fails.</param>
    /// <param name="useDefaultsForMissing">Whether missing form values should use defaults.</param>
    /// <returns><see langword="true"/> when the values are valid.</returns>
    /// <example>
    /// <code>
    /// if (!RotationCooldownSettings.TryCreate(toggle, count, out var settings, out var key))
    /// {
    ///     ModelState.AddModelError(nameof(count), key);
    /// }
    /// </code>
    /// </example>
    public static bool TryCreate(
        bool? avoidRecentRepeats,
        int? recentDrawCount,
        out RotationCooldownSettings settings,
        out string? validationKey,
        bool useDefaultsForMissing = true)
    {
        if (!useDefaultsForMissing && recentDrawCount is null)
        {
            settings = Default;
            validationKey = "Rotation.Validation.InvalidRecentDrawCount";
            return false;
        }

        settings = new RotationCooldownSettings(
            avoidRecentRepeats ?? Default.AvoidRecentRepeats,
            recentDrawCount ?? Default.RecentDrawCount);
        validationKey = settings.ValidationKey;
        return settings.IsValid;
    }
}
