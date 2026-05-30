using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryPreferenceDrawTests
{
    [Fact]
    public async Task DrawAsync_DoesNotSelectExcludedCard()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV5Document());
        var service = CreateService(library.FilePath, new FixedMealCardRandomizer(0));

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            RequestedLanguage = SupportedLanguage.ZhTw
        });

        Assert.True(result.Succeeded);
        Assert.NotEqual(DrawFeatureTestData.LunchCardId, result.CardId);
    }

    [Fact]
    public async Task DrawAsync_WhenMetadataMatchesOnlyExcludedCards_ReturnsPreferenceEmptyReason()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV5Document());
        var service = CreateService(library.FilePath, new ThrowingMealCardRandomizer());

        var result = await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = MealType.Lunch,
            CoinInserted = true,
            RequestedLanguage = SupportedLanguage.ZhTw,
            Filters = new CardFilterCriteria
            {
                Tags = new[] { "麵食" },
                CurrentLanguage = SupportedLanguage.ZhTw
            }
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Preference.EmptyPool", result.StatusKey);
        Assert.Equal(CandidatePoolEmptyReason.PreferenceExcludedCandidatePoolEmpty, result.CandidatePoolEmptyReason);
    }

    private static CardLibraryService CreateService(string filePath, IMealCardRandomizer randomizer)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            randomizer,
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance);
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

    private sealed class ThrowingMealCardRandomizer : IMealCardRandomizer
    {
        public int NextIndex(int count)
        {
            throw new InvalidOperationException("Randomizer must not be called for an empty preference-filtered pool.");
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

        public static async Task<TempCardLibrary> CreateWithDocumentAsync(object document)
        {
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-preference-draw-tests-").FullName);
            await File.WriteAllTextAsync(library.FilePath, DrawFeatureTestData.Serialize(document));
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
