using System.Net.Http.Headers;

using Microsoft.AspNetCore.Localization;

namespace CardPicker2.IntegrationTests.Infrastructure;

public static class LanguageCookieTestExtensions
{
    public static readonly string CultureCookieName = CookieRequestCultureProvider.DefaultCookieName;

    public static void AddCultureCookie(this HttpClient client, string cultureName)
    {
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureName, cultureName));
        client.DefaultRequestHeaders.Add("Cookie", $"{CultureCookieName}={cookieValue}");
    }

    public static bool HasCultureCookie(this HttpResponseHeaders headers, string cultureName)
    {
        var expectedValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureName, cultureName));
        var encodedExpectedValue = Uri.EscapeDataString(expectedValue);

        return headers.TryGetValues("Set-Cookie", out var values) &&
            values.Any(value =>
                value.Contains(CultureCookieName, StringComparison.Ordinal) &&
                (value.Contains(expectedValue, StringComparison.Ordinal) ||
                    value.Contains(encodedExpectedValue, StringComparison.Ordinal)));
    }
}
