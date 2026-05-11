using CardPicker2.Models;
using CardPicker2.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker2.Pages.Cards;

public sealed class DetailsModel : PageModel
{
    private readonly ICardLibraryService _cardLibraryService;

    public DetailsModel(ICardLibraryService cardLibraryService)
    {
        _cardLibraryService = cardLibraryService;
    }

    public MealCard? Card { get; private set; }

    public CardLibraryLoadResult? LibraryState { get; private set; }

    public string? Message { get; private set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            Message = LibraryState.UserMessage;
            return;
        }

        Card = await _cardLibraryService.FindByIdAsync(id, cancellationToken);
        if (Card is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            Message = "找不到餐點卡牌。";
        }
    }
}
