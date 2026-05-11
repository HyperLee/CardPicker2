using System.Net;
using System.Text.RegularExpressions;

using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed partial class DrawPageTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create();
    private readonly WebApplicationFactory<Program> _factory;

    public DrawPageTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.Configure<CardLibraryOptions>(options =>
                    {
                        options.LibraryFilePath = _library.FilePath;
                    });
                });
            });
    }

    [Fact]
    public async Task GetHome_ShowsSlotMachineDrawControls()
    {
        var client = _factory.CreateClient();

        var html = await GetDecodedHtmlAsync(client, "/");

        Assert.Contains("選擇餐別", html);
        Assert.Contains("投幣", html);
        Assert.Contains("拉桿", html);
        Assert.Contains("slot-reel", html);
    }

    [Fact]
    public async Task PostDraw_WithoutMealType_ShowsValidationMessage()
    {
        var client = _factory.CreateClient();
        var token = await GetAntiForgeryTokenAsync(client);

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["CoinInserted"] = "true"
        }));

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("請先選擇早餐、午餐或晚餐。", html);
    }

    [Fact]
    public async Task PostDraw_WithoutCoin_ShowsValidationMessage()
    {
        var client = _factory.CreateClient();
        var token = await GetAntiForgeryTokenAsync(client);

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["MealType"] = "Breakfast"
        }));

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("請先投幣再拉桿。", html);
    }

    [Fact]
    public async Task PostDraw_WithValidInput_RevealsBreakfastCard()
    {
        var client = _factory.CreateClient();
        var token = await GetAntiForgeryTokenAsync(client);

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["MealType"] = "Breakfast",
            ["CoinInserted"] = "true"
        }));

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("抽卡結果", html);
        Assert.Contains("早餐", html);
        Assert.Matches("鮪魚蛋餅|鹹粥小菜|花生厚片", html);
    }

    [Fact]
    public async Task PostDraw_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["MealType"] = "Breakfast",
            ["CoinInserted"] = "true"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHome_WhenLibraryIsBlocked_DisablesDrawAndShowsRecoveryMessage()
    {
        await File.WriteAllTextAsync(_library.FilePath, "{");
        var client = _factory.CreateClient();

        var html = await GetDecodedHtmlAsync(client, "/");

        Assert.Contains("卡牌庫檔案", html);
        Assert.Contains("disabled", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _library.Dispose();
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/");
        var match = AntiForgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Anti-forgery token should be present.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static async Task<string> GetDecodedHtmlAsync(HttpClient client, string requestUri)
    {
        return WebUtility.HtmlDecode(await client.GetStringAsync(requestUri));
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiForgeryTokenRegex();

    private sealed class TempCardLibrary : IDisposable
    {
        private TempCardLibrary(string directoryPath)
        {
            DirectoryPath = directoryPath;
            FilePath = Path.Combine(directoryPath, "cards.json");
        }

        public string DirectoryPath { get; }

        public string FilePath { get; }

        public static TempCardLibrary Create()
        {
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-web-tests-").FullName);
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}