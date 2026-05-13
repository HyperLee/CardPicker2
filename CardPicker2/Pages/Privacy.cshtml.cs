using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker2.Pages;

public class PrivacyModel : PageModel
{
    public string Title { get; private set; } = string.Empty;

    public void OnGet()
    {
        Title = "Privacy";
    }
}
