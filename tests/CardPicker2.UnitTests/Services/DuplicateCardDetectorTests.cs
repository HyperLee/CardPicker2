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

    [Fact]
    public void HasDuplicate_RejectsEnglishLocalizedDuplicate()
    {
        var existing = new[]
        {
            new MealCard(
                Guid.NewGuid(),
                MealType.Lunch,
                new Dictionary<string, MealCardLocalizedContent>
                {
                    [SupportedLanguage.ZhTw.CultureName] = new("牛肉麵", "清燉湯頭"),
                    [SupportedLanguage.EnUs.CultureName] = new("Beef Noodle Soup", "Clear broth")
                })
        };

        var input = new MealCardInputModel
        {
            NameZhTw = "番茄義大利麵",
            DescriptionZhTw = "番茄醬汁",
            NameEnUs = " beef noodle soup ",
            DescriptionEnUs = " clear broth ",
            MealType = MealType.Lunch
        };

        var detector = new DuplicateCardDetector();

        Assert.True(detector.HasDuplicate(existing, input));
    }

    [Fact]
    public void HasDuplicate_UsesFallbackEnglishCandidateForMissingTranslationCards()
    {
        var existing = new[]
        {
            new MealCard(Guid.NewGuid(), "鮪魚蛋餅", MealType.Breakfast, "加一杯無糖豆漿")
        };

        var input = new MealCardInputModel
        {
            NameZhTw = "花生厚片",
            DescriptionZhTw = "搭配熱紅茶",
            NameEnUs = "鮪魚蛋餅",
            DescriptionEnUs = "加一杯無糖豆漿",
            MealType = MealType.Breakfast
        };

        var detector = new DuplicateCardDetector();

        Assert.True(detector.HasDuplicate(existing, input));
    }
}
