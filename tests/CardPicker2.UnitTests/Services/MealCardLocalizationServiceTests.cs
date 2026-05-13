using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class MealCardLocalizationServiceTests
{
    [Fact]
    public void Project_UsesRequestedLanguageContentWhenPresent()
    {
        var card = CreateCompleteCard();
        var service = new MealCardLocalizationService();

        var view = service.Project(card, SupportedLanguage.EnUs);

        Assert.Equal(card.Id, view.CardId);
        Assert.Equal("Tuna Egg Pancake", view.DisplayName);
        Assert.Equal("Breakfast", view.MealTypeDisplayName);
        Assert.False(view.IsFallback);
        Assert.Empty(view.MissingTranslationCultures);
    }

    [Fact]
    public void Project_UsesTraditionalChineseFallbackWhenEnglishIsMissing()
    {
        var card = new MealCard(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "鮪魚蛋餅",
            MealType.Breakfast,
            "附近早餐店的鮪魚蛋餅。");
        var service = new MealCardLocalizationService();

        var view = service.Project(card, SupportedLanguage.EnUs);

        Assert.Equal("鮪魚蛋餅", view.DisplayName);
        Assert.Equal("附近早餐店的鮪魚蛋餅。", view.DisplayDescription);
        Assert.True(view.IsFallback);
        Assert.Contains("en-US", view.MissingTranslationCultures);
    }

    [Fact]
    public void Project_UsesTraditionalChineseMealLabelsInDefaultLanguage()
    {
        var card = CreateCompleteCard();
        var service = new MealCardLocalizationService();

        var view = service.Project(card, SupportedLanguage.ZhTw);

        Assert.Equal("鮪魚蛋餅", view.DisplayName);
        Assert.Equal("早餐", view.MealTypeDisplayName);
        Assert.False(view.IsFallback);
    }

    private static MealCard CreateCompleteCard()
    {
        return new MealCard(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MealType.Breakfast,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new("鮪魚蛋餅", "附近早餐店的鮪魚蛋餅。"),
                [SupportedLanguage.EnUs.CultureName] = new("Tuna Egg Pancake", "A tuna egg pancake from the nearby breakfast shop.")
            });
    }
}
