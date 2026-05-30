using CardPicker2.Models;
using CardPicker2.Services;

namespace CardPicker2.UnitTests.Services;

public sealed class DuplicateCardDetectorPreferenceTests
{
    [Fact]
    public void HasDuplicate_IgnoresPreferenceStateAndDeletedCards()
    {
        var active = CreateCard(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CardStatus.Active,
            new CardPreferenceState { IsFavorite = true, IsExcludedFromDraw = true });
        var deleted = CreateCard(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            CardStatus.Deleted,
            new CardPreferenceState { IsFavorite = false, IsExcludedFromDraw = false },
            deletedAtUtc: DateTimeOffset.UtcNow);
        var input = new MealCardInputModel
        {
            NameZhTw = "  重複午餐  ",
            DescriptionZhTw = " 重複描述 ",
            NameEnUs = "Duplicate Lunch",
            DescriptionEnUs = "Duplicate description",
            MealType = MealType.Lunch
        };
        var detector = new DuplicateCardDetector();

        Assert.True(detector.HasDuplicate(new[] { active }, input));
        Assert.False(detector.HasDuplicate(new[] { deleted }, input));
    }

    private static MealCard CreateCard(
        Guid id,
        CardStatus status,
        CardPreferenceState preferences,
        DateTimeOffset? deletedAtUtc = null)
    {
        return new MealCard(
            id,
            MealType.Lunch,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new("重複午餐", "重複描述"),
                [SupportedLanguage.EnUs.CultureName] = new("Duplicate Lunch", "Duplicate description")
            },
            status,
            deletedAtUtc)
        {
            Preferences = preferences
        };
    }
}
