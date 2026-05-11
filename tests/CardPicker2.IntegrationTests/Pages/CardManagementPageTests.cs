using System.Net;
using System.Text.RegularExpressions;
using CardPicker2.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed partial class CardManagementPageTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create();
    private readonly WebApplicationFactory<Program> _factory;

    public CardManagementPageTests()
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
    public async Task CreatePost_WithValidInput_RedirectsToNewDetails()
    {
        var client = CreateClientWithoutRedirect();
        var token = await GetAntiForgeryTokenAsync(client, "/Cards/Create");

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Name"] = "測試咖哩",
            ["Input.MealType"] = "Dinner",
            ["Input.Description"] = "晚餐測試描述"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var details = WebUtility.HtmlDecode(await _factory.CreateClient().GetStringAsync(response.Headers.Location!.ToString()));
        Assert.Contains("測試咖哩", details);
    }

    [Fact]
    public async Task CreatePost_WithMissingFields_ShowsFieldErrors()
    {
        var client = _factory.CreateClient();
        var token = await GetAntiForgeryTokenAsync(client, "/Cards/Create");

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("請輸入餐點名稱。", html);
    }

    [Fact]
    public async Task CreatePost_WithDuplicate_ShowsDuplicateError()
    {
        var client = _factory.CreateClient();
        var token = await GetAntiForgeryTokenAsync(client, "/Cards/Create");

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Name"] = "鮪魚蛋餅",
            ["Input.MealType"] = "Breakfast",
            ["Input.Description"] = "附近早餐店的鮪魚蛋餅，加一杯無糖豆漿。"
        }));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains("已有相同餐點名稱、餐別與描述的卡牌。", html);
    }

    [Fact]
    public async Task EditPost_WithValidInput_UpdatesDetails()
    {
        var client = CreateClientWithoutRedirect();
        var id = "11111111-1111-1111-1111-111111111111";
        var token = await GetAntiForgeryTokenAsync(client, $"/Cards/Edit/{id}");

        var response = await client.PostAsync($"/Cards/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Name"] = "更新早餐",
            ["Input.MealType"] = "Breakfast",
            ["Input.Description"] = "更新早餐描述"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var details = WebUtility.HtmlDecode(await _factory.CreateClient().GetStringAsync($"/Cards/{id}"));
        Assert.Contains("更新早餐", details);
        Assert.Contains("更新早餐描述", details);
    }

    [Fact]
    public async Task DeletePost_WithConfirmation_RemovesCard()
    {
        var client = CreateClientWithoutRedirect();
        var id = "11111111-1111-1111-1111-111111111111";
        var token = await GetAntiForgeryTokenAsync(client, $"/Cards/{id}");

        var response = await client.PostAsync($"/Cards/{id}?handler=Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ConfirmDelete"] = "true"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var missing = await _factory.CreateClient().GetAsync($"/Cards/{id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task CreatePost_WithoutAntiForgery_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Name"] = "測試",
            ["Input.MealType"] = "Lunch",
            ["Input.Description"] = "測試"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGet_WhenLibraryBlocked_DisablesForm()
    {
        await File.WriteAllTextAsync(_library.FilePath, "{");
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Cards/Create"));

        Assert.Contains("卡牌庫檔案", html);
        Assert.Contains("disabled", html);
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
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-management-web-tests-").FullName);
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
