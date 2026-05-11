using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker2.Pages;

public class IndexModel : PageModel
{
    private readonly ICardLibraryService _cardLibraryService;

    public IndexModel(ICardLibraryService cardLibraryService)
    {
        _cardLibraryService = cardLibraryService;
    }

    [BindProperty]
    public MealType? MealType { get; set; }

    [BindProperty]
    public bool CoinInserted { get; set; }

    public IReadOnlyList<MealType> MealTypes { get; } = Enum.GetValues<MealType>();

    public DrawOperationState OperationState { get; private set; } = DrawOperationState.Idle;

    public DrawResult? Result { get; private set; }

    public CardLibraryLoadResult? LibraryState { get; private set; }

    public string? StatusMessage { get; private set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public async Task OnGetAsync(MealType? mealType, CancellationToken cancellationToken)
    {
        MealType = mealType;
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            OperationState = DrawOperationState.Blocked;
            StatusMessage = LibraryState.UserMessage;
            return;
        }

        OperationState = DrawOperationStateTransitions.FromSelection(MealType);
        StatusMessage = OperationState == DrawOperationState.MealSelected
            ? "已選擇餐別，請投幣後拉桿。"
            : "請先選擇早餐、午餐或晚餐。";
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
            StatusMessage = "請先選擇早餐、午餐或晚餐。";
            ModelState.AddModelError(nameof(MealType), StatusMessage);
            return;
        }

        if (!CoinInserted)
        {
            OperationState = DrawOperationState.MealSelected;
            StatusMessage = "請先投幣再拉桿。";
            ModelState.AddModelError(nameof(CoinInserted), StatusMessage);
            return;
        }

        OperationState = DrawOperationState.Spinning;
        Result = await _cardLibraryService.DrawAsync(MealType.Value, cancellationToken);
        OperationState = Result.Succeeded ? DrawOperationState.Revealed : DrawOperationState.Blocked;
        StatusMessage = Result.UserMessage;
        if (!Result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, Result.UserMessage);
        }

    }

    public Task OnPostAsync(CancellationToken cancellationToken)
    {
        return OnPostDrawAsync(cancellationToken);
    }
}