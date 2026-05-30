using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class CardPreferenceStateTests
{
    [Fact]
    public void DefaultState_IsNotFavoriteAndDrawable()
    {
        var state = CardPreferenceState.Default;

        Assert.False(state.IsFavorite);
        Assert.False(state.IsExcludedFromDraw);
    }

    [Fact]
    public void MealCardDerivedPreferenceFlags_DoNotLetFavoriteAffectDrawability()
    {
        var favorite = CreateCard(new CardPreferenceState { IsFavorite = true });
        var excluded = CreateCard(new CardPreferenceState { IsFavorite = true, IsExcludedFromDraw = true });

        Assert.True(favorite.IsDrawable);
        Assert.True(favorite.IsPreferenceEditable);
        Assert.False(excluded.IsDrawable);
        Assert.True(excluded.IsPreferenceEditable);
    }

    [Fact]
    public void Normalize_PreservesPreferenceState()
    {
        var card = CreateCard(new CardPreferenceState { IsFavorite = true, IsExcludedFromDraw = true });

        var normalized = card.Normalize();

        Assert.True(normalized.Preferences.IsFavorite);
        Assert.True(normalized.Preferences.IsExcludedFromDraw);
    }

    private static MealCard CreateCard(CardPreferenceState preferences)
    {
        return new MealCard(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            MealType.Lunch,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new("測試卡", "測試描述"),
                [SupportedLanguage.EnUs.CultureName] = new("Test Card", "Test description")
            })
        {
            Preferences = preferences
        };
    }
}
