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

    public IReadOnlyList<MealType> MealTypes { get; } = Enum.GetValues<MealType>();

    public SupportedLanguage CurrentLanguage => SupportedLanguage.FromCultureNameOrDefault(Thread.CurrentThread.CurrentUICulture.Name);

    public DrawOperationState OperationState { get; private set; } = DrawOperationState.Idle;

    public DrawResult? Result { get; private set; }

    public DrawStatisticsSummary Statistics { get; private set; } =
        new(0, Array.Empty<CardDrawStatistic>(), "Statistics.Empty");

    public CardLibraryLoadResult? LibraryState { get; private set; }

    public string? StatusMessage { get; private set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public async Task OnGetAsync(
        MealType? mealType,
        bool coinInserted,
        Guid? resultCardId,
        DrawMode? drawMode,
        Guid? drawOperationId,
        CancellationToken cancellationToken)
    {
        MealType = mealType;
        CoinInserted = coinInserted;
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
            RequestedLanguage = CurrentLanguage
        };
        Result = await _cardLibraryService.DrawAsync(operation, cancellationToken);
        Statistics = await _cardLibraryService.GetDrawStatisticsAsync(CurrentLanguage, cancellationToken);
        OperationState = Result.Succeeded ? DrawOperationState.Revealed : DrawOperationState.Blocked;
        StatusMessage = Result.Succeeded
            ? (Result.IsReplay ? _localizer["Home.Status.Replay"] : _localizer["Home.Status.DrawSuccess"])
            : Result.UserMessage;
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
                ModelState.AddModelError(string.Empty, Result.UserMessage);
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
            MealType);
    }
}
