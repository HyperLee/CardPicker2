using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryPreferenceSearchTests
{
    [Fact]
    public async Task SearchAsync_AppliesKeywordMealMetadataAndPreferenceIntersection()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV5Document());
        var service = CreateService(library.FilePath);

        var results = await service.SearchAsync(new SearchCriteria
        {
            Keyword = "牛肉",
            MealType = MealType.Lunch,
            CurrentLanguage = SupportedLanguage.ZhTw,
            Filters = new CardFilterCriteria
            {
                Tags = new[] { "麵食" },
                CurrentLanguage = SupportedLanguage.ZhTw
            },
            Preferences = new CardPreferenceCriteria
            {
                FavoriteFilter = FavoriteFilter.All,
                DrawEligibilityFilter = DrawEligibilityFilter.DrawableOnly
            }
        });

        Assert.DoesNotContain(results, card => card.Preferences.IsExcludedFromDraw);
        Assert.Empty(results);
    }

    private static CardLibraryService CreateService(string filePath)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            new MealCardRandomizer(),
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance);
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
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-preference-search-tests-").FullName);
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
