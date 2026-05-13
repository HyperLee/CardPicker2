using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace CardPicker2.Pages;

public class IndexModel : PageModel
{
    private readonly ICardLibraryService _cardLibraryService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(ICardLibraryService cardLibraryService, IStringLocalizer<SharedResource> localizer)
    {
        _cardLibraryService = cardLibraryService;
        _localizer = localizer;
    }

    [BindProperty]
    public MealType? MealType { get; set; }

    [BindProperty]
    public bool CoinInserted { get; set; }

    [BindProperty]
    public DrawMode DrawMode { get; set; } = DrawMode.Normal;

    [BindProperty]
    public Guid DrawOperationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public PriceRange? PriceRange { get; set; }

    [BindProperty(SupportsGet = true)]
    public PreparationTimeRange? PreparationTimeRange { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<DietaryPreference> DietaryPreferences { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public SpiceLevel? MaxSpiceLevel { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> Tags { get; set; } = new();

    public IReadOnlyList<MealType> MealTypes { get; } = Enum.GetValues<MealType>();

    public IReadOnlyList<PriceRange> PriceRangeOptions { get; } = Enum.GetValues<PriceRange>();

    public IReadOnlyList<PreparationTimeRange> PreparationTimeRangeOptions { get; } = Enum.GetValues<PreparationTimeRange>();

    public IReadOnlyList<DietaryPreference> DietaryPreferenceOptions { get; } = Enum.GetValues<DietaryPreference>();

    public IReadOnlyList<SpiceLevel> SpiceLevelOptions { get; } = Enum.GetValues<SpiceLevel>();

    public SupportedLanguage CurrentLanguage => SupportedLanguage.FromCultureNameOrDefault(Thread.CurrentThread.CurrentUICulture.Name);

    public DrawOperationState OperationState { get; private set; } = DrawOperationState.Idle;

    public DrawResult? Result { get; private set; }

    public DrawStatisticsSummary Statistics { get; private set; } =
        new(0, Array.Empty<CardDrawStatistic>(), "Statistics.Empty");

    public CardLibraryLoadResult? LibraryState { get; private set; }

    public string? StatusMessage { get; private set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public FilterSummary CurrentFilterSummary { get; private set; } = FilterSummary.Empty;

    public async Task OnGetAsync(
        MealType? mealType,
        bool coinInserted,
        Guid? resultCardId,
        DrawMode? drawMode,
        Guid? drawOperationId,
        PriceRange? priceRange,
        PreparationTimeRange? preparationTimeRange,
        List<DietaryPreference>? dietaryPreferences,
        SpiceLevel? maxSpiceLevel,
        List<string>? tags,
        CancellationToken cancellationToken)
    {
        MealType = mealType;
        CoinInserted = coinInserted;
        PriceRange = priceRange;
        PreparationTimeRange = preparationTimeRange;
        DietaryPreferences = dietaryPreferences ?? new List<DietaryPreference>();
        MaxSpiceLevel = maxSpiceLevel;
        Tags = tags ?? new List<string>();
        CurrentFilterSummary = CreateLocalizedFilterSummary(BuildCriteria());
        DrawMode = drawMode is DrawMode submittedMode && Enum.IsDefined(typeof(DrawMode), submittedMode)
            ? submittedMode
            : CardPicker2.Models.DrawMode.Normal;
        DrawOperationId = drawOperationId is Guid operationId && operationId != Guid.Empty
            ? operationId
            : Guid.NewGuid();
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            Statistics = new DrawStatisticsSummary(0, Array.Empty<CardDrawStatistic>(), LibraryState.MessageKey);
            OperationState = DrawOperationState.Blocked;
            StatusMessage = LibraryState.UserMessage;
            return;
        }

        Statistics = await _cardLibraryService.GetDrawStatisticsAsync(CurrentLanguage, cancellationToken);

        if (resultCardId is Guid cardId)
        {
            await RestoreResultAsync(cardId, cancellationToken);
            return;
        }

        OperationState = DrawMode == CardPicker2.Models.DrawMode.Random
            ? DrawOperationState.MealSelected
            : DrawOperationStateTransitions.FromSelection(MealType);
        StatusMessage = OperationState == DrawOperationState.MealSelected
            ? _localizer["Home.Status.MealSelected"]
            : _localizer["Home.Status.ChooseMeal"];
    }

    public async Task OnPostDrawAsync(CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            Statistics = new DrawStatisticsSummary(0, Array.Empty<CardDrawStatistic>(), LibraryState.MessageKey);
            DrawOperationId = DrawOperationId == Guid.Empty ? Guid.NewGuid() : DrawOperationId;
            OperationState = DrawOperationState.Blocked;
            StatusMessage = LibraryState.UserMessage;
            return;
        }

        OperationState = DrawOperationState.Spinning;
        DrawOperationId = DrawOperationId == Guid.Empty ? Guid.NewGuid() : DrawOperationId;
        var operation = new DrawOperation
        {
            OperationId = DrawOperationId,
            Mode = DrawMode,
            MealType = MealType,
            CoinInserted = CoinInserted,
            RequestedLanguage = CurrentLanguage,
            Filters = BuildCriteria().ForDrawMode(DrawMode)
        };
        Result = await _cardLibraryService.DrawAsync(operation, cancellationToken);
        Statistics = await _cardLibraryService.GetDrawStatisticsAsync(CurrentLanguage, cancellationToken);
        OperationState = Result.Succeeded ? DrawOperationState.Revealed : DrawOperationState.Blocked;
        CurrentFilterSummary = CreateLocalizedFilterSummary(BuildCriteria());
        StatusMessage = Result.Succeeded
            ? (Result.IsReplay ? _localizer["Home.Status.Replay"] : _localizer["Home.Status.DrawSuccess"])
            : LocalizeStatus(Result.StatusKey, Result.UserMessage);
        if (Result.Succeeded)
        {
            DrawOperationId = Guid.NewGuid();
        }

        if (!Result.Succeeded)
        {
            if (Result.StatusKey == "Draw.InvalidMealType")
            {
                ModelState.AddModelError(nameof(MealType), Result.UserMessage);
            }
            else if (Result.StatusKey == "Draw.CoinRequired")
            {
                ModelState.AddModelError(nameof(CoinInserted), Result.UserMessage);
            }
            else
            {
                ModelState.AddModelError(string.Empty, StatusMessage);
            }
        }
    }

    public Task OnPostAsync(CancellationToken cancellationToken)
    {
        return OnPostDrawAsync(cancellationToken);
    }

    private async Task RestoreResultAsync(Guid cardId, CancellationToken cancellationToken)
    {
        var localizedCard = await _cardLibraryService.FindLocalizedByIdAsync(cardId, CurrentLanguage, cancellationToken);
        if (localizedCard is null || (MealType is not null && localizedCard.MealType != MealType))
        {
            OperationState = DrawOperationState.Blocked;
            StatusMessage = _localizer["Home.Status.ResultUnavailable"];
            return;
        }

        MealType ??= localizedCard.MealType;
        OperationState = DrawOperationState.Revealed;
        StatusMessage = _localizer["Home.Status.ResultRestored"];
        DrawOperationId = DrawOperationId == Guid.Empty ? Guid.NewGuid() : DrawOperationId;
        Result = new DrawResult(
            true,
            MealType.Value,
            localizedCard.CardId,
            localizedCard.DisplayName,
            localizedCard.MealType,
            localizedCard.DisplayDescription,
            StatusMessage,
            localizedCard,
            "Draw.Restored",
            DrawOperationId,
            DrawMode,
            MealType,
            AppliedFilters: BuildCriteria().ForDrawMode(DrawMode),
            FilterSummary: CurrentFilterSummary);
    }

    public string DisplayPriceRange(PriceRange value)
    {
        return _localizer[$"Metadata.PriceRange.{value}"];
    }

    public string DisplayPreparationTimeRange(PreparationTimeRange value)
    {
        return _localizer[$"Metadata.PreparationTimeRange.{value}"];
    }

    public string DisplayDietaryPreference(DietaryPreference value)
    {
        return _localizer[$"Metadata.DietaryPreference.{value}"];
    }

    public string DisplaySpiceLevel(SpiceLevel value)
    {
        return _localizer[$"Metadata.SpiceLevel.{value}"];
    }

    private CardFilterCriteria BuildCriteria()
    {
        return new CardFilterCriteria
        {
            MealType = MealType,
            PriceRange = PriceRange,
            PreparationTimeRange = PreparationTimeRange,
            DietaryPreferences = DietaryPreferences,
            MaxSpiceLevel = MaxSpiceLevel,
            Tags = SplitTags(Tags),
            CurrentLanguage = CurrentLanguage
        }.Normalize();
    }

    private FilterSummary CreateLocalizedFilterSummary(CardFilterCriteria criteria)
    {
        var normalized = criteria.Normalize();
        var items = new List<string>();
        if (normalized.PriceRange is PriceRange priceRange)
        {
            items.Add(DisplayPriceRange(priceRange));
        }

        if (normalized.PreparationTimeRange is PreparationTimeRange preparationTimeRange)
        {
            items.Add(DisplayPreparationTimeRange(preparationTimeRange));
        }

        items.AddRange(normalized.DietaryPreferences.Select(DisplayDietaryPreference));

        if (normalized.MaxSpiceLevel is SpiceLevel maxSpiceLevel)
        {
            items.Add(DisplaySpiceLevel(maxSpiceLevel));
        }

        items.AddRange(normalized.Tags);
        return items.Count == 0 ? FilterSummary.Empty : new FilterSummary(items);
    }

    private string LocalizeStatus(string statusKey, string fallback)
    {
        return statusKey switch
        {
            "Metadata.Filter.EmptyPool" => _localizer["Metadata.Filter.EmptyPool"],
            "Metadata.InvalidEnum" => _localizer["Metadata.Validation.InvalidEnum"],
            "Metadata.InvalidTag" => _localizer["Metadata.Validation.InvalidTag"],
            _ => fallback
        };
    }

    private static IReadOnlyList<string> SplitTags(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return Array.Empty<string>();
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value!.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();
    }
}
