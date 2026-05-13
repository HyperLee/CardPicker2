namespace CardPicker2.UnitTests.Documentation;

public sealed class PublicApiDocumentationTests
{
    [Theory]
    [InlineData("CardPicker2/Models/SupportedLanguage.cs")]
    [InlineData("CardPicker2/Models/MealCard.cs")]
    [InlineData("CardPicker2/Models/MealCardInputModel.cs")]
    [InlineData("CardPicker2/Services/LanguagePreferenceService.cs")]
    [InlineData("CardPicker2/Services/MealCardLocalizationService.cs")]
    [InlineData("CardPicker2/Services/CardLibraryService.cs")]
    public async Task PublicModelsAndServices_ContainXmlSummaryDocumentation(string relativePath)
    {
        var source = await File.ReadAllTextAsync(Path.Combine(GetRepositoryRoot(), relativePath));

        Assert.Contains("/// <summary>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TODO", source, StringComparison.OrdinalIgnoreCase);
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
