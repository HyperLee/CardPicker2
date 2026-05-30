using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace CardPicker2.Pages.Cards;

public sealed class IndexModel : PageModel
{
    private readonly ICardLibraryService _cardLibraryService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(ICardLibraryService cardLibraryService, IStringLocalizer<SharedResource> localizer)
    {
        _cardLibraryService = cardLibraryService;
        _localizer = localizer;
    }

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public MealType? MealType { get; set; }

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

    [BindProperty(SupportsGet = true)]
    public FavoriteFilter FavoriteFilter { get; set; } = FavoriteFilter.All;

    [BindProperty(SupportsGet = true)]
    public DrawEligibilityFilter DrawEligibilityFilter { get; set; } = DrawEligibilityFilter.All;

    public IReadOnlyList<MealType> MealTypes { get; } = Enum.GetValues<MealType>();

    public IReadOnlyList<PriceRange> PriceRangeOptions { get; } = Enum.GetValues<PriceRange>();

    public IReadOnlyList<PreparationTimeRange> PreparationTimeRangeOptions { get; } = Enum.GetValues<PreparationTimeRange>();

    public IReadOnlyList<DietaryPreference> DietaryPreferenceOptions { get; } = Enum.GetValues<DietaryPreference>();

    public IReadOnlyList<SpiceLevel> SpiceLevelOptions { get; } = Enum.GetValues<SpiceLevel>();

    public IReadOnlyList<FavoriteFilter> FavoriteFilterOptions { get; } = Enum.GetValues<FavoriteFilter>();

    public IReadOnlyList<DrawEligibilityFilter> DrawEligibilityFilterOptions { get; } = Enum.GetValues<DrawEligibilityFilter>();

    public SupportedLanguage CurrentLanguage => SupportedLanguage.FromCultureNameOrDefault(Thread.CurrentThread.CurrentUICulture.Name);

    public IReadOnlyList<LocalizedMealCardView> Cards { get; private set; } = Array.Empty<LocalizedMealCardView>();

    public CardLibraryLoadResult? LibraryState { get; private set; }

    public FilterSummary CurrentFilterSummary { get; private set; } = FilterSummary.Empty;

    public string EmptyStateMessage => CurrentFilterSummary.IsEmpty
        ? _localizer["Cards.Empty"]
        : _localizer["Metadata.Filter.EmptySearch"];

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var filters = BuildCriteria();
        CurrentFilterSummary = CreateLocalizedFilterSummary(filters);
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            Cards = Array.Empty<LocalizedMealCardView>();
            return;
        }

        Cards = await _cardLibraryService.SearchLocalizedAsync(new SearchCriteria
        {
            Keyword = Keyword,
            MealType = MealType,
            CurrentLanguage = CurrentLanguage,
            Filters = filters,
            Preferences = BuildPreferenceCriteria()
        }, cancellationToken);
    }

    public async Task<IActionResult> OnPostPreferenceAsync(
        CardPreferenceUpdateInputModel input,
        CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            TempData["StatusMessage"] = LibraryState.UserMessage;
            return RedirectToPage("/Cards/Index");
        }

        var result = await _cardLibraryService.SetPreferenceAsync(input, cancellationToken);
        TempData["StatusMessage"] = LocalizePreferenceResult(result);
        return RedirectToPage("/Cards/Index");
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

    public string DisplayFavoriteFilter(FavoriteFilter value)
    {
        return _localizer[$"Preference.Filter.Favorite.{value}"];
    }

    public string DisplayDrawEligibilityFilter(DrawEligibilityFilter value)
    {
        return _localizer[$"Preference.Filter.DrawEligibility.{value}"];
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

    private CardPreferenceCriteria BuildPreferenceCriteria()
    {
        return new CardPreferenceCriteria
        {
            FavoriteFilter = FavoriteFilter,
            DrawEligibilityFilter = DrawEligibilityFilter
        }.Normalize();
    }

    private FilterSummary CreateLocalizedFilterSummary(CardFilterCriteria criteria)
    {
        var normalized = criteria.Normalize();
        var items = new List<string>();
        if (normalized.MealType is MealType mealType)
        {
            items.Add(mealType.ToDisplayName(CurrentLanguage));
        }

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
        var preferenceCriteria = BuildPreferenceCriteria();
        if (preferenceCriteria.FavoriteFilter != FavoriteFilter.All)
        {
            items.Add(DisplayFavoriteFilter(preferenceCriteria.FavoriteFilter));
        }

        if (preferenceCriteria.DrawEligibilityFilter != DrawEligibilityFilter.All)
        {
            items.Add(DisplayDrawEligibilityFilter(preferenceCriteria.DrawEligibilityFilter));
        }

        return items.Count == 0 ? FilterSummary.Empty : new FilterSummary(items);
    }

    private string LocalizePreferenceResult(PreferenceMutationResult result)
    {
        return result.MessageKey switch
        {
            "Preference.Update.Succeeded" => _localizer["Preference.Update.Succeeded"],
            "Preference.Update.NotFound" => _localizer["Preference.Update.NotFound"],
            "Preference.Update.Deleted" => _localizer["Preference.Update.Deleted"],
            "Preference.Validation.InvalidTarget" => _localizer["Preference.Validation.InvalidTarget"],
            "Preference.Update.WriteFailed" => _localizer["Preference.Update.WriteFailed"],
            _ => result.UserMessage
        };
    }

    private static IReadOnlyList<string> SplitTags(IEnumerable<string?>? values)
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
