using System.Net;
using System.Text.RegularExpressions;

namespace CardPicker2.IntegrationTests.Pages;

public static partial class RotationCooldownHtmlAssertions
{
    public const string CooldownPanelAttribute = "data-rotation-cooldown-panel";
    public const string CooldownSummaryAttribute = "data-rotation-summary";
    public const string RotationEmptyAttribute = "data-rotation-empty-alert";

    public static void AssertCooldownControls(string html)
    {
        var decodedHtml = Decode(html);

        AssertContainsAny(decodedHtml, CooldownPanelAttribute, "rotation-cooldown", "cooldown-control");
        AssertContainsField(decodedHtml, "AvoidRecentRepeats", "avoidRecentRepeats");
        AssertContainsField(decodedHtml, "RecentDrawCount", "recentDrawCount");
    }

    public static void AssertDefaultCooldownState(string html)
    {
        var decodedHtml = Decode(html);

        Assert.Contains("checked", decodedHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(RecentDrawCountValueRegex(), decodedHtml);
    }

    public static void AssertRotationSuccessSummary(string html, params string[] expectedTexts)
    {
        var decodedHtml = Decode(html);

        AssertContainsAny(decodedHtml, CooldownSummaryAttribute, "rotation-summary", "cooldown-summary");
        AssertVisibleText(decodedHtml, expectedTexts);
    }

    public static void AssertRotationEmptyAlert(string html, params string[] expectedTexts)
    {
        var decodedHtml = Decode(html);

        AssertContainsAny(decodedHtml, RotationEmptyAttribute, "rotation-empty", "cooldown-empty");
        AssertVisibleText(decodedHtml, expectedTexts);
    }

    public static void AssertOldHistoryMissingSnapshotFallback(string html, string expectedText)
    {
        var decodedHtml = Decode(html);

        Assert.Contains(expectedText, decodedHtml, StringComparison.Ordinal);
    }

    public static void AssertLocalizedValidation(string html, string expectedText)
    {
        var decodedHtml = Decode(html);

        Assert.Contains(expectedText, decodedHtml, StringComparison.Ordinal);
    }

    public static void AssertNoUntranslatedRotationKeys(string html)
    {
        var decodedHtml = Decode(html);

        Assert.DoesNotMatch(UntranslatedRotationKeyRegex(), decodedHtml);
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

    [GeneratedRegex(@"\b(name|id)=""?(RecentDrawCount|recentDrawCount)""?[^>]*\bvalue=""?3""?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RecentDrawCountValueRegex();

    [GeneratedRegex(@"\b(Rotation|Cooldown)\.[A-Za-z0-9_.-]+\b", RegexOptions.CultureInvariant)]
    private static partial Regex UntranslatedRotationKeyRegex();
}
