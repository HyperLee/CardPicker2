using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class DrawIdempotencyTests
{
    [Fact]
    public async Task DrawAsync_WithSameSuccessfulOperationId_ReplaysOriginalCardWithoutAppendingHistory()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync();
        var randomizer = new SequenceMealCardRandomizer(0, 1);
        var service = CreateService(library.FilePath, randomizer);
        var operation = new DrawOperation
        {
            OperationId = DrawFeatureTestData.ReplayOperationId,
            Mode = DrawMode.Random,
            CoinInserted = true,
            RequestedLanguage = SupportedLanguage.ZhTw
        };

        var first = await service.DrawAsync(operation);
        var second = await service.DrawAsync(operation);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.CardId, second.CardId);
        Assert.False(first.IsReplay);
        Assert.True(second.IsReplay);
        Assert.Equal(1, randomizer.CallCount);
        Assert.Single((await ReadDocumentAsync(library.FilePath)).DrawHistory);
    }

    [Fact]
    public async Task DrawAsync_ReplayDoesNotReapplyNewFilters()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV4Document());
        var randomizer = new SequenceMealCardRandomizer(0, 0);
        var service = CreateService(library.FilePath, randomizer);
        var operationId = Guid.NewGuid();

        var first = await service.DrawAsync(new DrawOperation
        {
            OperationId = operationId,
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            Filters = new CardFilterCriteria { Tags = new[] { "蔬食", "便當" } }
        });
        var second = await service.DrawAsync(new DrawOperation
        {
            OperationId = operationId,
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            Filters = new CardFilterCriteria { Tags = new[] { "不存在" } }
        });

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.CardId, second.CardId);
        Assert.True(second.IsReplay);
        Assert.Equal(1, randomizer.CallCount);
        Assert.Single((await ReadDocumentAsync(library.FilePath)).DrawHistory);
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

        public int CallCount { get; private set; }

        public int NextIndex(int count)
        {
            CallCount++;
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

        public static async Task<TempCardLibrary> CreateWithDocumentAsync()
        {
            return await CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document());
        }

        public static async Task<TempCardLibrary> CreateWithDocumentAsync(object document)
        {
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-idempotency-tests-").FullName);
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
