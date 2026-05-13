namespace CardPicker2.Models;

/// <summary>
/// Describes the manually maintained spice level for a meal card, ordered from none to hot.
/// </summary>
/// <example>
/// <code>
/// var maxAcceptedSpice = SpiceLevel.Mild;
/// var isAllowed = cardSpice &lt;= maxAcceptedSpice;
/// </code>
/// </example>
public enum SpiceLevel
{
    /// <summary>
    /// No spice.
    /// </summary>
    None = 0,

    /// <summary>
    /// Mild spice.
    /// </summary>
    Mild = 1,

    /// <summary>
    /// Medium spice.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Hot spice.
    /// </summary>
    Hot = 3
}
