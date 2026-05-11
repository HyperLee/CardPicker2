namespace CardPicker2.Services;

/// <summary>
/// Produces unbiased card-pool indexes for meal draws.
/// </summary>
public interface IMealCardRandomizer
{
    /// <summary>
    /// Returns one index in the range <c>[0, count)</c>.
    /// </summary>
    /// <param name="count">The number of cards in the eligible pool.</param>
    /// <returns>An index inside the eligible pool.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is less than one.</exception>
    int NextIndex(int count);
}
