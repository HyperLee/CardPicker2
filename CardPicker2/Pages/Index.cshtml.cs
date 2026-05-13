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

    public IReadOnlyList<MealType> MealTypes { get; } = Enum.GetValues<MealType>();

    public SupportedLanguage CurrentLanguage => SupportedLanguage.FromCultureNameOrDefault(Thread.CurrentThread.CurrentUICulture.Name);

    public DrawOperationState OperationState { get; private set; } = DrawOperationState.Idle;

    public DrawResult? Result { get; private set; }

    public CardLibraryLoadResult? LibraryState { get; private set; }

    public string? StatusMessage { get; private set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public async Task OnGetAsync(MealType? mealType, bool coinInserted, Guid? resultCardId, CancellationToken cancellationToken)
    {
        MealType = mealType;
        CoinInserted = coinInserted;
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            OperationState = DrawOperationState.Blocked;
            StatusMessage = LibraryState.UserMessage;
            return;
        }

        if (resultCardId is Guid cardId)
        {
            await RestoreResultAsync(cardId, cancellationToken);
            return;
        }

        OperationState = DrawOperationStateTransitions.FromSelection(MealType);
        StatusMessage = OperationState == DrawOperationState.MealSelected
            ? _localizer["Home.Status.MealSelected"]
            : _localizer["Home.Status.ChooseMeal"];
    }

    public async Task OnPostDrawAsync(CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            OperationState = DrawOperationState.Blocked;
            StatusMessage = LibraryState.UserMessage;
            return;
        }

        if (MealType is null || !Enum.IsDefined(typeof(MealType), MealType.Value))
        {
            OperationState = DrawOperationState.Blocked;
            StatusMessage = _localizer["Home.Status.ChooseMeal"];
            ModelState.AddModelError(nameof(MealType), StatusMessage);
            return;
        }

        if (!CoinInserted)
        {
            OperationState = DrawOperationState.MealSelected;
            StatusMessage = _localizer["Home.Status.CoinRequired"];
            ModelState.AddModelError(nameof(CoinInserted), StatusMessage);
            return;
        }

        OperationState = DrawOperationState.Spinning;
        Result = await _cardLibraryService.DrawAsync(MealType.Value, CurrentLanguage, cancellationToken);
        OperationState = Result.Succeeded ? DrawOperationState.Revealed : DrawOperationState.Blocked;
        StatusMessage = Result.Succeeded ? _localizer["Home.Status.DrawSuccess"] : Result.UserMessage;
        if (!Result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, Result.UserMessage);
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
        Result = new DrawResult(
            true,
            MealType.Value,
            localizedCard.CardId,
            localizedCard.DisplayName,
            localizedCard.MealType,
            localizedCard.DisplayDescription,
            StatusMessage,
            localizedCard,
            "Draw.Restored");
    }
}
