namespace CardPicker2.Models;

/// <summary>
/// Defines how a draw builds its fair candidate pool.
/// </summary>
/// <example>
/// <code>
/// var mode = DrawMode.Normal;
/// </code>
/// </example>
public enum DrawMode
{
    /// <summary>
    /// Draw only from active cards that match the requested meal type.
    /// </summary>
    Normal,

    /// <summary>
    /// Draw from all active cards, ignoring any submitted meal type.
    /// </summary>
    Random
}
