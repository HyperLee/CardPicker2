namespace CardPicker2.Models;

/// <summary>
/// Represents the persisted summary of rotation cooldown membership for one successful draw.
/// </summary>
/// <example>
/// <code>
/// var snapshot = RotationSnapshot.Create(settings, preRotationCandidateCount: 5, excludedCandidateCount: 2);
/// var postCount = snapshot.PostRotationCandidateCount;
/// </code>
/// </example>
public sealed class RotationSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RotationSnapshot"/> class.
    /// </summary>
    public RotationSnapshot(
        bool avoidRecentRepeats,
        int recentDrawCount,
        int preRotationCandidateCount,
        int excludedCandidateCount,
        int postRotationCandidateCount)
    {
        AvoidRecentRepeats = avoidRecentRepeats;
        RecentDrawCount = recentDrawCount;
        PreRotationCandidateCount = preRotationCandidateCount;
        ExcludedCandidateCount = excludedCandidateCount;
        PostRotationCandidateCount = postRotationCandidateCount;
    }

    /// <summary>
    /// Gets whether the user enabled recent-repeat avoidance for the successful draw.
    /// </summary>
    public bool AvoidRecentRepeats { get; init; }

    /// <summary>
    /// Gets the recent successful draw count submitted for the successful draw.
    /// </summary>
    public int RecentDrawCount { get; init; }

    /// <summary>
    /// Gets the candidate count after draw mode and metadata filters but before rotation exclusion.
    /// </summary>
    public int PreRotationCandidateCount { get; init; }

    /// <summary>
    /// Gets the active candidate count excluded by recent draw history.
    /// </summary>
    public int ExcludedCandidateCount { get; init; }

    /// <summary>
    /// Gets the final candidate count after rotation exclusion.
    /// </summary>
    public int PostRotationCandidateCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the snapshot counts are internally consistent.
    /// </summary>
    public bool IsValid =>
        RecentDrawCount is >= RotationCooldownSettings.MinRecentDrawCount and <= RotationCooldownSettings.MaxRecentDrawCount &&
        PreRotationCandidateCount >= 0 &&
        ExcludedCandidateCount >= 0 &&
        PostRotationCandidateCount >= 0 &&
        ExcludedCandidateCount <= PreRotationCandidateCount &&
        PostRotationCandidateCount == PreRotationCandidateCount - ExcludedCandidateCount;

    /// <summary>
    /// Gets the stable validation key when the snapshot is invalid.
    /// </summary>
    public string? ValidationKey => IsValid ? null : "Rotation.Validation.InvalidSnapshotCounts";

    /// <summary>
    /// Creates a snapshot from validated settings and candidate counts.
    /// </summary>
    /// <param name="settings">The submitted rotation settings.</param>
    /// <param name="preRotationCandidateCount">The candidate count before rotation exclusion.</param>
    /// <param name="excludedCandidateCount">The candidate count removed by rotation exclusion.</param>
    /// <returns>The persisted rotation snapshot.</returns>
    /// <example>
    /// <code>
    /// var snapshot = RotationSnapshot.Create(settings, pool.Count, excluded.Count);
    /// </code>
    /// </example>
    public static RotationSnapshot Create(
        RotationCooldownSettings settings,
        int preRotationCandidateCount,
        int excludedCandidateCount)
    {
        return new RotationSnapshot(
            settings.AvoidRecentRepeats,
            settings.RecentDrawCount,
            preRotationCandidateCount,
            excludedCandidateCount,
            preRotationCandidateCount - excludedCandidateCount);
    }
}
