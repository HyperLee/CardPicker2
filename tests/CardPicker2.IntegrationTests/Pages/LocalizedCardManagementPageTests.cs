using System.Net;
using System.Text.RegularExpressions;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed partial class LocalizedCardManagementPageTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-localized-management-tests-");
    private readonly WebApplicationFactory<Program> _factory;

    public LocalizedCardManagementPageTests()
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
    public async Task CreateGet_WithEnglishCookie_RendersBilingualFormLabels()
    {
        var client = _factory.CreateClient();
        client.AddCultureCookie("en-US");

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Cards/Create"));

        Assert.Contains("Traditional Chinese meal name", html);
        Assert.Contains("English meal name", html);
        Assert.Contains("Add card", html);
    }

    [Fact]
    public async Task CreatePost_WithCompleteBilingualInput_PersistsAndShowsEnglishDetails()
    {
        var client = CreateClientWithoutRedirect();
        client.AddCultureCookie("en-US");
        var token = await GetAntiForgeryTokenAsync(client, "/Cards/Create");

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.NameZhTw"] = "測試咖哩",
            ["Input.DescriptionZhTw"] = "晚餐測試描述",
            ["Input.NameEnUs"] = "Test Curry",
            ["Input.DescriptionEnUs"] = "Dinner test description",
            ["Input.MealType"] = "Dinner"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var followClient = _factory.CreateClient();
        followClient.AddCultureCookie("en-US");
        var html = WebUtility.HtmlDecode(await followClient.GetStringAsync(response.Headers.Location!.ToString()));

        Assert.Contains("Test Curry", html);
        Assert.Contains("Dinner test description", html);
    }

    [Fact]
    public async Task CreatePost_MissingEnglishName_ShowsLocalizedValidationAndDoesNotSave()
    {
        var client = _factory.CreateClient();
        client.AddCultureCookie("en-US");
        var token = await GetAntiForgeryTokenAsync(client, "/Cards/Create");

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.NameZhTw"] = "測試咖哩",
            ["Input.DescriptionZhTw"] = "晚餐測試描述",
            ["Input.DescriptionEnUs"] = "Dinner test description",
            ["Input.MealType"] = "Dinner"
        }));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Enter the English meal name.", html);
        Assert.DoesNotContain("Test Curry", html);
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
