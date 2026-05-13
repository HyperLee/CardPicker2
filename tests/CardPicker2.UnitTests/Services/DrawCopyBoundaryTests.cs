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
            "guaranteed pull",
            "higher chance next time",
            "paid boost",
            "rarity"
        };

        foreach (var term in forbiddenTerms)
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
