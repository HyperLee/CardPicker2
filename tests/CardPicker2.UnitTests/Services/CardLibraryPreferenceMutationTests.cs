using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibraryPreferenceMutationTests
{
    [Fact]
    public async Task SetPreferenceAsync_UpdatesExcludedTargetStateAndIsIdempotent()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV5Document());
        var service = CreateService(library.FilePath);
        var input = new CardPreferenceUpdateInputModel
        {
            CardId = DrawFeatureTestData.LowPriceLunchCardId,
            TargetIsExcludedFromDraw = true
        };

        var first = await service.SetPreferenceAsync(input);
        var second = await service.SetPreferenceAsync(input);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.True((await service.FindByIdAsync(DrawFeatureTestData.LowPriceLunchCardId))!.Preferences.IsExcludedFromDraw);
    }

    [Fact]
    public async Task SetPreferenceAsync_RejectsMissingDeletedBlockedAndWriteFailure()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV5Document());
        var service = CreateService(library.FilePath);

        var missing = await service.SetPreferenceAsync(new CardPreferenceUpdateInputModel
        {
            CardId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            TargetIsExcludedFromDraw = true
        });
        var deleted = await service.SetPreferenceAsync(new CardPreferenceUpdateInputModel
        {
            CardId = DrawFeatureTestData.DeletedCardId,
            TargetIsExcludedFromDraw = true
        });

        using var blockedLibrary = await TempCardLibrary.CreateWithJsonAsync("{");
        var blocked = await CreateService(blockedLibrary.FilePath).SetPreferenceAsync(new CardPreferenceUpdateInputModel
        {
            CardId = DrawFeatureTestData.LunchCardId,
            TargetIsExcludedFromDraw = true
        });

        using var writeFailureLibrary = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV5Document());
        Directory.CreateDirectory(writeFailureLibrary.FilePath + ".tmp");
        var writeFailure = await CreateService(writeFailureLibrary.FilePath).SetPreferenceAsync(new CardPreferenceUpdateInputModel
        {
            CardId = DrawFeatureTestData.LowPriceLunchCardId,
            TargetIsExcludedFromDraw = true
        });

        Assert.Equal(PreferenceMutationStatus.NotFound, missing.Status);
        Assert.Equal(PreferenceMutationStatus.Deleted, deleted.Status);
        Assert.Equal(PreferenceMutationStatus.Blocked, blocked.Status);
        Assert.Equal(PreferenceMutationStatus.WriteFailed, writeFailure.Status);
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
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-preference-mutation-tests-").FullName);
            await File.WriteAllTextAsync(library.FilePath, DrawFeatureTestData.Serialize(document));
            return library;
        }

        public static async Task<TempCardLibrary> CreateWithJsonAsync(string json)
        {
            var library = new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-preference-mutation-tests-").FullName);
            await File.WriteAllTextAsync(library.FilePath, json);
            return library;
        }

        public void Dispose()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }

            if (Directory.Exists(FilePath + ".tmp"))
            {
                Directory.Delete(FilePath + ".tmp");
            }

            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath);
            }
        }
    }
}
