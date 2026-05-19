using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryRotationSnapshotTests
{
    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    public async Task DrawAsync_SuccessfulDrawPersistsNonNullRotationSnapshot(bool avoidRecentRepeats, int recentDrawCount)
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV4Document());
        var service = CreateService(library.FilePath, new FixedMealCardRandomizer(0));

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            CoinInserted = true,
            RotationCooldown = new RotationCooldownSettings(avoidRecentRepeats, recentDrawCount)
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.RotationSnapshot);
        var history = Assert.Single((await ReadDocumentAsync(library.FilePath)).DrawHistory);
        Assert.NotNull(history.RotationSnapshot);
        Assert.Equal(avoidRecentRepeats, history.RotationSnapshot.AvoidRecentRepeats);
        Assert.Equal(recentDrawCount, history.RotationSnapshot.RecentDrawCount);
    }

    [Fact]
    public async Task DrawAsync_WhenWriteFails_DoesNotDeclareSuccessOrAppendSnapshot()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV4Document());
        Directory.CreateDirectory(library.FilePath + ".tmp");
        var original = await File.ReadAllTextAsync(library.FilePath);
        var service = CreateService(library.FilePath, new FixedMealCardRandomizer(0));

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            CoinInserted = true,
            RotationCooldown = RotationCooldownSettings.Default
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Draw.WriteFailed", result.StatusKey);
        Assert.Equal(original, await File.ReadAllTextAsync(library.FilePath));
    }

    private static CardLibraryService CreateService(string filePath, IMealCardRandomizer randomizer)
    {
        var localizationService = new MealCardLocalizationService();
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            randomizer,
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

    private sealed class FixedMealCardRandomizer : IMealCardRandomizer
    {
        private readonly int _index;

        public FixedMealCardRandomizer(int index)
        {
            _index = index;
        }

        public int NextIndex(int count)
        {
            Assert.InRange(_index, 0, count - 1);
            return _index;
        }
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
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-rotation-snapshot-tests-").FullName);
            await File.WriteAllTextAsync(library.FilePath, JsonSerializer.Serialize(document, DrawFeatureTestData.JsonOptions));
            return library;
        }

        public void Dispose()
        {
            var tempPath = FilePath + ".tmp";
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath);
            }

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
