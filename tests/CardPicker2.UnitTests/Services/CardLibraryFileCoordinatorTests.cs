using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryFileCoordinatorTests
{
    [Fact]
    public async Task RunExclusiveAsync_SerializesConcurrentOperations()
    {
        var coordinator = new CardLibraryFileCoordinator();
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = false;

        var first = coordinator.RunExclusiveAsync(async cancellationToken =>
        {
            firstEntered.SetResult();
            await releaseFirst.Task.WaitAsync(cancellationToken);
            return 1;
        });

        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));

        var second = coordinator.RunExclusiveAsync(_ =>
        {
            secondEntered = true;
            return Task.FromResult(2);
        });

        await Task.Delay(100);
        Assert.False(secondEntered);

        releaseFirst.SetResult();

        Assert.Equal(1, await first);
        Assert.Equal(2, await second);
        Assert.True(secondEntered);
    }

    [Fact]
    public async Task CreateAsync_WhenWriteFails_DoesNotOverwriteOriginalFile()
    {
        using var library = TempCardLibrary.Create();
        var service = CreateService(library.FilePath);
        await service.LoadAsync();
        var original = await File.ReadAllTextAsync(library.FilePath);
        Directory.CreateDirectory(library.FilePath + ".tmp");

        var result = await service.CreateAsync(new MealCardInputModel
        {
            Name = "寫入失敗測試",
            MealType = MealType.Dinner,
            Description = "保留原始檔案"
        });

        Assert.Equal(CardLibraryMutationStatus.WriteFailed, result.Status);
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
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-coordinator-tests-").FullName);
        }

        public void Dispose()
        {
            var tempPath = FilePath + ".tmp";
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath);
            }

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
