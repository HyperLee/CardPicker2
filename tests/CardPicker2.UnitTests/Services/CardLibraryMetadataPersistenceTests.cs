using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryMetadataPersistenceTests
{
    [Fact]
    public async Task CreateAsync_WithMetadata_PersistsSchemaV4MetadataAcrossReload()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);

        var result = await service.CreateAsync(new MealCardInputModel
        {
            NameZhTw = "測試便當",
            DescriptionZhTw = "測試描述",
            NameEnUs = "Test Bento",
            DescriptionEnUs = "Test description",
            MealType = MealType.Lunch,
            TagsInput = "便當, 外帶",
            PriceRange = PriceRange.Low,
            PreparationTimeRange = PreparationTimeRange.Quick,
            DietaryPreferences = new List<DietaryPreference> { DietaryPreference.TakeoutFriendly },
            SpiceLevel = SpiceLevel.None
        });

        Assert.True(result.Succeeded);
        var reloaded = await CreateService(library.FilePath).FindByIdAsync(result.Card!.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(CardLibraryDocument.CurrentSchemaVersion, (await ReadDocumentAsync(library.FilePath)).SchemaVersion);
        Assert.Equal(new[] { "便當", "外帶" }, reloaded.DecisionMetadata?.Tags);
        Assert.Equal(PriceRange.Low, reloaded.DecisionMetadata?.PriceRange);
    }

    [Fact]
    public async Task UpdateAsync_WithMetadata_PreservesIdentityHistoryAndStatus()
    {
        using var library = await TempCardLibrary.CreateWithSchemaV4Async();
        var service = CreateService(library.FilePath);

        var result = await service.UpdateAsync(DrawFeatureTestData.BreakfastCardId, new MealCardInputModel
        {
            NameZhTw = "更新蛋餅",
            DescriptionZhTw = "更新描述",
            NameEnUs = "Updated Crepe",
            DescriptionEnUs = "Updated description",
            MealType = MealType.Breakfast,
            TagsInput = "早餐, 快速",
            PriceRange = PriceRange.Medium,
            PreparationTimeRange = PreparationTimeRange.Quick,
            DietaryPreferences = new List<DietaryPreference> { DietaryPreference.TakeoutFriendly },
            SpiceLevel = SpiceLevel.None
        });

        Assert.True(result.Succeeded);
        var document = await ReadDocumentAsync(library.FilePath);
        var card = Assert.Single(document.Cards, card => card.Id == DrawFeatureTestData.BreakfastCardId);
        Assert.Equal(CardStatus.Active, card.Status);
        Assert.Equal(DrawFeatureTestData.HistoryRecordId, Assert.Single(document.DrawHistory).Id);
        Assert.Equal(new[] { "早餐", "快速" }, card.DecisionMetadata?.Tags);
    }

    [Fact]
    public async Task SchemaV3CardsWithoutMetadata_RemainSearchableUntilMetadataFiltersAreApplied()
    {
        using var library = await TempCardLibrary.CreateWithSchemaV3Async();
        var service = CreateService(library.FilePath);

        var unfiltered = await service.SearchAsync(new SearchCriteria { MealType = MealType.Dinner });
        var filtered = await service.SearchAsync(new SearchCriteria
        {
            MealType = MealType.Dinner,
            Filters = new CardFilterCriteria { PriceRange = PriceRange.Low }
        });

        Assert.Contains(unfiltered, card => card.Id == DrawFeatureTestData.DinnerCardId);
        Assert.DoesNotContain(filtered, card => card.Id == DrawFeatureTestData.DinnerCardId);
    }

    private static CardLibraryService CreateService(string filePath)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            new MealCardRandomizer(),
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance);
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

        public string DirectoryPath { get; }

        public string FilePath { get; }

        public static TempCardLibrary Create()
        {
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-metadata-persistence-tests-").FullName);
        }

        public static async Task<TempCardLibrary> CreateWithSchemaV4Async()
        {
            var library = Create();
            await File.WriteAllTextAsync(
                library.FilePath,
                DrawFeatureTestData.Serialize(DrawFeatureTestData.SchemaV4Document(
                    drawHistory: new[] { DrawFeatureTestData.DrawHistory(cardId: DrawFeatureTestData.BreakfastCardId) })));
            return library;
        }

        public static async Task<TempCardLibrary> CreateWithSchemaV3Async()
        {
            var library = Create();
            await File.WriteAllTextAsync(library.FilePath, DrawFeatureTestData.Serialize(DrawFeatureTestData.SchemaV3Document()));
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
