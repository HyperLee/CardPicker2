using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardDeletionRetentionTests
{
    [Fact]
    public async Task DeleteAsync_WithoutHistory_HardDeletesCard()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document());
        var service = CreateService(library.FilePath);

        var result = await service.DeleteAsync(DrawFeatureTestData.SecondBreakfastCardId);

        Assert.True(result.Succeeded);
        var document = await ReadDocumentAsync(library.FilePath);
        Assert.DoesNotContain(document.Cards, card => card.Id == DrawFeatureTestData.SecondBreakfastCardId);
    }

    [Fact]
    public async Task DeleteAsync_WithHistory_RetainsDeletedCardAndExcludesItFromActiveSurfaces()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document(
            drawHistory: new[]
            {
                DrawFeatureTestData.DrawHistory(cardId: DrawFeatureTestData.BreakfastCardId)
            }));
        var service = CreateService(library.FilePath);

        var result = await service.DeleteAsync(DrawFeatureTestData.BreakfastCardId);

        Assert.True(result.Succeeded);
        var document = await ReadDocumentAsync(library.FilePath);
        var retained = Assert.Single(document.Cards, card => card.Id == DrawFeatureTestData.BreakfastCardId);
        Assert.Equal(CardStatus.Deleted, retained.Status);
        Assert.NotNull(retained.DeletedAtUtc);
        Assert.DoesNotContain(await service.SearchAsync(new SearchCriteria()), card => card.Id == retained.Id);
        Assert.Null(await service.FindByIdAsync(retained.Id));

        var update = await service.UpdateAsync(retained.Id, new MealCardInputModel
        {
            Name = "重新啟用",
            MealType = MealType.Breakfast,
            Description = "不應重新啟用"
        });
        Assert.Equal(CardLibraryMutationStatus.NotFound, update.Status);
    }

    [Fact]
    public void DrawCandidatePoolBuilder_AlwaysExcludesDeletedCards()
    {
        var builder = new DrawCandidatePoolBuilder();
        var document = Deserialize(DrawFeatureTestData.SchemaV3Document());

        var pool = builder.Build(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Random,
            CoinInserted = true
        }, document.Cards);

        Assert.DoesNotContain(pool.Cards, card => card.Status == CardStatus.Deleted);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateMatchesOnlyDeletedCard_Succeeds()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document(
            cards: new[]
            {
                DrawFeatureTestData.SchemaV3Card(
                    DrawFeatureTestData.BreakfastCardId,
                    "Breakfast",
                    "Deleted",
                    "鮪魚蛋餅",
                    "Tuna Egg Crepe",
                    DrawFeatureTestData.KnownTimestamp())
            },
            drawHistory: new[]
            {
                DrawFeatureTestData.DrawHistory(cardId: DrawFeatureTestData.BreakfastCardId)
            }));
        var service = CreateService(library.FilePath);

        var result = await service.CreateAsync(new MealCardInputModel
        {
            NameZhTw = "鮪魚蛋餅",
            DescriptionZhTw = "鮪魚蛋餅 描述",
            NameEnUs = "Tuna Egg Crepe",
            DescriptionEnUs = "Tuna Egg Crepe description",
            MealType = MealType.Breakfast
        });

        Assert.True(result.Succeeded);
    }

    private static CardLibraryService CreateService(string filePath)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            new MealCardRandomizer(),
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance,
            new MealCardLocalizationService(),
            new CardLibraryFileCoordinator(),
            new DrawCandidatePoolBuilder(),
            new DrawStatisticsService(new MealCardLocalizationService()));
    }

    private static CardLibraryDocument Deserialize(object document)
    {
        return JsonSerializer.Deserialize<CardLibraryDocument>(
            JsonSerializer.Serialize(document, DrawFeatureTestData.JsonOptions),
            DrawFeatureTestData.JsonOptions)!;
    }

    private static async Task<CardLibraryDocument> ReadDocumentAsync(string filePath)
    {
        return JsonSerializer.Deserialize<CardLibraryDocument>(
            await File.ReadAllTextAsync(filePath),
            DrawFeatureTestData.JsonOptions)!;
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
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-delete-retention-tests-").FullName);
            await File.WriteAllTextAsync(library.FilePath, JsonSerializer.Serialize(document, DrawFeatureTestData.JsonOptions));
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
