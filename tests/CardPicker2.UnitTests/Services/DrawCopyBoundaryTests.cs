namespace CardPicker2.UnitTests.Services;

public sealed class DrawCopyBoundaryTests
{
    [Theory]
    [InlineData("CardPicker2/Resources/SharedResource.zh-TW.resx")]
    [InlineData("CardPicker2/Resources/SharedResource.en-US.resx")]
    public async Task SharedResources_DoNotContainMisleadingProbabilityOrPaymentCopy(string relativePath)
    {
        var source = await File.ReadAllTextAsync(Path.Combine(GetRepositoryRoot(), relativePath));
        var forbiddenTerms = new[]
        {
            "保底",
            "下一次機率更高",
            "連抽加成",
            "付費",
            "賭金",
            "偏好加權",
            "價值分級",
            "冷卻後更幸運",
            "guaranteed pull",
            "higher chance next time",
            "paid boost",
            "rarity",
            "preference weighting",
            "weighted preference",
            "value tier",
            "cooldown luck",
            "boosted odds"
        };

        foreach (var term in forbiddenTerms)
        {
            Assert.DoesNotContain(term, source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("CardPicker2/Resources/SharedResource.zh-TW.resx")]
    [InlineData("CardPicker2/Resources/SharedResource.en-US.resx")]
    public async Task PreferenceCopy_DoesNotSuggestFavoritesAffectDrawOdds(string relativePath)
    {
        var source = await File.ReadAllTextAsync(Path.Combine(GetRepositoryRoot(), relativePath));
        var forbiddenPreferenceClaims = new[]
        {
            "收藏提高",
            "收藏加權",
            "收藏後更容易",
            "偏好提高機率",
            "排除後補償",
            "favorite boost",
            "favorites increase",
            "favorite weighting",
            "more likely when favorited",
            "excluded compensation"
        };

        foreach (var term in forbiddenPreferenceClaims)
        {
            Assert.DoesNotContain(term, source, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CardPicker2.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
