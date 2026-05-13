using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.Models;

using Microsoft.Extensions.Options;

namespace CardPicker2.Services;

/// <summary>
/// Loads, validates, and persists the local JSON card library.
/// </summary>
/// <example>
/// <code>
/// var loadResult = await service.LoadAsync(cancellationToken);
/// </code>
/// </example>
public sealed class CardLibraryService : ICardLibraryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CardLibraryOptions _options;
    private readonly IMealCardRandomizer _randomizer;
    private readonly DuplicateCardDetector _duplicateDetector;
    private readonly ILogger<CardLibraryService> _logger;
    private readonly MealCardLocalizationService _localizationService;
    private readonly CardLibraryFileCoordinator _fileCoordinator;
    private readonly DrawCandidatePoolBuilder _candidatePoolBuilder;
    private readonly DrawStatisticsService _statisticsService;
    private readonly MealCardMetadataValidator _metadataValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardLibraryService"/> class.
    /// </summary>
    /// <param name="options">The card-library options.</param>
    /// <param name="randomizer">The random index generator used for draw fairness.</param>
    /// <param name="duplicateDetector">The duplicate detector.</param>
    /// <param name="logger">The structured logger.</param>
    /// <param name="localizationService">The localized card projection service.</param>
    /// <param name="fileCoordinator">The same-process file coordination gate.</param>
    /// <param name="candidatePoolBuilder">The draw candidate-pool builder.</param>
    /// <param name="statisticsService">The draw statistics projection service.</param>
    /// <param name="metadataValidator">The decision metadata validator.</param>
    public CardLibraryService(
        IOptions<CardLibraryOptions> options,
        IMealCardRandomizer randomizer,
        DuplicateCardDetector duplicateDetector,
        ILogger<CardLibraryService> logger,
        MealCardLocalizationService? localizationService = null,
        CardLibraryFileCoordinator? fileCoordinator = null,
        DrawCandidatePoolBuilder? candidatePoolBuilder = null,
        DrawStatisticsService? statisticsService = null,
        MealCardMetadataValidator? metadataValidator = null)
    {
        _options = options.Value;
        _randomizer = randomizer;
        _duplicateDetector = duplicateDetector;
        _logger = logger;
        _localizationService = localizationService ?? new MealCardLocalizationService();
        _fileCoordinator = fileCoordinator ?? new CardLibraryFileCoordinator();
        _candidatePoolBuilder = candidatePoolBuilder ?? new DrawCandidatePoolBuilder();
        _statisticsService = statisticsService ?? new DrawStatisticsService(_localizationService);
        _metadataValidator = metadataValidator ?? new MealCardMetadataValidator();
    }

    /// <inheritdoc />
    public async Task<CardLibraryLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        var filePath = GetLibraryFilePath();
        if (!File.Exists(filePath))
        {
            return await CreateSeedFileAsync(filePath, cancellationToken);
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Card library file could not be read at {CardLibraryPath}", filePath);
            return CardLibraryLoadResult.BlockedUnreadable("Read failed.");
        }

        CardLibraryDocument? document;
        try
        {
            document = DeserializeDocument(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Card library file contains invalid JSON at {CardLibraryPath}", filePath);
            return CardLibraryLoadResult.BlockedCorrupt("JSON parse failed.");
        }

        var validationError = ValidateDocument(document, requireCompleteEnglish: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Card library validation failed: {ValidationError}", validationError);
            return CardLibraryLoadResult.BlockedCorrupt(validationError);
        }

        _logger.LogInformation(
            "Card library loaded from {CardLibraryPath} with {CardCount} cards using schema {SchemaVersion}",
            filePath,
            document!.Cards.Count,
            document.SchemaVersion);
        return CardLibraryLoadResult.Ready(document);
    }

    /// <inheritdoc />
    public async Task<bool> IsBlockedAsync(CancellationToken cancellationToken = default)
    {
        return (await LoadAsync(cancellationToken)).IsBlocked;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MealCard>> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        return SearchCoreAsync(criteria, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LocalizedMealCardView>> SearchLocalizedAsync(SearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var cards = await SearchCoreAsync(criteria, cancellationToken);
        return _localizationService.ProjectMany(cards, criteria.CurrentLanguage);
    }

    /// <inheritdoc />
    public Task<MealCard?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return FindByIdCoreAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LocalizedMealCardView?> FindLocalizedByIdAsync(Guid id, SupportedLanguage language, CancellationToken cancellationToken = default)
    {
        var card = await FindByIdCoreAsync(id, cancellationToken);
        return card is null ? null : _localizationService.Project(card, language);
    }

    /// <inheritdoc />
    public Task<DrawResult> DrawAsync(MealType mealType, CancellationToken cancellationToken = default)
    {
        return DrawAsync(mealType, SupportedLanguage.ZhTw, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DrawResult> DrawAsync(MealType mealType, SupportedLanguage language, CancellationToken cancellationToken = default)
    {
        return DrawCoreAsync(new DrawOperation
        {
            OperationId = Guid.NewGuid(),
            Mode = DrawMode.Normal,
            MealType = mealType,
            CoinInserted = true,
            RequestedLanguage = language
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DrawResult> DrawAsync(DrawOperation operation, CancellationToken cancellationToken = default)
    {
        return DrawCoreAsync(operation, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DrawStatisticsSummary> GetDrawStatisticsAsync(SupportedLanguage language, CancellationToken cancellationToken = default)
    {
        var loadResult = await LoadAsync(cancellationToken);
        if (loadResult.IsBlocked || loadResult.Document is null)
        {
            _logger.LogWarning(
                "Draw statistics blocked because card library is unavailable. Status: {LoadStatus}",
                loadResult.Status);
            return new DrawStatisticsSummary(0, Array.Empty<CardDrawStatistic>(), loadResult.MessageKey);
        }

        var summary = _statisticsService.CreateSummary(loadResult.Document, language);
        _logger.LogInformation(
            "Draw statistics projected with {TotalSuccessfulDraws} successful draws and {StatisticRowCount} rows",
            summary.TotalSuccessfulDraws,
            summary.Rows.Count);
        return summary;
    }

    /// <inheritdoc />
    public Task<CardLibraryMutationResult> CreateAsync(MealCardInputModel input, CancellationToken cancellationToken = default)
    {
        return CreateCoreAsync(input, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CardLibraryMutationResult> UpdateAsync(Guid id, MealCardInputModel input, CancellationToken cancellationToken = default)
    {
        return UpdateCoreAsync(id, input, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CardLibraryMutationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DeleteCoreAsync(id, cancellationToken);
    }

    private async Task<CardLibraryLoadResult> CreateSeedFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var document = SeedMealCards.CreateDocument();
        var validationError = ValidateDocument(document, requireCompleteEnglish: false);
        if (validationError is not null)
        {
            _logger.LogCritical("Seed card data is invalid: {ValidationError}", validationError);
            return CardLibraryLoadResult.BlockedCorrupt("Seed data invalid.");
        }

        try
        {
            await WriteDocumentAsync(filePath, document, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Missing card library could not be created at {CardLibraryPath}", filePath);
            return CardLibraryLoadResult.BlockedUnreadable("Seed write failed.");
        }

        _logger.LogInformation(
            "Missing card library created at {CardLibraryPath} with {CardCount} seed cards",
            filePath,
            document.Cards.Count);
        return CardLibraryLoadResult.CreatedFromSeed(document);
    }

    private async Task<DrawResult> DrawCoreAsync(DrawOperation operation, CancellationToken cancellationToken)
    {
        if (!operation.HasValidOperationId)
        {
            _logger.LogWarning("Draw rejected because operation ID is empty");
            return DrawResult.Failure(operation, "抽卡操作已失效，請重新整理後再試。", "Draw.InvalidOperationId");
        }

        if (!operation.HasValidMode)
        {
            _logger.LogWarning("Draw rejected because mode is invalid");
            return DrawResult.Failure(operation, "請選擇正常模式或隨機模式。", "Draw.InvalidMode");
        }

        if (!operation.CoinInserted)
        {
            _logger.LogWarning("Draw rejected because coin confirmation is missing");
            return DrawResult.Failure(operation, "請先投幣再拉桿。", "Draw.CoinRequired");
        }

        if (!operation.HasValidMealType)
        {
            _logger.LogWarning("Draw rejected because meal type is invalid for {DrawMode}", operation.Mode);
            return DrawResult.Failure(operation, "請先選擇早餐、午餐或晚餐。", "Draw.InvalidMealType");
        }

        return await _fileCoordinator.RunExclusiveAsync(async innerCancellationToken =>
        {
            var loadResult = await LoadAsync(innerCancellationToken);
            if (loadResult.IsBlocked || loadResult.Document is null)
            {
                _logger.LogWarning("Draw blocked because card library is unavailable. Status: {LoadStatus}", loadResult.Status);
                return DrawResult.Failure(operation, loadResult.UserMessage, loadResult.MessageKey);
            }

            var existingHistory = loadResult.Document.DrawHistory.FirstOrDefault(history => history.OperationId == operation.OperationId);
            if (existingHistory is not null)
            {
                var replayCard = loadResult.Document.Cards.FirstOrDefault(card => card.Id == existingHistory.CardId);
                if (replayCard is null)
                {
                    _logger.LogError("Draw replay failed because card {CardId} is missing", existingHistory.CardId);
                    return DrawResult.Failure(operation, "原抽卡結果已無法顯示，請檢查卡牌庫。", "Draw.ReplayUnavailable");
                }

                _logger.LogInformation(
                    "Draw operation {DrawOperationId} replayed card {CardId}",
                    operation.OperationId,
                    replayCard.Id);
                var replayProjection = _localizationService.Project(replayCard, operation.RequestedLanguage);
                return DrawResult.Success(operation, replayCard, replayProjection, "已重顯同一次抽卡結果。", "Draw.Replay", isReplay: true);
            }

            var pool = _candidatePoolBuilder.Build(operation, loadResult.Document.Cards);
            if (pool.Cards.Count == 0)
            {
                _logger.LogWarning(
                    "Draw rejected because candidate pool is empty for mode {DrawMode} and meal type {MealType}",
                    operation.Mode,
                    operation.MealType);
                return DrawResult.Failure(operation, "目前沒有可抽取的餐點卡牌。", "Draw.EmptyPool");
            }

            var selected = pool.Cards[_randomizer.NextIndex(pool.Cards.Count)];
            var history = new DrawHistoryRecord
            {
                Id = Guid.NewGuid(),
                OperationId = operation.OperationId,
                DrawMode = operation.Mode,
                CardId = selected.Id,
                MealTypeAtDraw = selected.MealType,
                SucceededAtUtc = DateTimeOffset.UtcNow
            };
            var updatedDocument = new CardLibraryDocument
            {
                SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
                Cards = loadResult.Document.Cards,
                DrawHistory = loadResult.Document.DrawHistory.Concat(new[] { history }).ToList()
            };

            var writeResult = await TryWriteDocumentAsync(updatedDocument, innerCancellationToken, requireCompleteEnglish: false);
            if (writeResult is not null)
            {
                _logger.LogError(
                    "Draw operation {DrawOperationId} failed while writing selected card {CardId}",
                    operation.OperationId,
                    selected.Id);
                return DrawResult.Failure(operation, "抽卡結果暫時無法保存，請稍後再試。", "Draw.WriteFailed");
            }

            _logger.LogInformation(
                "Draw operation {DrawOperationId} succeeded with mode {DrawMode}, card {CardId}, pool size {PoolCount}",
                operation.OperationId,
                operation.Mode,
                selected.Id,
                pool.Cards.Count);

            var localizedCard = _localizationService.Project(selected, operation.RequestedLanguage);
            return DrawResult.Success(operation, selected, localizedCard, "已抽出餐點卡牌。", "Draw.Success");
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<MealCard>> SearchCoreAsync(SearchCriteria criteria, CancellationToken cancellationToken)
    {
        var loadResult = await LoadAsync(cancellationToken);
        if (loadResult.IsBlocked || loadResult.Document is null)
        {
            return Array.Empty<MealCard>();
        }

        if (criteria.MealType is MealType mealType && !Enum.IsDefined(typeof(MealType), mealType))
        {
            return Array.Empty<MealCard>();
        }

        var query = loadResult.Document.Cards.Where(card => card.IsActive);
        var keyword = criteria.NormalizedKeyword;
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(card =>
                _localizationService.Project(card, criteria.CurrentLanguage)
                    .DisplayName
                    .Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.MealType is not null)
        {
            query = query.Where(card => card.MealType == criteria.MealType.Value);
        }

        return query
            .OrderBy(card => card.MealType)
            .ThenBy(card => _localizationService.Project(card, criteria.CurrentLanguage).DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<MealCard?> FindByIdCoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var loadResult = await LoadAsync(cancellationToken);
        if (loadResult.IsBlocked || loadResult.Document is null)
        {
            return null;
        }

        return loadResult.Document.Cards.FirstOrDefault(card => card.Id == id && card.IsActive);
    }

    private async Task<CardLibraryMutationResult> CreateCoreAsync(MealCardInputModel input, CancellationToken cancellationToken)
    {
        return await _fileCoordinator.RunExclusiveAsync(async innerCancellationToken =>
        {
            var loadResult = await LoadAsync(innerCancellationToken);
            if (loadResult.IsBlocked || loadResult.Document is null)
            {
                return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.Blocked, loadResult.UserMessage);
            }

            var normalized = input.Normalize();
            if (!IsValidInput(normalized))
            {
                _logger.LogWarning("Create card rejected due to invalid input");
                return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.ValidationFailed, "請確認餐點名稱、餐別與描述皆已正確填寫。");
            }

            if (_duplicateDetector.HasDuplicate(loadResult.Document.Cards, normalized))
            {
                _logger.LogWarning("Create card rejected because duplicate card content was submitted");
                return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.Duplicate, "已有相同餐點名稱、餐別與描述的卡牌。");
            }

            var card = new MealCard(Guid.NewGuid(), normalized.MealType!.Value, normalized.ToLocalizations());
            var updatedDocument = new CardLibraryDocument
            {
                SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
                Cards = loadResult.Document.Cards.Concat(new[] { card }).ToList(),
                DrawHistory = loadResult.Document.DrawHistory
            };

            var writeResult = await TryWriteDocumentAsync(updatedDocument, innerCancellationToken);
            return writeResult ?? CardLibraryMutationResult.Success(card, "已新增餐點卡牌。");
        }, cancellationToken);
    }

    private async Task<CardLibraryMutationResult> UpdateCoreAsync(Guid id, MealCardInputModel input, CancellationToken cancellationToken)
    {
        return await _fileCoordinator.RunExclusiveAsync(async innerCancellationToken =>
        {
            var loadResult = await LoadAsync(innerCancellationToken);
            if (loadResult.IsBlocked || loadResult.Document is null)
            {
                return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.Blocked, loadResult.UserMessage);
            }

            var existing = loadResult.Document.Cards.FirstOrDefault(card => card.Id == id && card.IsActive);
            if (existing is null)
            {
                return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.NotFound, "找不到餐點卡牌。");
            }

            var normalized = input.Normalize();
            if (!IsValidInput(normalized))
            {
                _logger.LogWarning("Update card {CardId} rejected due to invalid input", id);
                return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.ValidationFailed, "請確認餐點名稱、餐別與描述皆已正確填寫。");
            }

            if (_duplicateDetector.HasDuplicate(loadResult.Document.Cards, normalized, ignoredCardId: id))
            {
                _logger.LogWarning("Update card {CardId} rejected because duplicate card content was submitted", id);
                return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.Duplicate, "已有相同餐點名稱、餐別與描述的卡牌。");
            }

            var updatedCard = new MealCard(
                id,
                normalized.MealType!.Value,
                normalized.ToLocalizations(),
                existing.Status,
                existing.DeletedAtUtc,
                existing.DecisionMetadata);
            var cards = loadResult.Document.Cards
                .Select(card => card.Id == id ? updatedCard : card)
                .ToList();
            var updatedDocument = new CardLibraryDocument
            {
                SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
                Cards = cards,
                DrawHistory = loadResult.Document.DrawHistory
            };

            var writeResult = await TryWriteDocumentAsync(updatedDocument, innerCancellationToken);
            return writeResult ?? CardLibraryMutationResult.Success(updatedCard, "已更新餐點卡牌。");
        }, cancellationToken);
    }

    private async Task<CardLibraryMutationResult> DeleteCoreAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _fileCoordinator.RunExclusiveAsync(async innerCancellationToken =>
        {
            var loadResult = await LoadAsync(innerCancellationToken);
            if (loadResult.IsBlocked || loadResult.Document is null)
            {
                return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.Blocked, loadResult.UserMessage);
            }

            var existing = loadResult.Document.Cards.FirstOrDefault(card => card.Id == id && card.IsActive);
            if (existing is null)
            {
                return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.NotFound, "找不到餐點卡牌。");
            }

            var hasHistory = loadResult.Document.DrawHistory.Any(history => history.CardId == id);
            var cards = hasHistory
                ? loadResult.Document.Cards
                    .Select(card => card.Id == id
                        ? new MealCard(
                            card.Id,
                            card.MealType,
                            card.Localizations,
                            CardStatus.Deleted,
                            DateTimeOffset.UtcNow,
                            card.DecisionMetadata)
                        : card)
                    .ToList()
                : loadResult.Document.Cards.Where(card => card.Id != id).ToList();
            var updatedDocument = new CardLibraryDocument
            {
                SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
                Cards = cards,
                DrawHistory = loadResult.Document.DrawHistory
            };

            var writeResult = await TryWriteDocumentAsync(updatedDocument, innerCancellationToken);
            if (writeResult is not null)
            {
                return writeResult;
            }

            if (hasHistory)
            {
                _logger.LogInformation("Card {CardId} retained as deleted because it has draw history", id);
            }
            else
            {
                _logger.LogInformation("Card {CardId} deleted with no draw history", id);
            }

            var message = hasHistory
                ? "已刪除餐點卡牌，歷史統計已保留。"
                : "已刪除餐點卡牌。";
            return CardLibraryMutationResult.Success(existing, message);
        }, cancellationToken);
    }

    private async Task<CardLibraryMutationResult?> TryWriteDocumentAsync(
        CardLibraryDocument document,
        CancellationToken cancellationToken,
        bool requireCompleteEnglish = true)
    {
        var validationError = ValidateDocument(document, requireCompleteEnglish);
        if (validationError is not null)
        {
            _logger.LogError("Card library write rejected because the new document is invalid: {ValidationError}", validationError);
            return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.ValidationFailed, "卡牌資料不完整，請確認雙語欄位皆已填寫。");
        }

        try
        {
            await WriteDocumentAsync(GetLibraryFilePath(), document, cancellationToken);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Card library write failed");
            return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.WriteFailed, "卡牌暫時無法儲存，請稍後再試。");
        }
    }

    private static bool IsValidInput(MealCardInputModel input)
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        return System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            input,
            new System.ComponentModel.DataAnnotations.ValidationContext(input),
            validationResults,
            validateAllProperties: true);
    }

    private async Task WriteDocumentAsync(string filePath, CardLibraryDocument document, CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var tempPath = filePath + ".tmp";
        try
        {
            await using (var stream = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough | FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private string GetLibraryFilePath()
    {
        return string.IsNullOrWhiteSpace(_options.LibraryFilePath)
            ? Path.Combine(AppContext.BaseDirectory, "data", "cards.json")
            : _options.LibraryFilePath;
    }

    private static CardLibraryDocument? DeserializeDocument(string json)
    {
        using var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;
        if (!root.TryGetProperty("schemaVersion", out var schemaVersionElement) ||
            !schemaVersionElement.TryGetInt32(out var schemaVersion) ||
            !root.TryGetProperty("cards", out var cardsElement) ||
            cardsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        if ((schemaVersion == CardLibraryDocument.CurrentSchemaVersion ||
                schemaVersion == CardLibraryDocument.DrawHistorySchemaVersion) &&
            (!root.TryGetProperty("drawHistory", out var drawHistoryElement) ||
                drawHistoryElement.ValueKind != JsonValueKind.Array))
        {
            return null;
        }

        return schemaVersion switch
        {
            CardLibraryDocument.LegacySchemaVersion => ConvertLegacyDocument(JsonSerializer.Deserialize<LegacyCardLibraryDocument>(json, JsonOptions)),
            CardLibraryDocument.BilingualSchemaVersion => ConvertBilingualDocument(JsonSerializer.Deserialize<CardLibraryDocument>(json, JsonOptions)),
            CardLibraryDocument.DrawHistorySchemaVersion => NormalizeCurrentDocument(JsonSerializer.Deserialize<CardLibraryDocument>(json, JsonOptions)),
            CardLibraryDocument.CurrentSchemaVersion => NormalizeCurrentDocument(JsonSerializer.Deserialize<CardLibraryDocument>(json, JsonOptions)),
            _ => new CardLibraryDocument { SchemaVersion = schemaVersion, Cards = Array.Empty<MealCard>() }
        };
    }

    private static CardLibraryDocument? ConvertLegacyDocument(LegacyCardLibraryDocument? legacyDocument)
    {
        if (legacyDocument?.Cards is null)
        {
            return null;
        }

        return new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = legacyDocument.Cards
                .Select(card =>
                {
                    var name = card.Name ?? string.Empty;
                    var description = card.Description ?? string.Empty;
                    return new MealCard(
                        card.Id,
                        card.MealType,
                        new Dictionary<string, MealCardLocalizedContent>
                        {
                            [SupportedLanguage.ZhTw.CultureName] = new(name, description)
                        });
                })
                .ToList(),
            DrawHistory = Array.Empty<DrawHistoryRecord>()
        };
    }

    private static CardLibraryDocument? ConvertBilingualDocument(CardLibraryDocument? document)
    {
        if (document?.Cards is null)
        {
            return null;
        }

        return new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = document.Cards
                .Select(card => new MealCard(
                    card.Id,
                    card.MealType,
                    card.Localizations,
                    CardStatus.Active,
                    deletedAtUtc: null))
                .ToList(),
            DrawHistory = Array.Empty<DrawHistoryRecord>()
        };
    }

    private static CardLibraryDocument? NormalizeCurrentDocument(CardLibraryDocument? document)
    {
        if (document?.Cards is null || document.DrawHistory is null)
        {
            return null;
        }

        return new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = document.Cards.Select(card => card.Normalize()).ToList(),
            DrawHistory = document.DrawHistory.ToList()
        };
    }

    private string? ValidateDocument(CardLibraryDocument? document, bool requireCompleteEnglish)
    {
        if (document is null)
        {
            return "Document is null.";
        }

        if (document.SchemaVersion is not CardLibraryDocument.CurrentSchemaVersion)
        {
            return "Unsupported schema version.";
        }

        if (document.Cards is null)
        {
            return "Cards collection is missing.";
        }

        var seenIds = new HashSet<Guid>();
        foreach (var card in document.Cards)
        {
            if (card.Id == Guid.Empty)
            {
                return "Card ID is empty.";
            }

            if (!seenIds.Add(card.Id))
            {
                return "Duplicate card ID.";
            }

            if (!Enum.IsDefined(typeof(MealType), card.MealType))
            {
                return "Card meal type is invalid.";
            }

            if (!Enum.IsDefined(typeof(CardStatus), card.Status))
            {
                return "Card status is invalid.";
            }

            if (card.Status == CardStatus.Active && card.DeletedAtUtc is not null)
            {
                return "Active card cannot have a deletion time.";
            }

            if (card.Status == CardStatus.Deleted && card.DeletedAtUtc is null)
            {
                return "Deleted card requires a deletion time.";
            }

            var metadataResult = _metadataValidator.ValidateAndNormalize(card.DecisionMetadata);
            if (!metadataResult.Succeeded)
            {
                return metadataResult.MessageKey;
            }

            foreach (var cultureName in card.Localizations.Keys)
            {
                if (!SupportedLanguage.TryGet(cultureName, out _))
                {
                    return "Card localization culture is unsupported.";
                }
            }

            if (!card.HasCompleteContent(SupportedLanguage.ZhTw))
            {
                return "Card Traditional Chinese content is missing.";
            }

            if (requireCompleteEnglish && !card.HasCompleteContent(SupportedLanguage.EnUs))
            {
                return "Card English content is missing.";
            }

            if (card.IsActive && _duplicateDetector.HasDuplicate(document.Cards.Where(existing => existing.IsActive), card))
            {
                return "Duplicate card content.";
            }
        }

        if (document.DrawHistory is null)
        {
            return "Draw history collection is missing.";
        }

        var cardIds = document.Cards.Select(card => card.Id).ToHashSet();
        var operationIds = new HashSet<Guid>();
        var historyIds = new HashSet<Guid>();
        foreach (var record in document.DrawHistory)
        {
            if (record.Id == Guid.Empty)
            {
                return "Draw history ID is empty.";
            }

            if (!historyIds.Add(record.Id))
            {
                return "Duplicate draw history ID.";
            }

            if (record.OperationId == Guid.Empty)
            {
                return "Draw history operation ID is empty.";
            }

            if (!operationIds.Add(record.OperationId))
            {
                return "Duplicate draw operation ID.";
            }

            if (!Enum.IsDefined(typeof(DrawMode), record.DrawMode))
            {
                return "Draw history mode is invalid.";
            }

            if (record.CardId == Guid.Empty || !cardIds.Contains(record.CardId))
            {
                return "Draw history card reference is invalid.";
            }

            if (!Enum.IsDefined(typeof(MealType), record.MealTypeAtDraw))
            {
                return "Draw history meal type is invalid.";
            }

            if (record.SucceededAtUtc == default)
            {
                return "Draw history success time is missing.";
            }
        }

        return null;
    }

    private sealed class LegacyCardLibraryDocument
    {
        public int SchemaVersion { get; init; }

        public IReadOnlyList<LegacyMealCard>? Cards { get; init; }
    }

    private sealed class LegacyMealCard
    {
        public Guid Id { get; init; }

        public string? Name { get; init; }

        public MealType MealType { get; init; }

        public string? Description { get; init; }
    }
}
