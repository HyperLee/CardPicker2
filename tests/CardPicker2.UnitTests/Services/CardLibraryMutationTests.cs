using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryMutationTests
{
    [Fact]
    public async Task CreateAsync_WithValidInput_AddsTrimmedCard()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);

        var result = await service.CreateAsync(new MealCardInputModel
        {
            Name = "  新餐點  ",
            MealType = MealType.Lunch,
            Description = "  新描述  "
        });

        Assert.True(result.Succeeded);
        Assert.Equal("新餐點", result.Card?.Name);
        Assert.Contains(await service.SearchAsync(new SearchCriteria { Keyword = "新餐點" }), card => card.Id == result.Card?.Id);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateInput_ReturnsDuplicate()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);
        await service.LoadAsync();

        var result = await service.CreateAsync(new MealCardInputModel
        {
            Name = " 鮪魚蛋餅 ",
            MealType = MealType.Breakfast,
            Description = "附近早餐店的鮪魚蛋餅，加一杯無糖豆漿。"
        });

        Assert.Equal(CardLibraryMutationStatus.Duplicate, result.Status);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task UpdateAsync_WithValidInput_ChangesContentButKeepsId()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);
        var originalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await service.LoadAsync();

        var result = await service.UpdateAsync(originalId, new MealCardInputModel
        {
            Name = "更新蛋餅",
            MealType = MealType.Breakfast,
            Description = "更新描述"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(originalId, result.Card?.Id);
        Assert.Equal("更新蛋餅", (await service.FindByIdAsync(originalId))?.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenInputDuplicatesAnotherCard_DoesNotChangeOriginal()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);
        var targetId = Guid.Parse("11111111-1111-1111-1111-111111111112");
        await service.LoadAsync();

        var result = await service.UpdateAsync(targetId, new MealCardInputModel
        {
            Name = "鮪魚蛋餅",
            MealType = MealType.Breakfast,
            Description = "附近早餐店的鮪魚蛋餅，加一杯無糖豆漿。"
        });

        Assert.Equal(CardLibraryMutationStatus.Duplicate, result.Status);
        Assert.Equal("鹹粥小菜", (await service.FindByIdAsync(targetId))?.Name);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingCard_RemovesItFromSearchAndDetails()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);
        var targetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await service.LoadAsync();

        var result = await service.DeleteAsync(targetId);

        Assert.True(result.Succeeded);
        Assert.Null(await service.FindByIdAsync(targetId));
        Assert.DoesNotContain(await service.SearchAsync(new SearchCriteria()), card => card.Id == targetId);
    }

    [Fact]
    public async Task CreateAsync_WhenAtomicWriteFails_DoesNotPolluteExistingFile()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);
        await service.LoadAsync();
        var before = await File.ReadAllTextAsync(library.FilePath);
        Directory.CreateDirectory(library.FilePath + ".tmp");

        var result = await service.CreateAsync(new MealCardInputModel
        {
            Name = "寫入失敗測試",
            MealType = MealType.Dinner,
            Description = "保留原始檔案"
        });

        Assert.Equal(CardLibraryMutationStatus.WriteFailed, result.Status);
        Assert.Equal(before, await File.ReadAllTextAsync(library.FilePath));
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

        public static TempCardLibrary Create()
        {
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-mutation-tests-").FullName);
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