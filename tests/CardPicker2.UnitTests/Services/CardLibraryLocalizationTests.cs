using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryLocalizationTests
{
    [Fact]
    public async Task LoadAsync_WhenSchemaV1Exists_LoadsTraditionalChineseContentWithMissingEnglish()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, new
        {
            schemaVersion = 1,
            cards = new[]
            {
                new
                {
                    id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    name = "早餐卡",
                    mealType = "Breakfast",
                    description = "早餐描述"
                }
            }
        });
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.False(result.IsBlocked);
        var card = Assert.Single(result.Document!.Cards);
        Assert.Equal("早餐卡", card.GetContent(SupportedLanguage.ZhTw).Name);
        Assert.False(card.HasCompleteContent(SupportedLanguage.EnUs));
        Assert.Equal(MealCardTranslationStatus.MissingEnglish, card.TranslationStatus);
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsMissing_CreatesSchemaV3SeedWithCompleteBilingualContent()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(3, result.Document!.SchemaVersion);
        Assert.Empty(result.Document.DrawHistory);
        Assert.All(result.Document.Cards, card =>
        {
            Assert.True(card.HasCompleteContent(SupportedLanguage.ZhTw));
            Assert.True(card.HasCompleteContent(SupportedLanguage.EnUs));
        });
    }

    [Fact]
    public async Task LoadAsync_WhenSchemaV2Exists_LoadsLocalizedContent()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, new
        {
            schemaVersion = 2,
            cards = new[]
            {
                new
                {
                    id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    mealType = "Lunch",
                    localizations = new Dictionary<string, object>
                    {
                        ["zh-TW"] = new { name = "午餐卡", description = "午餐描述" },
                        ["en-US"] = new { name = "Lunch Card", description = "Lunch description" }
                    }
                }
            }
        });
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.False(result.IsBlocked);
        var card = Assert.Single(result.Document!.Cards);
        Assert.Equal("Lunch Card", card.GetContent(SupportedLanguage.EnUs).Name);
        Assert.Equal(MealCardTranslationStatus.Complete, card.TranslationStatus);
    }

    [Fact]
    public async Task LoadAsync_WhenSchemaV2IsCorrupted_PreservesOriginalFileAndBlocksOperations()
    {
        using var library = TempCardLibrary.Create();
        const string original = """{"schemaVersion":2,"cards":[{"id":""}]}""";
        await File.WriteAllTextAsync(library.FilePath, original);
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.True(result.IsBlocked);
        Assert.Equal(original, await File.ReadAllTextAsync(library.FilePath));
    }

    private static CardLibraryService CreateService(string filePath)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            new MealCardRandomizer(),
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance);
    }

    private static Task WriteJsonAsync(string filePath, object document)
    {
        return File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(document));
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
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-localization-tests-").FullName);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
