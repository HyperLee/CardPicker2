using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class DrawOperationStateTests
{
    [Fact]
    public void FromSelection_WithoutMealType_ReturnsIdle()
    {
        Assert.Equal(DrawOperationState.Idle, DrawOperationStateTransitions.FromSelection(null));
    }

    [Fact]
    public void FromSelection_WithMealType_ReturnsMealSelected()
    {
        Assert.Equal(DrawOperationState.MealSelected, DrawOperationStateTransitions.FromSelection(MealType.Breakfast));
    }

    [Fact]
    public void CanStartDraw_OnlyAllowsCoinInserted()
    {
        Assert.True(DrawOperationStateTransitions.CanStartDraw(DrawOperationState.CoinInserted));
        Assert.False(DrawOperationStateTransitions.CanStartDraw(DrawOperationState.Idle));
        Assert.False(DrawOperationStateTransitions.CanStartDraw(DrawOperationState.Spinning));
    }

    [Fact]
    public void DrawResultSuccess_MapsCardFieldsAndMealDisplayName()
    {
        var card = new MealCard(Guid.NewGuid(), "雞腿便當", MealType.Lunch, "烤雞腿便當搭配三樣配菜。");

        var result = DrawResult.Success(MealType.Lunch, card);

        Assert.True(result.Succeeded);
        Assert.Equal(card.Id, result.CardId);
        Assert.Equal("雞腿便當", result.Name);
        Assert.Equal("午餐", result.MealTypeDisplayName);
        Assert.Equal("烤雞腿便當搭配三樣配菜。", result.Description);
    }
}
