using CardPicker2.Models;
using CardPicker2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker2.Pages.Cards;

public sealed class CreateModel : PageModel
{
    private readonly ICardLibraryService _cardLibraryService;

    public CreateModel(ICardLibraryService cardLibraryService)
    {
        _cardLibraryService = cardLibraryService;
    }

    [BindProperty]
    public MealCardInputModel Input { get; set; } = new();

    public CardLibraryLoadResult? LibraryState { get; private set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _cardLibraryService.CreateAsync(Input, cancellationToken);
        if (result.Succeeded && result.Card is not null)
        {
            TempData["StatusMessage"] = result.UserMessage;
            return Redirect($"/Cards/{result.Card.Id}");
        }

        ModelState.AddModelError(string.Empty, result.UserMessage);
        return Page();
    }
}
