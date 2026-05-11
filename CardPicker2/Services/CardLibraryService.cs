using System.ComponentModel.DataAnnotations;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CardLibraryService"/> class.
    /// </summary>
    /// <param name="options">The card-library options.</param>
    /// <param name="randomizer">The random index generator used for draw fairness.</param>
    /// <param name="duplicateDetector">The duplicate detector.</param>
    /// <param name="logger">The structured logger.</param>
    public CardLibraryService(
        IOptions<CardLibraryOptions> options,
        IMealCardRandomizer randomizer,
        DuplicateCardDetector duplicateDetector,
        ILogger<CardLibraryService> logger)
    {
        _options = options.Value;
        _randomizer = randomizer;
        _duplicateDetector = duplicateDetector;
        _logger = logger;
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
            document = JsonSerializer.Deserialize<CardLibraryDocument>(json, JsonOptions);
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
            "Card library loaded from {CardLibraryPath} with {CardCount} cards",
            filePath,
            document!.Cards.Count);
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
    public Task<MealCard?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return FindByIdCoreAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DrawResult> DrawAsync(MealType mealType, CancellationToken cancellationToken = default)
    {
        return DrawCoreAsync(mealType, cancellationToken);
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

    private async Task<DrawResult> DrawCoreAsync(MealType mealType, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(MealType), mealType))
        {
            _logger.LogWarning("Draw rejected because meal type is invalid: {MealType}", mealType);
            return DrawResult.Failure(mealType, "請先選擇早餐、午餐或晚餐。");
        }

        var loadResult = await LoadAsync(cancellationToken);
        if (loadResult.IsBlocked || loadResult.Document is null)
        {
            _logger.LogWarning("Draw blocked because card library is unavailable. Status: {LoadStatus}", loadResult.Status);
            return DrawResult.Failure(mealType, loadResult.UserMessage);
        }

        var pool = loadResult.Document.Cards
            .Where(card => card.MealType == mealType)
            .ToList();
        if (pool.Count == 0)
        {
            _logger.LogWarning("Draw rejected because meal type {MealType} has no cards", mealType);
            return DrawResult.Failure(mealType, "這個餐別目前沒有可抽取的餐點卡牌。");
        }

        var selected = pool[_randomizer.NextIndex(pool.Count)];
        _logger.LogInformation(
            "Meal draw succeeded for {MealType} with card {CardId} from pool size {PoolCount}",
            mealType,
            selected.Id,
            pool.Count);

        return DrawResult.Success(mealType, selected);
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
            query = query.Where(card => card.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.MealType is not null)
        {
            query = query.Where(card => card.MealType == criteria.MealType.Value);
        }

        return query
            .OrderBy(card => card.MealType)
            .ThenBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
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

        var card = new MealCard(Guid.NewGuid(), normalized.Name!, normalized.MealType!.Value, normalized.Description!);
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

        var updatedCard = new MealCard(id, normalized.Name!, normalized.MealType!.Value, normalized.Description!);
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
        var validationResults = new List<ValidationResult>();
        return Validator.TryValidateObject(input, new ValidationContext(input), validationResults, validateAllProperties: true);
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

    private string? ValidateDocument(CardLibraryDocument? document)
    {
        if (document is null)
        {
            return "Document is null.";
        }

        if (document.SchemaVersion != CardLibraryDocument.CurrentSchemaVersion)
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

            if (string.IsNullOrWhiteSpace(card.Name))
            {
                return "Card name is missing.";
            }

            if (!Enum.IsDefined(typeof(MealType), card.MealType))
            {
                return "Card meal type is invalid.";
            }

            if (string.IsNullOrWhiteSpace(card.Description))
            {
                return "Card description is missing.";
            }

            var validationResults = new List<ValidationResult>();
            var input = new MealCardInputModel
            {
                Name = card.Name,
                MealType = card.MealType,
                Description = card.Description
            };
            if (!Validator.TryValidateObject(input, new ValidationContext(input), validationResults, validateAllProperties: true))
            {
                return "Card input validation failed.";
            }

            if (_duplicateDetector.HasDuplicate(document.Cards, card))
            {
                return "Duplicate card content.";
            }
        }

        return null;
    }
}