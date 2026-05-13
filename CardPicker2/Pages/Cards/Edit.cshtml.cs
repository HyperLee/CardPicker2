using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker2.Pages.Cards;

public sealed class EditModel : PageModel
{
    private readonly ICardLibraryService _cardLibraryService;

    public EditModel(ICardLibraryService cardLibraryService)
    {
        _cardLibraryService = cardLibraryService;
    }

    [BindProperty]
    public MealCardInputModel Input { get; set; } = new();

    public CardLibraryLoadResult? LibraryState { get; private set; }

    public string? Message { get; private set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public bool MissingEnglishTranslation { get; private set; }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        await LoadForEditAsync(id, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            Message = LibraryState.UserMessage;
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _cardLibraryService.UpdateAsync(id, Input, cancellationToken);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = result.UserMessage;
            return Redirect($"/Cards/{id}");
        }

        if (result.Status == CardLibraryMutationStatus.NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
        }

        ModelState.AddModelError(string.Empty, result.UserMessage);
        Message = result.UserMessage;
        return Page();
    }

    private async Task LoadForEditAsync(Guid id, CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            Message = LibraryState.UserMessage;
            return;
        }

        var card = await _cardLibraryService.FindByIdAsync(id, cancellationToken);
        if (card is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            Message = "找不到餐點卡牌。";
            return;
        }

        Input = new MealCardInputModel
        {
            NameZhTw = card.GetContent(SupportedLanguage.ZhTw).Name,
            DescriptionZhTw = card.GetContent(SupportedLanguage.ZhTw).Description,
            NameEnUs = card.HasCompleteContent(SupportedLanguage.EnUs)
                ? card.GetContent(SupportedLanguage.EnUs).Name
                : null,
            DescriptionEnUs = card.HasCompleteContent(SupportedLanguage.EnUs)
                ? card.GetContent(SupportedLanguage.EnUs).Description
                : null,
            MealType = card.MealType,
        };
        MissingEnglishTranslation = !card.HasCompleteContent(SupportedLanguage.EnUs);
    }
}
