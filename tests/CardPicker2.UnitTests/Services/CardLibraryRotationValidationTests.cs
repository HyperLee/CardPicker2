using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryRotationValidationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task DrawAsync_WithInvalidRecentDrawCount_RejectsWithoutAppendingHistory(int recentDrawCount)
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV4Document());
        var original = await File.ReadAllTextAsync(library.FilePath);
        var service = CreateService(library.FilePath);

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            CoinInserted = true,
            RotationCooldown = new RotationCooldownSettings(true, recentDrawCount)
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Rotation.Validation.InvalidRecentDrawCount", result.StatusKey);
        Assert.Equal(CandidatePoolEmptyReason.InvalidRotationSettings, result.CandidatePoolEmptyReason);
        Assert.Equal(original, await File.ReadAllTextAsync(library.FilePath));
        Assert.Empty((await ReadDocumentAsync(library.FilePath)).DrawHistory);
    }

    private static CardLibraryService CreateService(string filePath)
    {
        var localizationService = new MealCardLocalizationService();
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            new MealCardRandomizer(),
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance,
            localizationService,
            new CardLibraryFileCoordinator(),
            new DrawCandidatePoolBuilder(new MealCardFilterService()),
            new DrawStatisticsService(localizationService),
            new MealCardMetadataValidator(),
            new MealCardFilterService());
    }

    private static async Task<CardLibraryDocument> ReadDocumentAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<CardLibraryDocument>(json, DrawFeatureTestData.JsonOptions)!;
    }

    private sealed class TempCardLibrary : IDisposable
    {
        private TempCardLibrary(string directoryPath)
        {
            DirectoryPath = directoryPath;
            FilePath = Path.Combine(directoryPath, "cards.json");
        }

        private string DirectoryPath { get; }

        public string FilePath { get; }

        public static async Task<TempCardLibrary> CreateWithDocumentAsync(object document)
        {
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-rotation-validation-tests-").FullName);
            await File.WriteAllTextAsync(
                library.FilePath,
                JsonSerializer.Serialize(document, DrawFeatureTestData.JsonOptions));
            return library;
        }

        public void Dispose()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }

            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath);
            }
        }
    }
}
