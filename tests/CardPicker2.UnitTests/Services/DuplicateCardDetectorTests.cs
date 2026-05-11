using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class DuplicateCardDetectorTests
{
    [Fact]
    public void HasDuplicate_TrimsAndIgnoresCaseForNameAndDescription()
    {
        var existing = new[]
        {
            new MealCard(Guid.NewGuid(), " 鮪魚蛋餅 ", MealType.Breakfast, " 加一杯無糖豆漿 ")
        };

        var input = new MealCardInputModel
        {
            Name = "鮪魚蛋餅",
            MealType = MealType.Breakfast,
            Description = "加一杯無糖豆漿"
        };

        var detector = new DuplicateCardDetector();

        Assert.True(detector.HasDuplicate(existing, input));
    }

    [Fact]
    public void HasDuplicate_AllowsSameNameWithDifferentDescription()
    {
        var existing = new[]
        {
            new MealCard(Guid.NewGuid(), "鮪魚蛋餅", MealType.Breakfast, "加一杯無糖豆漿")
        };

        var input = new MealCardInputModel
        {
            Name = "鮪魚蛋餅",
            MealType = MealType.Breakfast,
            Description = "改搭熱紅茶"
        };

        var detector = new DuplicateCardDetector();

        Assert.False(detector.HasDuplicate(existing, input));
    }

    [Fact]
    public void HasDuplicate_IgnoresTheEditedCardId()
    {
        var id = Guid.NewGuid();
        var existing = new[]
        {
            new MealCard(id, "牛肉麵", MealType.Lunch, "清燉湯頭")
        };

        var input = new MealCardInputModel
        {
            Name = "牛肉麵",
            MealType = MealType.Lunch,
            Description = "清燉湯頭"
        };

        var detector = new DuplicateCardDetector();

        Assert.False(detector.HasDuplicate(existing, input, ignoredCardId: id));
    }
}