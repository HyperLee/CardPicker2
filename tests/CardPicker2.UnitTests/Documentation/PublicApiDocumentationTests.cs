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

    [Theory]
    [InlineData("CardPicker2/Models/DrawMode.cs")]
    [InlineData("CardPicker2/Models/CardStatus.cs")]
    [InlineData("CardPicker2/Models/DrawOperation.cs")]
    [InlineData("CardPicker2/Models/DrawHistoryRecord.cs")]
    [InlineData("CardPicker2/Models/CardDrawStatistic.cs")]
    [InlineData("CardPicker2/Models/DrawStatisticsSummary.cs")]
    [InlineData("CardPicker2/Models/DrawResult.cs")]
    [InlineData("CardPicker2/Models/MealCard.cs")]
    [InlineData("CardPicker2/Models/CardLibraryDocument.cs")]
    [InlineData("CardPicker2/Models/PriceRange.cs")]
    [InlineData("CardPicker2/Models/PreparationTimeRange.cs")]
    [InlineData("CardPicker2/Models/DietaryPreference.cs")]
    [InlineData("CardPicker2/Models/SpiceLevel.cs")]
    [InlineData("CardPicker2/Models/MealCardDecisionMetadata.cs")]
    [InlineData("CardPicker2/Models/CardFilterCriteria.cs")]
    [InlineData("CardPicker2/Models/FilterSummary.cs")]
    [InlineData("CardPicker2/Models/MealCardInputModel.cs")]
    [InlineData("CardPicker2/Services/ICardLibraryService.cs")]
    [InlineData("CardPicker2/Services/CardLibraryService.cs")]
    [InlineData("CardPicker2/Services/CardLibraryFileCoordinator.cs")]
    [InlineData("CardPicker2/Services/DrawCandidatePoolBuilder.cs")]
    [InlineData("CardPicker2/Services/DrawStatisticsService.cs")]
    [InlineData("CardPicker2/Services/MealCardMetadataValidator.cs")]
    [InlineData("CardPicker2/Services/MealCardFilterService.cs")]
    [InlineData("CardPicker2/Services/MealCardLocalizationService.cs")]
    public async Task DrawFeaturePublicApiDocumentation_IncludesSummaryExampleAndCode(string relativePath)
    {
        var source = await File.ReadAllTextAsync(Path.Combine(GetRepositoryRoot(), relativePath));

        Assert.Contains("/// <summary>", source, StringComparison.Ordinal);
        Assert.Contains("/// <example>", source, StringComparison.Ordinal);
        Assert.Contains("/// <code>", source, StringComparison.Ordinal);
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
