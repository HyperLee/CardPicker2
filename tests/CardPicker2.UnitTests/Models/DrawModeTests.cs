using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class DrawModeTests
{
    [Fact]
    public void DrawOperation_WithInvalidMode_IsNotDrawable()
    {
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = (DrawMode)999,
            MealType = MealType.Breakfast,
            CoinInserted = true
        };

        Assert.False(operation.HasValidMode);
        Assert.False(operation.CanAttemptDraw);
    }

    [Fact]
    public void DrawOperation_WithNormalModeAndMissingMealType_IsNotDrawable()
    {
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            CoinInserted = true
        };

        Assert.True(operation.RequiresMealType);
        Assert.False(operation.HasValidMealType);
        Assert.False(operation.CanAttemptDraw);
    }

    [Fact]
    public void DrawOperation_WithRandomMode_DoesNotRequireMealType()
    {
        var operation = new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            CoinInserted = true
        };

        Assert.False(operation.RequiresMealType);
        Assert.True(operation.HasValidMealType);
        Assert.True(operation.CanAttemptDraw);
    }

    [Fact]
    public void DrawOperation_WithEmptyOperationId_IsNotDrawable()
    {
        var operation = new DrawOperation
        {
            OperationId = Guid.Empty,
            Mode = DrawMode.Random,
            CoinInserted = true
        };

        Assert.False(operation.HasValidOperationId);
        Assert.False(operation.CanAttemptDraw);
    }
}
