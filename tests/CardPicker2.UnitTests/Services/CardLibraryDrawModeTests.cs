using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryDrawModeTests
{
    [Fact]
    public async Task DrawAsync_WithNormalMode_AppendsHistoryForSelectedMealType()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync();
        var operation = CreateOperation(DrawMode.Normal, MealType.Lunch);
        var service = CreateService(library.FilePath, new FixedMealCardRandomizer(0));

        var result = await service.DrawAsync(operation);

        Assert.True(result.Succeeded);
        Assert.Equal(DrawFeatureTestData.LunchCardId, result.CardId);
        Assert.Equal(DrawMode.Normal, result.DrawMode);
        Assert.False(result.IsReplay);

        var persisted = await ReadDocumentAsync(library.FilePath);
        var history = Assert.Single(persisted.DrawHistory);
        Assert.Equal(operation.OperationId, history.OperationId);
        Assert.Equal(DrawMode.Normal, history.DrawMode);
        Assert.Equal(DrawFeatureTestData.LunchCardId, history.CardId);
    }

    [Fact]
    public async Task DrawAsync_WithRandomMode_UsesAllActiveCardsAndIgnoresSubmittedMealType()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync();
        var operation = CreateOperation(DrawMode.Random, MealType.Breakfast);
        var service = CreateService(library.FilePath, new FixedMealCardRandomizer(3));

        var result = await service.DrawAsync(operation);

        Assert.True(result.Succeeded);
        Assert.Equal(DrawFeatureTestData.DinnerCardId, result.CardId);
        Assert.Equal(DrawMode.Random, result.DrawMode);
        Assert.Null(result.RequestedMealType);
    }

    [Fact]
    public async Task DrawAsync_WithoutCoin_ReturnsFailureAndDoesNotAppendHistory()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync();
        var operation = new DrawOperation
        {
            OperationId = DrawFeatureTestData.FirstOperationId,
            Mode = DrawMode.Random,
            CoinInserted = false,
            RequestedLanguage = SupportedLanguage.ZhTw
        };
        var service = CreateService(library.FilePath);

        var result = await service.DrawAsync(operation);

        Assert.False(result.Succeeded);
        Assert.Equal("Draw.CoinRequired", result.StatusKey);
        Assert.Empty((await ReadDocumentAsync(library.FilePath)).DrawHistory);
    }

    [Fact]
    public async Task DrawAsync_WithInvalidMealType_ReturnsFailureAndDoesNotAppendHistory()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync();
        var operation = CreateOperation(DrawMode.Normal, (MealType)999);
        var service = CreateService(library.FilePath);

        var result = await service.DrawAsync(operation);

        Assert.False(result.Succeeded);
        Assert.Equal("Draw.InvalidMealType", result.StatusKey);
        Assert.Empty((await ReadDocumentAsync(library.FilePath)).DrawHistory);
    }

    [Fact]
    public async Task DrawAsync_WithEmptyPool_ReturnsFailureAndDoesNotAppendHistory()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document(
            cards: new[]
            {
                DrawFeatureTestData.SchemaV3Card(
                    DrawFeatureTestData.BreakfastCardId,
                    "Breakfast",
                    "Active",
                    "早餐卡",
                    "Breakfast Card")
            }));
        var operation = CreateOperation(DrawMode.Normal, MealType.Dinner);
        var service = CreateService(library.FilePath);

        var result = await service.DrawAsync(operation);

        Assert.False(result.Succeeded);
        Assert.Equal("Draw.EmptyPool", result.StatusKey);
        Assert.Empty((await ReadDocumentAsync(library.FilePath)).DrawHistory);
    }

    [Fact]
    public async Task DrawAsync_WhenLibraryIsBlocked_ReturnsFailure()
    {
        using var library = TempCardLibrary.Create();
        await File.WriteAllTextAsync(library.FilePath, "{");
        var service = CreateService(library.FilePath);

        var result = await service.DrawAsync(CreateOperation(DrawMode.Random, null));

        Assert.False(result.Succeeded);
        Assert.Equal("Library.BlockedCorrupt", result.StatusKey);
    }

    [Fact]
    public async Task DrawAsync_WhenWriteFails_DoesNotAppendHistoryOrChangeOriginalFile()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync();
        var original = await File.ReadAllTextAsync(library.FilePath);
        Directory.CreateDirectory(library.FilePath + ".tmp");
        var service = CreateService(library.FilePath);

        var result = await service.DrawAsync(CreateOperation(DrawMode.Random, null));

        Assert.False(result.Succeeded);
        Assert.Equal("Draw.WriteFailed", result.StatusKey);
        Assert.Equal(original, await File.ReadAllTextAsync(library.FilePath));
    }

    private static DrawOperation CreateOperation(DrawMode mode, MealType? mealType)
    {
        return new DrawOperation
        {
            OperationId = DrawFeatureTestData.FirstOperationId,
            Mode = mode,
            MealType = mealType,
            CoinInserted = true,
            RequestedLanguage = SupportedLanguage.ZhTw
        };
    }

    private static CardLibraryService CreateService(string filePath, IMealCardRandomizer? randomizer = null)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            randomizer ?? new FixedMealCardRandomizer(0),
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance,
            new MealCardLocalizationService(),
            new CardLibraryFileCoordinator());
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

        public string DirectoryPath { get; }

        public string FilePath { get; }

        public static TempCardLibrary Create()
        {
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-draw-mode-tests-").FullName);
        }

        public static async Task<TempCardLibrary> CreateWithDocumentAsync(object? document = null)
        {
            var library = Create();
            await File.WriteAllTextAsync(
                library.FilePath,
                JsonSerializer.Serialize(document ?? DrawFeatureTestData.SchemaV3Document(), DrawFeatureTestData.JsonOptions));
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
