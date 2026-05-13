namespace CardPicker2.Models;

/// <summary>
/// Defines whether a meal card can participate in future draws.
/// </summary>
/// <example>
/// <code>
/// var status = CardStatus.Active;
/// </code>
/// </example>
public enum CardStatus
{
    /// <summary>
    /// The card is visible in the library and eligible for future draw candidate pools.
    /// </summary>
    Active,

    /// <summary>
    /// The card is retained only for successful draw history and statistics.
    /// </summary>
    Deleted
}
