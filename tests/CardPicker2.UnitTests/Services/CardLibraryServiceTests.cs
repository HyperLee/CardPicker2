using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryServiceTests
{
    [Fact]
    public async Task LoadAsync_WhenJsonIsMissing_CreatesSeedDocumentWithAtLeastThreeCardsPerMealType()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.CreatedFromSeed, result.Status);
        Assert.False(result.IsBlocked);
        Assert.True(File.Exists(library.FilePath));
        Assert.NotNull(result.Document);
        Assert.All(Enum.GetValues<MealType>(), mealType =>
        {
            Assert.True(result.Document.Cards.Count(card => card.MealType == mealType) >= 3);
        });
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsCorrupted_PreservesOriginalFileAndBlocksOperations()
    {
        using var library = TempCardLibrary.Create();
        await File.WriteAllTextAsync(library.FilePath, "{");
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.True(result.IsBlocked);
        Assert.Equal("{", await File.ReadAllTextAsync(library.FilePath));
    }

    [Fact]
    public async Task LoadAsync_WhenSchemaVersionIsUnsupported_BlocksOperations()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, new
        {
            schemaVersion = 999,
            cards = Array.Empty<object>()
        });
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.True(result.IsBlocked);
    }

    [Fact]
    public async Task LoadAsync_WhenRequiredCardFieldIsMissing_BlocksOperations()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, new
        {
            schemaVersion = 1,
            cards = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    mealType = "Breakfast",
                    description = "附近早餐店的鮪魚蛋餅。"
                }
            }
        });
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.True(result.IsBlocked);
    }

    [Fact]
    public async Task LoadAsync_WhenMealTypeIsInvalid_BlocksOperations()
    {
        using var library = TempCardLibrary.Create();
        await File.WriteAllTextAsync(library.FilePath, """
            {
              "schemaVersion": 1,
              "cards": [
                {
                  "id": "018f4c92-7a7d-4b7e-b34a-88f4f3a82d91",
                  "name": "宵夜粥",
                  "mealType": "LateNight",
                  "description": "不支援的餐別。"
                }
              ]
            }
            """);
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.True(result.IsBlocked);
    }

    [Fact]
    public async Task LoadAsync_WhenPersistedCardsContainDuplicateKey_BlocksOperations()
    {
        using var library = TempCardLibrary.Create();
        await WriteJsonAsync(library.FilePath, new
        {
            schemaVersion = 1,
            cards = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    name = " 牛肉麵 ",
                    mealType = "Lunch",
                    description = " 清燉湯頭 "
                },
                new
                {
                    id = Guid.NewGuid(),
                    name = "牛肉麵",
                    mealType = "Lunch",
                    description = "清燉湯頭"
                }
            }
        });
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedCorruptFile, result.Status);
        Assert.True(result.IsBlocked);
    }

    [Fact]
    public async Task LoadAsync_WhenSeedWriteFails_DoesNotCreatePartialTargetFile()
    {
        using var library = TempCardLibrary.Create();
        Directory.CreateDirectory(library.FilePath + ".tmp");
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.Equal(CardLibraryLoadStatus.BlockedUnreadableFile, result.Status);
        Assert.True(result.IsBlocked);
        Assert.False(File.Exists(library.FilePath));
    }

    [Fact]
    public async Task DrawAsync_SelectsOnlyFromRequestedMealType()
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
                },
                new
                {
                    id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    name = "午餐卡",
                    mealType = "Lunch",
                    description = "午餐描述"
                }
            }
        });
        var service = CreateService(library.FilePath, new FixedMealCardRandomizer(0));

        var result = await service.DrawAsync(MealType.Lunch);

        Assert.True(result.Succeeded);
        Assert.Equal(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), result.CardId);
        Assert.Equal(MealType.Lunch, result.MealType);
        Assert.Equal("午餐卡", result.Name);
    }

    [Fact]
    public async Task DrawAsync_WithEmptyMealPool_ReturnsFailure()
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

        var result = await service.DrawAsync(MealType.Dinner);

        Assert.False(result.Succeeded);
        Assert.Contains("沒有可抽取", result.UserMessage);
    }

    [Fact]
    public async Task DrawAsync_WhenLibraryIsBlocked_ReturnsRecoveryFailure()
    {
        using var library = TempCardLibrary.Create();
        await File.WriteAllTextAsync(library.FilePath, "{");
        var service = CreateService(library.FilePath);

        var result = await service.DrawAsync(MealType.Breakfast);

        Assert.False(result.Succeeded);
        Assert.Contains("卡牌庫", result.UserMessage);
    }

    private static CardLibraryService CreateService(string filePath, IMealCardRandomizer? randomizer = null)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            randomizer ?? new FixedMealCardRandomizer(0),
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
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-tests-").FullName);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
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
}