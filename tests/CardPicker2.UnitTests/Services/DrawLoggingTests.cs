using System.Text.Json;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class DrawLoggingTests
{
    [Fact]
    public async Task DrawAsync_LogsSuccessReplayAndValidationFailureWithoutRawPayloads()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document());
        var logger = new CapturingLogger<CardLibraryService>();
        var service = CreateService(library.FilePath, logger);
        var operation = CreateOperation(DrawMode.Random, null);

        await service.DrawAsync(operation);
        await service.DrawAsync(operation);
        await service.DrawAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = operation.Mode,
            MealType = operation.MealType,
            CoinInserted = false,
            RequestedLanguage = operation.RequestedLanguage
        });

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("succeeded with mode", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("replayed card", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("coin confirmation is missing", StringComparison.Ordinal));
        AssertNoSensitivePayloads(logger);
    }

    [Fact]
    public async Task DrawAsync_LogsEmptyPoolBlockedLibraryAndWriteFailureWithoutRawPayloads()
    {
        using var emptyPoolLibrary = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document(
            cards: new[]
            {
                DrawFeatureTestData.SchemaV3Card(
                    DrawFeatureTestData.BreakfastCardId,
                    "Breakfast",
                    "Active",
                    "早餐",
                    "Breakfast")
            }));
        var emptyPoolLogger = new CapturingLogger<CardLibraryService>();
        await CreateService(emptyPoolLibrary.FilePath, emptyPoolLogger)
            .DrawAsync(CreateOperation(DrawMode.Normal, MealType.Dinner));

        using var blockedLibrary = TempCardLibrary.Create();
        await File.WriteAllTextAsync(blockedLibrary.FilePath, """{"secret":"do-not-log" """);
        var blockedLogger = new CapturingLogger<CardLibraryService>();
        await CreateService(blockedLibrary.FilePath, blockedLogger)
            .DrawAsync(CreateOperation(DrawMode.Random, null));

        using var writeFailureLibrary = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document());
        Directory.CreateDirectory(writeFailureLibrary.FilePath + ".tmp");
        var writeFailureLogger = new CapturingLogger<CardLibraryService>();
        await CreateService(writeFailureLibrary.FilePath, writeFailureLogger)
            .DrawAsync(CreateOperation(DrawMode.Random, null));

        Assert.Contains(emptyPoolLogger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("candidate pool is empty", StringComparison.Ordinal));
        Assert.Contains(blockedLogger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("invalid JSON", StringComparison.Ordinal));
        Assert.Contains(blockedLogger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("card library is unavailable", StringComparison.Ordinal));
        Assert.Contains(writeFailureLogger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("failed while writing selected card", StringComparison.Ordinal));
        AssertNoSensitivePayloads(emptyPoolLogger);
        AssertNoSensitivePayloads(blockedLogger);
        AssertNoSensitivePayloads(writeFailureLogger);
    }

    [Fact]
    public async Task GetDrawStatisticsAsync_LogsProjectionAndBlockedStateWithoutRawPayloads()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document(
            drawHistory: new[]
            {
                DrawFeatureTestData.DrawHistory()
            }));
        var logger = new CapturingLogger<CardLibraryService>();

        await CreateService(library.FilePath, logger).GetDrawStatisticsAsync(SupportedLanguage.ZhTw);

        using var blockedLibrary = TempCardLibrary.Create();
        await File.WriteAllTextAsync(blockedLibrary.FilePath, """{"secret":"do-not-log" """);
        var blockedLogger = new CapturingLogger<CardLibraryService>();

        await CreateService(blockedLibrary.FilePath, blockedLogger).GetDrawStatisticsAsync(SupportedLanguage.ZhTw);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("Draw statistics projected", StringComparison.Ordinal));
        Assert.Contains(blockedLogger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("Draw statistics blocked", StringComparison.Ordinal));
        AssertNoSensitivePayloads(logger);
        AssertNoSensitivePayloads(blockedLogger);
    }

    [Fact]
    public async Task DeleteAsync_WithHistory_LogsRetainedDeletedCardWithoutRawPayloads()
    {
        using var library = await TempCardLibrary.CreateWithDocumentAsync(DrawFeatureTestData.SchemaV3Document(
            drawHistory: new[]
            {
                DrawFeatureTestData.DrawHistory(cardId: DrawFeatureTestData.BreakfastCardId)
            }));
        var logger = new CapturingLogger<CardLibraryService>();

        await CreateService(library.FilePath, logger).DeleteAsync(DrawFeatureTestData.BreakfastCardId);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("retained as deleted", StringComparison.Ordinal));
        AssertNoSensitivePayloads(logger);
    }

    private static DrawOperation CreateOperation(DrawMode mode, MealType? mealType)
    {
        return new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = mode,
            MealType = mealType,
            CoinInserted = true,
            RequestedLanguage = SupportedLanguage.ZhTw
        };
    }

    private static CardLibraryService CreateService(
        string filePath,
        CapturingLogger<CardLibraryService> logger,
        IMealCardRandomizer? randomizer = null)
    {
        var localizationService = new MealCardLocalizationService();
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            randomizer ?? new FixedMealCardRandomizer(0),
            new DuplicateCardDetector(),
            logger,
            localizationService,
            new CardLibraryFileCoordinator(),
            new DrawCandidatePoolBuilder(),
            new DrawStatisticsService(localizationService));
    }

    private static void AssertNoSensitivePayloads(CapturingLogger<CardLibraryService> logger)
    {
        var text = logger.AllText;
        Assert.DoesNotContain("do-not-log", text, StringComparison.Ordinal);
        Assert.DoesNotContain("schemaVersion", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("localizations", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("system prompt", text, StringComparison.OrdinalIgnoreCase);
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

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public string AllText => string.Join(Environment.NewLine, Entries.Select(entry => $"{entry.Message} {entry.ExceptionMessage}"));

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception?.Message ?? string.Empty));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, string ExceptionMessage);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
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
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-draw-logging-tests-").FullName);
        }

        public static async Task<TempCardLibrary> CreateWithDocumentAsync(object document)
        {
            var library = Create();
            await File.WriteAllTextAsync(library.FilePath, JsonSerializer.Serialize(document, DrawFeatureTestData.JsonOptions));
            return library;
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
