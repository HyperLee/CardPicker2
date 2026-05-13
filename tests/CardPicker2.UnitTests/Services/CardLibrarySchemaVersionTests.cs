using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibrarySchemaVersionTests
{
    [Fact]
    public async Task LoadAsync_WhenLegacySchemaV1IsValid_MigratesInMemoryToSchemaV4()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, new
        {
            schemaVersion = 1,
            cards = new[]
            {
                new
                {
                    id = DrawFeatureTestData.BreakfastCardId,
                    name = " 早餐卡 ",
                    mealType = "Breakfast",
                    description = " 早餐描述 "
                }
            }
        });
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.Ready, result.Status);
        Assert.NotNull(result.Document);
        Assert.Equal(4, result.Document.SchemaVersion);
        Assert.Empty(result.Document.DrawHistory);
        var card = Assert.Single(result.Document.Cards);
        Assert.Equal(CardStatus.Active, card.Status);
        Assert.Null(card.DeletedAtUtc);
        Assert.Null(card.DecisionMetadata);
        Assert.Equal("早餐卡", card.GetContent(SupportedLanguage.ZhTw).Name);
    }

    [Fact]
    public async Task LoadAsync_WhenBilingualSchemaV2IsValid_MigratesInMemoryToSchemaV4()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, DrawFeatureTestData.SchemaV2Document());
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.Ready, result.Status);
        Assert.NotNull(result.Document);
        Assert.Equal(4, result.Document.SchemaVersion);
        Assert.Empty(result.Document.DrawHistory);
        Assert.All(result.Document.Cards, card => Assert.Equal(CardStatus.Active, card.Status));
        Assert.All(result.Document.Cards, card => Assert.Null(card.DecisionMetadata));
    }

    [Fact]
    public async Task LoadAsync_WhenSchemaV3IsValid_MigratesInMemoryToSchemaV4AndPreservesHistory()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, DrawFeatureTestData.SchemaV3Document(
            drawHistory: new[] { DrawFeatureTestData.DrawHistory() }));
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.Ready, result.Status);
        Assert.NotNull(result.Document);
        Assert.Equal(4, result.Document.SchemaVersion);
        Assert.Single(result.Document.DrawHistory);
        Assert.All(result.Document.Cards, card => Assert.Null(card.DecisionMetadata));
    }

    [Fact]
    public async Task LoadAsync_WhenSchemaV3HasDuplicateOperationIds_BlocksOperations()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, DrawFeatureTestData.SchemaV3Document(
            drawHistory: new[]
            {
                DrawFeatureTestData.DrawHistory(id: Guid.Parse("66666666-6666-6666-6666-666666666661")),
                DrawFeatureTestData.DrawHistory(id: Guid.Parse("66666666-6666-6666-6666-666666666662"))
            }));
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.True(result.IsBlocked);
    }

    [Fact]
    public async Task LoadAsync_WhenSchemaV3HistoryReferencesMissingCard_BlocksOperations()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, DrawFeatureTestData.SchemaV3Document(
            drawHistory: new[]
            {
                DrawFeatureTestData.DrawHistory(cardId: Guid.Parse("99999999-9999-9999-9999-999999999999"))
            }));
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.True(result.IsBlocked);
    }

    [Fact]
    public async Task LoadAsync_WhenSchemaV4HasInvalidMetadataEnum_BlocksOperationsAndPreservesOriginalFile()
    {
        using var library = TempCardLibrary.Create();
        var original = DrawFeatureTestData.Serialize(DrawFeatureTestData.SchemaV4Document(
            cards: new[]
            {
                DrawFeatureTestData.SchemaV4Card(
                    DrawFeatureTestData.BreakfastCardId,
                    "Breakfast",
                    "Active",
                    "早餐",
                    "Breakfast",
                    DrawFeatureTestData.DecisionMetadataWithInvalidEnum())
            }));
        await File.WriteAllTextAsync(library.FilePath, original);
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.Equal(original, await File.ReadAllTextAsync(library.FilePath));
    }

    [Fact]
    public async Task LoadAsync_WhenSchemaV4IsValid_NormalizesMetadata()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, DrawFeatureTestData.SchemaV4Document(
            cards: new[]
            {
                DrawFeatureTestData.SchemaV4Card(
                    DrawFeatureTestData.BreakfastCardId,
                    "Breakfast",
                    "Active",
                    "早餐",
                    "Breakfast",
                    DrawFeatureTestData.DecisionMetadataWithDuplicateAndBlankTags())
            }));
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        var card = Assert.Single(result.Document!.Cards);
        Assert.NotNull(card.DecisionMetadata);
        Assert.Equal(new[] { "便當", "Bento" }, card.DecisionMetadata.Tags);
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsMissing_CreatesSchemaV4SeedWithEmptyHistoryAndMetadata()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.CreatedFromSeed, result.Status);
        Assert.NotNull(result.Document);
        Assert.Equal(4, result.Document.SchemaVersion);
        Assert.Empty(result.Document.DrawHistory);
        Assert.All(Enum.GetValues<MealType>(), mealType =>
        {
            Assert.True(result.Document.Cards.Count(card => card.MealType == mealType && card.Status == CardStatus.Active) >= 3);
        });
        Assert.Contains(result.Document.Cards, card => card.DecisionMetadata is not null);
    }

    [Fact]
    public async Task LoadAsync_WhenUnsupportedSchemaIsFound_PreservesOriginalFile()
    {
        using var library = TempCardLibrary.Create();
        const string original = """
            {"schemaVersion":999,"cards":[]}
            """;
        await File.WriteAllTextAsync(library.FilePath, original);
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.Equal(original, await File.ReadAllTextAsync(library.FilePath));
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsCorrupted_PreservesOriginalFile()
    {
        using var library = TempCardLibrary.Create();
        const string original = "{";
        await File.WriteAllTextAsync(library.FilePath, original);
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.Equal(original, await File.ReadAllTextAsync(library.FilePath));
    }

    private static CardLibraryService CreateService(string filePath)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            new MealCardRandomizer(),
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance,
            new MealCardLocalizationService(),
            new CardLibraryFileCoordinator());
    }

    private static Task WriteJsonAsync(string filePath, object document)
    {
        return File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(document, DrawFeatureTestData.JsonOptions));
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
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-schema-tests-").FullName);
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
