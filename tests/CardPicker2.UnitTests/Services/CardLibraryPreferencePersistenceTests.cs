using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryPreferencePersistenceTests
{
    [Fact]
    public async Task LoadAsync_WithSchemaV4MissingPreferences_MigratesToSchemaV5DefaultsInMemory()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV4Document());
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.False(result.IsBlocked);
        Assert.Equal(CardLibraryDocument.CurrentSchemaVersion, result.Document!.SchemaVersion);
        Assert.All(result.Document.Cards, card =>
        {
            Assert.False(card.Preferences.IsFavorite);
            Assert.False(card.Preferences.IsExcludedFromDraw);
        });
    }

    [Fact]
    public async Task LoadAsync_WithSchemaV5Preferences_RoundTripsPreferenceState()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV5Document());
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.False(result.IsBlocked);
        Assert.Contains(result.Document!.Cards, card => card.Preferences.IsFavorite);
        Assert.Contains(result.Document.Cards, card => card.Preferences.IsExcludedFromDraw);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidPreferenceJson_BlocksAndPreservesOriginalFile()
    {
        var invalidCard = DrawFeatureTestData.SchemaV5Card(
            DrawFeatureTestData.LunchCardId,
            "Lunch",
            "Active",
            "錯誤偏好卡",
            "Invalid Preference Card",
            DrawFeatureTestData.CompleteDecisionMetadata(),
            isFavorite: false,
            isExcludedFromDraw: false);
        invalidCard = DrawFeatureTestData.WithPreferences(
            invalidCard,
            isFavorite: false,
            isExcludedFromDraw: false);
        var json = DrawFeatureTestData.Serialize(new
        {
            schemaVersion = 5,
            cards = new[]
            {
                new Dictionary<string, object?>((IDictionary<string, object?>)invalidCard, StringComparer.OrdinalIgnoreCase)
                {
                    ["preferences"] = DrawFeatureTestData.InvalidPreferenceState()
                }
            },
            drawHistory = Array.Empty<object>()
        });
        using var library = await TempCardLibrary.CreateWithJsonAsync(json);
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.True(result.IsBlocked);
        Assert.Equal(json, await File.ReadAllTextAsync(library.FilePath));
    }

    [Fact]
    public async Task CreateAsync_WritesSchemaV5WithDefaultPreferences()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV4Document());
        var service = CreateService(library.FilePath);

        var result = await service.CreateAsync(new MealCardInputModel
        {
            NameZhTw = "新午餐",
            DescriptionZhTw = "新午餐描述",
            NameEnUs = "New Lunch",
            DescriptionEnUs = "New lunch description",
            MealType = MealType.Lunch
        });

        Assert.True(result.Succeeded);
        var persisted = await ReadDocumentAsync(library.FilePath);
        Assert.Equal(CardLibraryDocument.CurrentSchemaVersion, persisted.SchemaVersion);
        var created = Assert.Single(persisted.Cards, card => card.Id == result.Card!.Id);
        Assert.False(created.Preferences.IsFavorite);
        Assert.False(created.Preferences.IsExcludedFromDraw);
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

        public static async Task<TempCardLibrary> CreateWithDocumentAsync(object document)
        {
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-preference-persistence-tests-").FullName);
            await File.WriteAllTextAsync(library.FilePath, DrawFeatureTestData.Serialize(document));
            return library;
        }

        public static async Task<TempCardLibrary> CreateWithJsonAsync(string json)
        {
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-preference-persistence-tests-").FullName);
            await File.WriteAllTextAsync(library.FilePath, json);
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
