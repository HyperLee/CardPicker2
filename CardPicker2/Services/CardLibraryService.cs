using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.Models;

using Microsoft.Extensions.Options;

namespace CardPicker2.Services;

/// <summary>
/// Loads, validates, and persists the local JSON card library.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CardLibraryService"/> class.
    /// </summary>
    /// <param name="options">The card-library options.</param>
    /// <param name="randomizer">The random index generator used for draw fairness.</param>
    /// <param name="duplicateDetector">The duplicate detector.</param>
    /// <param name="logger">The structured logger.</param>
    /// <param name="localizationService">The localized card projection service.</param>
    public CardLibraryService(
        IOptions<CardLibraryOptions> options,
        IMealCardRandomizer randomizer,
        DuplicateCardDetector duplicateDetector,
        ILogger<CardLibraryService> logger,
        MealCardLocalizationService? localizationService = null)
    {
        _options = options.Value;
        _randomizer = randomizer;
        _duplicateDetector = duplicateDetector;
        _logger = logger;
        _localizationService = localizationService ?? new MealCardLocalizationService();
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

        var validationError = ValidateDocument(document);
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
        return DrawCoreAsync(mealType, SupportedLanguage.ZhTw, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DrawResult> DrawAsync(MealType mealType, SupportedLanguage language, CancellationToken cancellationToken = default)
    {
        return DrawCoreAsync(mealType, language, cancellationToken);
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
        var validationError = ValidateDocument(document);
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

    private async Task<DrawResult> DrawCoreAsync(MealType mealType, SupportedLanguage language, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(MealType), mealType))
        {
            _logger.LogWarning("Draw rejected because meal type is invalid: {MealType}", mealType);
            return DrawResult.Failure(mealType, "請先選擇早餐、午餐或晚餐。", "Draw.InvalidMealType");
        }

        var loadResult = await LoadAsync(cancellationToken);
        if (loadResult.IsBlocked || loadResult.Document is null)
        {
            _logger.LogWarning("Draw blocked because card library is unavailable. Status: {LoadStatus}", loadResult.Status);
            return DrawResult.Failure(mealType, loadResult.UserMessage, loadResult.MessageKey);
        }

        var pool = loadResult.Document.Cards
            .Where(card => card.MealType == mealType)
            .ToList();
        if (pool.Count == 0)
        {
            _logger.LogWarning("Draw rejected because meal type {MealType} has no cards", mealType);
            return DrawResult.Failure(mealType, "這個餐別目前沒有可抽取的餐點卡牌。", "Draw.EmptyMealPool");
        }

        var selected = pool[_randomizer.NextIndex(pool.Count)];
        _logger.LogInformation(
            "Meal draw succeeded for {MealType} with card {CardId} from pool size {PoolCount}",
            mealType,
            selected.Id,
            pool.Count);

        var localizedCard = _localizationService.Project(selected, language);
        return DrawResult.Success(mealType, selected, localizedCard, "已抽出餐點卡牌。", "Draw.Success");
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

        var query = loadResult.Document.Cards.AsEnumerable();
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

        return loadResult.Document.Cards.FirstOrDefault(card => card.Id == id);
    }

    private async Task<CardLibraryMutationResult> CreateCoreAsync(MealCardInputModel input, CancellationToken cancellationToken)
    {
        var loadResult = await LoadAsync(cancellationToken);
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
            Cards = loadResult.Document.Cards.Concat(new[] { card }).ToList()
        };

        var writeResult = await TryWriteDocumentAsync(updatedDocument, cancellationToken);
        return writeResult ?? CardLibraryMutationResult.Success(card, "已新增餐點卡牌。");
    }

    private async Task<CardLibraryMutationResult> UpdateCoreAsync(Guid id, MealCardInputModel input, CancellationToken cancellationToken)
    {
        var loadResult = await LoadAsync(cancellationToken);
        if (loadResult.IsBlocked || loadResult.Document is null)
        {
            return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.Blocked, loadResult.UserMessage);
        }

        var existing = loadResult.Document.Cards.FirstOrDefault(card => card.Id == id);
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

        var updatedCard = new MealCard(id, normalized.MealType!.Value, normalized.ToLocalizations());
        var cards = loadResult.Document.Cards
            .Select(card => card.Id == id ? updatedCard : card)
            .ToList();
        var updatedDocument = new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = cards
        };

        var writeResult = await TryWriteDocumentAsync(updatedDocument, cancellationToken);
        return writeResult ?? CardLibraryMutationResult.Success(updatedCard, "已更新餐點卡牌。");
    }

    private async Task<CardLibraryMutationResult> DeleteCoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var loadResult = await LoadAsync(cancellationToken);
        if (loadResult.IsBlocked || loadResult.Document is null)
        {
            return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.Blocked, loadResult.UserMessage);
        }

        var existing = loadResult.Document.Cards.FirstOrDefault(card => card.Id == id);
        if (existing is null)
        {
            return CardLibraryMutationResult.Failure(CardLibraryMutationStatus.NotFound, "找不到餐點卡牌。");
        }

        var updatedDocument = new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = loadResult.Document.Cards.Where(card => card.Id != id).ToList()
        };

        var writeResult = await TryWriteDocumentAsync(updatedDocument, cancellationToken);
        return writeResult ?? CardLibraryMutationResult.Success(existing, "已刪除餐點卡牌。");
    }

    private async Task<CardLibraryMutationResult?> TryWriteDocumentAsync(CardLibraryDocument document, CancellationToken cancellationToken)
    {
        var validationError = ValidateDocument(document);
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

        return schemaVersion switch
        {
            CardLibraryDocument.LegacySchemaVersion => ConvertLegacyDocument(JsonSerializer.Deserialize<LegacyCardLibraryDocument>(json, JsonOptions)),
            CardLibraryDocument.CurrentSchemaVersion => JsonSerializer.Deserialize<CardLibraryDocument>(json, JsonOptions),
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
            SchemaVersion = CardLibraryDocument.LegacySchemaVersion,
            Cards = legacyDocument.Cards
                .Select(card => new MealCard(card.Id, card.Name ?? string.Empty, card.MealType, card.Description ?? string.Empty))
                .ToList()
        };
    }

    private string? ValidateDocument(CardLibraryDocument? document)
    {
        if (document is null)
        {
            return "Document is null.";
        }

        if (document.SchemaVersion is not CardLibraryDocument.LegacySchemaVersion and not CardLibraryDocument.CurrentSchemaVersion)
        {
            return "Unsupported schema version.";
        }

        if (document.Cards is null)
        {
            return "Cards collection is missing.";
        }

        var requireCompleteEnglish = document.SchemaVersion == CardLibraryDocument.CurrentSchemaVersion;
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

            if (_duplicateDetector.HasDuplicate(document.Cards, card))
            {
                return "Duplicate card content.";
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
