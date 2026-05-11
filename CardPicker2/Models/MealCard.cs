namespace CardPicker2.Models;

/// <summary>
/// Represents one persisted meal option that can be browsed, searched, edited, deleted, or drawn.
/// </summary>
/// <param name="Id">The immutable system-generated card identifier.</param>
/// <param name="Name">The user-visible meal name.</param>
/// <param name="MealType">The meal period this card belongs to.</param>
/// <param name="Description">The complete meal description.</param>
public sealed record MealCard(Guid Id, string Name, MealType MealType, string Description);