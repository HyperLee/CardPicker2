using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace CardPicker2.Pages.Cards;

public sealed class DetailsModel : PageModel
{
    private readonly ICardLibraryService _cardLibraryService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DetailsModel(ICardLibraryService cardLibraryService, IStringLocalizer<SharedResource> localizer)
    {
        _cardLibraryService = cardLibraryService;
        _localizer = localizer;
    }

    public LocalizedMealCardView? Card { get; private set; }

    public CardLibraryLoadResult? LibraryState { get; private set; }

    public string? Message { get; private set; }

    public bool IsBlocked => LibraryState?.IsBlocked == true;

    public SupportedLanguage CurrentLanguage => SupportedLanguage.FromCultureNameOrDefault(Thread.CurrentThread.CurrentUICulture.Name);

    [BindProperty]
    public bool ConfirmDelete { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        await LoadCardAsync(id, cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            Message = LibraryState.UserMessage;
            return Page();
        }

        Card = await _cardLibraryService.FindLocalizedByIdAsync(id, CurrentLanguage, cancellationToken);
        if (Card is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            Message = _localizer["Details.NotFound"];
            return Page();
        }

        if (!ConfirmDelete)
        {
            Message = _localizer["Details.ConfirmRequired"];
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

    private async Task LoadCardAsync(Guid id, CancellationToken cancellationToken)
    {
        LibraryState = await _cardLibraryService.LoadAsync(cancellationToken);
        if (LibraryState.IsBlocked)
        {
            Message = LibraryState.UserMessage;
            return;
        }

        Card = await _cardLibraryService.FindLocalizedByIdAsync(id, CurrentLanguage, cancellationToken);
        if (Card is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            Message = _localizer["Details.NotFound"];
        }
    }
}
