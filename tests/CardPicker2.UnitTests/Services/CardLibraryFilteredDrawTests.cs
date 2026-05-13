using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryFilteredDrawTests
{
    [Fact]
    public async Task DrawAsync_WithFilters_DrawsOnlyFromFilteredPoolAndRecordsHistory()
    {
        using var library = await TempCardLibrary.CreateWithSchemaV4Async();
        var service = CreateService(library.FilePath, new SequenceMealCardRandomizer(0));

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            Filters = new CardFilterCriteria
            {
                Tags = new[] { "蔬食", "便當" },
                DietaryPreferences = new[] { DietaryPreference.Vegetarian },
                MaxSpiceLevel = SpiceLevel.Mild
            }
        });

        Assert.True(result.Succeeded);
        Assert.Equal(DrawFeatureTestData.VegetarianLunchCardId, result.CardId);
        Assert.Equal(1, result.FilteredPoolSize);
        Assert.Single((await ReadDocumentAsync(library.FilePath)).DrawHistory);
    }

    [Fact]
    public async Task DrawAsync_WhenFilteredPoolIsEmpty_DoesNotAppendHistory()
    {
        using var library = await TempCardLibrary.CreateWithSchemaV4Async();
        var service = CreateService(library.FilePath, new SequenceMealCardRandomizer(0));

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Breakfast,
            CoinInserted = true,
            Filters = new CardFilterCriteria
            {
                PriceRange = PriceRange.High,
                Tags = new[] { "不存在" }
            }
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Metadata.Filter.EmptyPool", result.StatusKey);
        Assert.Empty((await ReadDocumentAsync(library.FilePath)).DrawHistory);
    }

    private static CardLibraryService CreateService(string filePath, IMealCardRandomizer randomizer)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            randomizer,
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance,
            new MealCardLocalizationService(),
            new CardLibraryFileCoordinator(),
            new DrawCandidatePoolBuilder(new MealCardFilterService()),
            new DrawStatisticsService(new MealCardLocalizationService()),
            new MealCardMetadataValidator());
    }

    private static async Task<CardLibraryDocument> ReadDocumentAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<CardLibraryDocument>(json, DrawFeatureTestData.JsonOptions)!;
    }

    private sealed class SequenceMealCardRandomizer : IMealCardRandomizer
    {
        private readonly Queue<int> _indices;

        public SequenceMealCardRandomizer(params int[] indices)
        {
            _indices = new Queue<int>(indices);
        }

        public int NextIndex(int count)
        {
            var index = _indices.Dequeue();
            Assert.InRange(index, 0, count - 1);
            return index;
        }
    }

    private sealed class TempCardLibrary : IDisposable
    {
        private TempCardLibrary(string directoryPath)
        {
            DirectoryPath = directoryPath;
            FilePath = Path.Combine(directoryPath, "cards.json");
        }

        public string DirectoryPath { get; }

        public string FilePath { get; }

        public static async Task<TempCardLibrary> CreateWithSchemaV4Async()
        {
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-filtered-draw-tests-").FullName);
            await File.WriteAllTextAsync(
                library.FilePath,
                JsonSerializer.Serialize(DrawFeatureTestData.SchemaV4Document(), DrawFeatureTestData.JsonOptions));
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
