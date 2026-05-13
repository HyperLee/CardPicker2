using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker2.Pages.Cards;

public sealed class IndexModel : PageModel
{
    private readonly ICardLibraryService _cardLibraryService;

    public IndexModel(ICardLibraryService cardLibraryService)
    {
        _cardLibraryService = cardLibraryService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public MealType? MealType { get; set; }

    public IReadOnlyList<MealType> MealTypes { get; } = Enum.GetValues<MealType>();

    public SupportedLanguage CurrentLanguage => SupportedLanguage.FromCultureNameOrDefault(Thread.CurrentThread.CurrentUICulture.Name);

    public IReadOnlyList<LocalizedMealCardView> Cards { get; private set; } = Array.Empty<LocalizedMealCardView>();

    public CardLibraryLoadResult? LibraryState { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
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
            CurrentLanguage = CurrentLanguage
        }, cancellationToken);
    }
}
