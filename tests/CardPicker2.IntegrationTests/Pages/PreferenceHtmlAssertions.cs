using System.Net;
using System.Text.RegularExpressions;

namespace CardPicker2.IntegrationTests.Pages;

public static partial class PreferenceHtmlAssertions
{
    public const string PreferenceControlsAttribute = "data-card-preference-controls";
    public const string PreferenceBadgeAttribute = "data-preference-badge";
    public const string FavoriteFilterField = "favoriteFilter";
    public const string DrawEligibilityFilterField = "drawEligibilityFilter";

    public static void AssertPreferenceControls(string html)
    {
        var decodedHtml = Decode(html);

        AssertContainsAny(decodedHtml, PreferenceControlsAttribute, "preference-control");
        AssertContainsField(decodedHtml, "CardId", "cardId");
        AssertContainsAny(decodedHtml, "TargetIsFavorite", "targetIsFavorite", "TargetIsExcludedFromDraw", "targetIsExcludedFromDraw");
    }

    public static void AssertPreferenceBadges(string html, params string[] expectedTexts)
    {
        var decodedHtml = Decode(html);

        AssertContainsAny(decodedHtml, PreferenceBadgeAttribute, "preference-badge");
        AssertVisibleText(decodedHtml, expectedTexts);
    }

    public static void AssertCardLibraryPreferenceFilters(string html)
    {
        var decodedHtml = Decode(html);

        AssertContainsField(decodedHtml, FavoriteFilterField, "FavoriteFilter");
        AssertContainsField(decodedHtml, DrawEligibilityFilterField, "DrawEligibilityFilter");
    }

    public static void AssertResultPreferenceActionState(
        string html,
        Guid expectedCardId,
        params string[] expectedTexts)
    {
        var decodedHtml = Decode(html);

        Assert.Contains($"data-result-card-id=\"{expectedCardId}\"", decodedHtml, StringComparison.OrdinalIgnoreCase);
        AssertPreferenceControls(decodedHtml);
        AssertVisibleText(decodedHtml, expectedTexts);
    }

    public static void AssertNoUntranslatedPreferenceKeys(string html)
    {
        var decodedHtml = Decode(html);

        Assert.DoesNotMatch(UntranslatedPreferenceKeyRegex(), decodedHtml);
    }

    private static string Decode(string html)
    {
        return WebUtility.HtmlDecode(html);
    }

    private static void AssertContainsField(string html, params string[] fieldNames)
    {
        var hasAnyField = fieldNames.Any(fieldName =>
            Regex.IsMatch(
                html,
                $@"\b(name|id)=""?{Regex.Escape(fieldName)}""?",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

        Assert.True(hasAnyField, $"Expected one of these fields to be rendered: {string.Join(", ", fieldNames)}.");
    }

    private static void AssertContainsAny(string html, params string[] expectedFragments)
    {
        var hasAnyFragment = expectedFragments.Any(fragment =>
            html.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        Assert.True(hasAnyFragment, $"Expected one of these fragments to be rendered: {string.Join(", ", expectedFragments)}.");
    }

    private static void AssertVisibleText(string html, params string[] expectedTexts)
    {
        foreach (var text in expectedTexts)
        {
            Assert.Contains(text, html, StringComparison.Ordinal);
        }
    }

    [GeneratedRegex(@"\b(Preference|Favorite|DrawEligibility|ExcludedFromDraw)\.[A-Za-z0-9_.-]+\b", RegexOptions.CultureInvariant)]
    private static partial Regex UntranslatedPreferenceKeyRegex();
}
