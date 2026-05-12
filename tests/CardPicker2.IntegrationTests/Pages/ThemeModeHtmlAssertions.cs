using System.Net;
using System.Text.RegularExpressions;

namespace CardPicker2.IntegrationTests.Pages;

public static partial class ThemeModeHtmlAssertions
{
    public const string SelectorAttribute = "data-theme-mode-selector";
    public const string StorageKey = "cardpicker.theme.mode";

    public static void AssertHomeThemeSelector(string html)
    {
        var decodedHtml = WebUtility.HtmlDecode(html);

        Assert.Contains("網站主題", decodedHtml);
        Assert.Contains("亮色模式", decodedHtml);
        Assert.Contains("暗黑模式", decodedHtml);
        Assert.Contains("跟隨系統", decodedHtml);
        Assert.Contains(SelectorAttribute, decodedHtml);
        Assert.Contains(StorageKey, decodedHtml);

        Assert.Contains("value=\"light\"", decodedHtml);
        Assert.Contains("value=\"dark\"", decodedHtml);
        Assert.Contains("value=\"system\"", decodedHtml);
    }

    public static void AssertNoThemeSelector(string html)
    {
        var decodedHtml = WebUtility.HtmlDecode(html);

        Assert.DoesNotContain(SelectorAttribute, decodedHtml);
        Assert.DoesNotContain("網站主題", decodedHtml);
        Assert.DoesNotMatch(ThemeModeInputRegex(), decodedHtml);
    }

    [GeneratedRegex("name=\"theme-mode\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ThemeModeInputRegex();
}