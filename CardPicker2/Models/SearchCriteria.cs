namespace CardPicker2.Models;

/// <summary>
/// Represents optional filters for the meal-card list.
/// </summary>
public sealed class SearchCriteria
{
    /// <summary>
    /// Gets or sets an optional meal-name keyword.
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Gets or sets an optional meal type filter.
    /// </summary>
    public MealType? MealType { get; set; }

    /// <summary>
    /// Gets or sets the language used for visible-name search.
    /// </summary>
    public SupportedLanguage CurrentLanguage { get; set; } = SupportedLanguage.ZhTw;

    /// <summary>
    /// Gets the trimmed keyword, or <see langword="null"/> when empty.
    /// </summary>
    public string? NormalizedKeyword
    {
        get
        {
            var trimmed = Keyword?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }
}
