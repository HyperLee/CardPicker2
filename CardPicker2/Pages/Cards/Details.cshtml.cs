using CardPicker2.Models;
using CardPicker2.Services;
using Microsoft.AspNetCore.Mvc;
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

    [BindProperty]
    public bool ConfirmDelete { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

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

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            Message = LibraryState.UserMessage;
            return Page();
        }

        Card = await _cardLibraryService.FindByIdAsync(id, cancellationToken);
        if (Card is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            Message = "找不到餐點卡牌。";
            return Page();
        }

        if (!ConfirmDelete)
        {
            Message = "請先確認刪除意圖。";
            ModelState.AddModelError(nameof(ConfirmDelete), Message);
            return Page();
        }

        var result = await _cardLibraryService.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = result.UserMessage;
            return RedirectToPage("/Cards/Index");
        }

        Message = result.UserMessage;
        ModelState.AddModelError(string.Empty, result.UserMessage);
        return Page();
    }
}
