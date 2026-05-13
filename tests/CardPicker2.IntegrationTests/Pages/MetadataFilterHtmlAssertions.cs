using System.Net;
using System.Text.RegularExpressions;

namespace CardPicker2.IntegrationTests.Pages;

public static partial class MetadataFilterHtmlAssertions
{
    public const string FilterPanelAttribute = "data-metadata-filter-panel";
    public const string FilterSummaryAttribute = "data-filter-summary";
    public const string MetadataBadgeAttribute = "data-metadata-badge";
    public const string TagChipAttribute = "data-tag-chip";

    public static void AssertHomeFilterControls(string html)
    {
        var decodedHtml = Decode(html);

        Assert.Contains("drawMode", decodedHtml, StringComparison.OrdinalIgnoreCase);
        AssertContainsField(decodedHtml, "PriceRange", "priceRange");
        AssertContainsField(decodedHtml, "PreparationTimeRange", "preparationTimeRange");
        AssertContainsField(decodedHtml, "DietaryPreferences", "dietaryPreferences");
        AssertContainsField(decodedHtml, "MaxSpiceLevel", "maxSpiceLevel");
        AssertContainsField(decodedHtml, "Tags", "tags");
    }

    public static void AssertCardLibraryFilterControls(string html)
    {
        var decodedHtml = Decode(html);

        AssertContainsField(decodedHtml, "keyword");
        AssertContainsField(decodedHtml, "MealType", "mealType");
        AssertContainsField(decodedHtml, "PriceRange", "priceRange");
        AssertContainsField(decodedHtml, "PreparationTimeRange", "preparationTimeRange");
        AssertContainsField(decodedHtml, "DietaryPreferences", "dietaryPreferences");
        AssertContainsField(decodedHtml, "MaxSpiceLevel", "maxSpiceLevel");
        AssertContainsField(decodedHtml, "Tags", "tags");
    }

    public static void AssertCardFormMetadataFields(string html)
    {
        var decodedHtml = Decode(html);

        AssertContainsField(decodedHtml, "TagsInput");
        AssertContainsField(decodedHtml, "PriceRange");
        AssertContainsField(decodedHtml, "PreparationTimeRange");
        AssertContainsField(decodedHtml, "DietaryPreferences");
        AssertContainsField(decodedHtml, "SpiceLevel");
    }

    public static void AssertFilterSummary(string html, params string[] expectedTexts)
    {
        var decodedHtml = Decode(html);

        AssertContainsAny(decodedHtml, FilterSummaryAttribute, "active-filter", "filter-summary");
        AssertVisibleText(decodedHtml, expectedTexts);
    }

    public static void AssertMetadataBadges(string html, params string[] expectedTexts)
    {
        var decodedHtml = Decode(html);

        AssertContainsAny(decodedHtml, MetadataBadgeAttribute, "metadata-badge", "card-metadata");
        AssertVisibleText(decodedHtml, expectedTexts);
    }

    public static void AssertTagChips(string html, params string[] expectedTags)
    {
        var decodedHtml = Decode(html);

        AssertContainsAny(decodedHtml, TagChipAttribute, "tag-chip", "filter-chip");
        AssertVisibleText(decodedHtml, expectedTags);
    }

    public static void AssertLocalizedEmptyState(string html, string expectedEmptyStateText)
    {
        var decodedHtml = Decode(html);

        Assert.Contains(expectedEmptyStateText, decodedHtml, StringComparison.Ordinal);
    }

    public static void AssertDetailsMetadataSection(string html, params string[] expectedTexts)
    {
        var decodedHtml = Decode(html);

        AssertContainsAny(decodedHtml, "metadata-summary", "decision-metadata", MetadataBadgeAttribute);
        AssertVisibleText(decodedHtml, expectedTexts);
    }

    public static void AssertNoUntranslatedMetadataKeys(string html)
    {
        var decodedHtml = Decode(html);

        Assert.DoesNotMatch(UntranslatedMetadataKeyRegex(), decodedHtml);
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

    [GeneratedRegex(@"\b(Metadata|Filter|PriceRange|PreparationTimeRange|DietaryPreference|SpiceLevel)\.[A-Za-z0-9_.-]+\b", RegexOptions.CultureInvariant)]
    private static partial Regex UntranslatedMetadataKeyRegex();
}
