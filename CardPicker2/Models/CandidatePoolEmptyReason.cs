namespace CardPicker2.Models;

/// <summary>
/// Identifies why a submitted draw did not have a selectable candidate pool.
/// </summary>
/// <example>
/// <code>
/// var statusKey = reason == CandidatePoolEmptyReason.RotationCandidatePoolEmpty
///     ? "Rotation.Empty.AfterCooldown"
///     : "Draw.EmptyPool";
/// </code>
/// </example>
public enum CandidatePoolEmptyReason
{
    /// <summary>
    /// The draw mode, meal type, or metadata filters produced no base candidates.
    /// </summary>
    BaseCandidatePoolEmpty,

    /// <summary>
    /// Rotation cooldown excluded every base candidate.
    /// </summary>
    RotationCandidatePoolEmpty,

    /// <summary>
    /// The submitted rotation settings were invalid.
    /// </summary>
    InvalidRotationSettings
}
