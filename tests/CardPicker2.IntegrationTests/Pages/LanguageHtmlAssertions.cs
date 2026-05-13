using System.Net;
using System.Text.RegularExpressions;

namespace CardPicker2.IntegrationTests.Pages;

public static partial class LanguageHtmlAssertions
{
    public const string SwitcherAttribute = "data-language-switcher";

    public static void AssertHtmlLanguage(string html, string expectedLang)
    {
        var decodedHtml = WebUtility.HtmlDecode(html);

        Assert.Contains($"<html lang=\"{expectedLang}\"", decodedHtml, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertLanguageSwitcher(string html)
    {
        var decodedHtml = WebUtility.HtmlDecode(html);

        Assert.Contains(SwitcherAttribute, decodedHtml);
        Assert.Contains("name=\"culture\"", decodedHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"returnUrl\"", decodedHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(AntiForgeryInputRegex(), decodedHtml);
    }

    public static void AssertVisibleText(string html, params string[] expectedTexts)
    {
        var decodedHtml = WebUtility.HtmlDecode(html);

        foreach (var text in expectedTexts)
        {
            Assert.Contains(text, decodedHtml, StringComparison.Ordinal);
        }
    }

    public static void AssertNoVisibleText(string html, params string[] unexpectedTexts)
    {
        var decodedHtml = WebUtility.HtmlDecode(html);

        foreach (var text in unexpectedTexts)
        {
            Assert.DoesNotContain(text, decodedHtml, StringComparison.Ordinal);
        }
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AntiForgeryInputRegex();
}
