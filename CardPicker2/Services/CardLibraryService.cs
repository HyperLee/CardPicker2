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
    private readonly DuplicateCardDetector _duplicateDetector;
    private readonly ILogger<CardLibraryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardLibraryService"/> class.
    /// </summary>
    /// <param name="options">The card-library options.</param>
    /// <param name="duplicateDetector">The duplicate detector.</param>
    /// <param name="logger">The structured logger.</param>
    public CardLibraryService(
        IOptions<CardLibraryOptions> options,
        DuplicateCardDetector duplicateDetector,
        ILogger<CardLibraryService> logger)
    {
        _options = options.Value;
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
        return Task.FromResult<IReadOnlyList<MealCard>>(Array.Empty<MealCard>());
    }

    /// <inheritdoc />
    public Task<MealCard?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<MealCard?>(null);
    }

    /// <inheritdoc />
    public Task<DrawResult> DrawAsync(MealType mealType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DrawResult.Failure(mealType, "抽卡功能尚未完成。"));
    }

    /// <inheritdoc />
    public Task<CardLibraryMutationResult> CreateAsync(MealCardInputModel input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CardLibraryMutationResult.NotAvailable("卡牌管理功能尚未完成。"));
    }

    /// <inheritdoc />
    public Task<CardLibraryMutationResult> UpdateAsync(Guid id, MealCardInputModel input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CardLibraryMutationResult.NotAvailable("卡牌管理功能尚未完成。"));
    }

    /// <inheritdoc />
    public Task<CardLibraryMutationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CardLibraryMutationResult.NotAvailable("卡牌管理功能尚未完成。"));
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
