namespace CardPicker2.Services;

/// <summary>
/// Uses the BCL shared random source to choose a card-pool index.
/// </summary>
public sealed class MealCardRandomizer : IMealCardRandomizer
{
    /// <inheritdoc />
    public int NextIndex(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "卡池必須至少包含一張卡牌。");
        }

        return Random.Shared.Next(count);
    }
}