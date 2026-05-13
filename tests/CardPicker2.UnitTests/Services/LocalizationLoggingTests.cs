using CardPicker2.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class LocalizationLoggingTests
{
    [Fact]
    public async Task LoadAsync_WithCorruptedJson_ReturnsSafeDiagnosticWithoutRawJson()
    {
        using var library = TempCardLibrary.Create();
        await File.WriteAllTextAsync(library.FilePath, """{"secret":"do-not-log" """);
        var service = CreateService(library.FilePath);

        var result = await service.LoadAsync();

        Assert.True(result.IsBlocked);
        Assert.DoesNotContain("do-not-log", result.DiagnosticMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LanguagePreferenceService_WithUnsupportedCookie_ReturnsSafeFallbackState()
    {
        var service = new LanguagePreferenceService();

        var preference = service.ResolveCookieValue("c=fr-FR|uic=fr-FR");

        Assert.Equal("zh-TW", preference.CultureName.CultureName);
        Assert.True(preference.IsFallback);
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
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-logging-tests-").FullName);
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
