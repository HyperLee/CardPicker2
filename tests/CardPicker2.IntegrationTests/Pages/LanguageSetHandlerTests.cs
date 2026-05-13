using System.Net;
using System.Text.RegularExpressions;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed partial class LanguageSetHandlerTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-language-handler-tests-");
    private readonly WebApplicationFactory<Program> _factory;

    public LanguageSetHandlerTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    _library.Configure(services);
                });
            });
    }

    [Fact]
    public async Task PostSet_WithEnglishCulture_WritesCultureCookieAndRedirectsToLocalReturnUrl()
    {
        var client = CreateClientWithoutRedirect();
        var token = await GetAntiForgeryTokenAsync(client, "/");

        var response = await client.PostAsync("/Language?handler=Set", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["culture"] = "en-US",
            ["returnUrl"] = "/?mealType=Breakfast"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/?mealType=Breakfast", response.Headers.Location?.ToString());
        Assert.True(response.Headers.HasCultureCookie("en-US"));
    }

    [Fact]
    public async Task PostSet_WithUnsafeReturnUrl_RedirectsHome()
    {
        var client = CreateClientWithoutRedirect();
        var token = await GetAntiForgeryTokenAsync(client, "/");

        var response = await client.PostAsync("/Language?handler=Set", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["culture"] = "en-US",
            ["returnUrl"] = "https://example.com"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PostSet_WithUnsupportedCulture_FallsBackToTraditionalChineseCookie()
    {
        var client = CreateClientWithoutRedirect();
        var token = await GetAntiForgeryTokenAsync(client, "/");

        var response = await client.PostAsync("/Language?handler=Set", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["culture"] = "fr-FR",
            ["returnUrl"] = "/"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(response.Headers.HasCultureCookie("zh-TW"));
    }

    [Fact]
    public async Task PostSet_WithoutAntiForgery_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/Language?handler=Set", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["culture"] = "en-US",
            ["returnUrl"] = "/"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _library.Dispose();
    }

    private HttpClient CreateClientWithoutRedirect()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string requestUri)
    {
        var html = await client.GetStringAsync(requestUri);
        var match = AntiForgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Anti-forgery token should be present.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiForgeryTokenRegex();
}
