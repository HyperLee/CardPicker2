using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class MealCardRandomizerTests
{
    [Fact]
    public void NextIndex_WithPositiveCount_ReturnsIndexInsideRange()
    {
        var randomizer = new MealCardRandomizer();

        for (var i = 0; i < 250; i++)
        {
            var index = randomizer.NextIndex(3);

            Assert.InRange(index, 0, 2);
        }
    }

    [Fact]
    public void NextIndex_WithEmptyPool_Throws()
    {
        var randomizer = new MealCardRandomizer();

        Assert.Throws<ArgumentOutOfRangeException>(() => randomizer.NextIndex(0));
    }
}
