using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryRotationDrawTests
{
    [Fact]
    public async Task DrawAsync_WhenRotationEnabled_ExcludesRecentCandidatesAndPersistsSnapshot()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV4Document(
            drawHistory: RecentLunchHistory()));
        var service = CreateService(library.FilePath, new SequenceMealCardRandomizer(0));

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            RotationCooldown = RotationCooldownSettings.Default
        });

        Assert.True(result.Succeeded);
        Assert.Equal(DrawFeatureTestData.VegetarianLunchCardId, result.CardId);
        Assert.NotNull(result.RotationSnapshot);
        Assert.Equal(3, result.RotationSnapshot.PreRotationCandidateCount);
        Assert.Equal(2, result.RotationSnapshot.ExcludedCandidateCount);
        Assert.Equal(1, result.RotationSnapshot.PostRotationCandidateCount);

        var persisted = await ReadDocumentAsync(library.FilePath);
        var history = persisted.DrawHistory.Last();
        Assert.NotNull(history.RotationSnapshot);
        Assert.Equal(1, history.RotationSnapshot.PostRotationCandidateCount);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 3)]
    public async Task DrawAsync_WhenRotationInactive_UsesMetadataFilteredPoolWithoutRecentExclusion(
        bool avoidRecentRepeats,
        int recentDrawCount)
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV4Document(
            drawHistory: RecentLunchHistory()));
        var service = CreateService(library.FilePath, new SequenceMealCardRandomizer(0));

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            Filters = new CardFilterCriteria { Tags = new[] { "便當" } },
            RotationCooldown = new RotationCooldownSettings(avoidRecentRepeats, recentDrawCount)
        });

        Assert.True(result.Succeeded);
        Assert.Equal(DrawFeatureTestData.LowPriceLunchCardId, result.CardId);
        Assert.NotNull(result.RotationSnapshot);
        Assert.Equal(2, result.RotationSnapshot.PreRotationCandidateCount);
        Assert.Equal(0, result.RotationSnapshot.ExcludedCandidateCount);
        Assert.Equal(2, result.RotationSnapshot.PostRotationCandidateCount);
    }

    [Fact]
    public async Task DrawAsync_WhenRotationEmptiesPool_DoesNotAppendHistoryOrCallRandomizer()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV4Document(
            drawHistory: RecentLunchHistoryIncludingAllBentoCandidates()));
        var randomizer = new ThrowingMealCardRandomizer();
        var service = CreateService(library.FilePath, randomizer);

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            Filters = new CardFilterCriteria { Tags = new[] { "便當" } },
            RotationCooldown = RotationCooldownSettings.Default
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Rotation.Empty.AfterCooldown", result.StatusKey);
        Assert.Equal(CandidatePoolEmptyReason.RotationCandidatePoolEmpty, result.CandidatePoolEmptyReason);
        Assert.False(randomizer.WasCalled);
        Assert.Equal(RecentLunchHistoryIncludingAllBentoCandidates().Count, (await ReadDocumentAsync(library.FilePath)).DrawHistory.Count);
    }

    private static IReadOnlyList<object> RecentLunchHistory()
    {
        return new[]
        {
            DrawFeatureTestData.DrawHistoryWithRotationSnapshot(
                id: Guid.Parse("77777777-7777-7777-7777-777777777771"),
                operationId: Guid.Parse("88888888-8888-8888-8888-888888888881"),
                cardId: DrawFeatureTestData.LunchCardId,
                mealTypeAtDraw: "Lunch",
                succeededAtUtc: DrawFeatureTestData.KnownTimestamp().AddMinutes(1)),
            DrawFeatureTestData.DrawHistoryWithRotationSnapshot(
                id: Guid.Parse("77777777-7777-7777-7777-777777777772"),
                operationId: Guid.Parse("88888888-8888-8888-8888-888888888882"),
                cardId: DrawFeatureTestData.LowPriceLunchCardId,
                mealTypeAtDraw: "Lunch",
                succeededAtUtc: DrawFeatureTestData.KnownTimestamp().AddMinutes(2))
        };
    }

    private static IReadOnlyList<object> RecentLunchHistoryIncludingAllBentoCandidates()
    {
        return new[]
        {
            DrawFeatureTestData.DrawHistoryWithRotationSnapshot(
                id: Guid.Parse("77777777-7777-7777-7777-777777777773"),
                operationId: Guid.Parse("88888888-8888-8888-8888-888888888883"),
                cardId: DrawFeatureTestData.LowPriceLunchCardId,
                mealTypeAtDraw: "Lunch",
                succeededAtUtc: DrawFeatureTestData.KnownTimestamp().AddMinutes(1)),
            DrawFeatureTestData.DrawHistoryWithRotationSnapshot(
                id: Guid.Parse("77777777-7777-7777-7777-777777777774"),
                operationId: Guid.Parse("88888888-8888-8888-8888-888888888884"),
                cardId: DrawFeatureTestData.VegetarianLunchCardId,
                mealTypeAtDraw: "Lunch",
                succeededAtUtc: DrawFeatureTestData.KnownTimestamp().AddMinutes(2))
        };
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

    private sealed class ThrowingMealCardRandomizer : IMealCardRandomizer
    {
        public bool WasCalled { get; private set; }

        public int NextIndex(int count)
        {
            WasCalled = true;
            throw new InvalidOperationException("Randomizer must not be called when rotation empties the pool.");
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
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-rotation-draw-tests-").FullName);
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
