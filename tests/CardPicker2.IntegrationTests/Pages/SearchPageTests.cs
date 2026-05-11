using System.Net;
using CardPicker2.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class SearchPageTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create();
    private readonly WebApplicationFactory<Program> _factory;

    public SearchPageTests()
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
    public async Task GetCards_WithoutQuery_ShowsSeedCards()
    {
        var client = _factory.CreateClient();

        var html = await GetDecodedHtmlAsync(client, "/Cards");

        Assert.Contains("餐點卡牌庫", html);
        Assert.Contains("鮪魚蛋餅", html);
        Assert.Contains("清燉牛肉麵", html);
        Assert.Contains("壽喜燒鍋", html);
    }

    [Fact]
    public async Task GetCards_WithKeywordAndMealType_FiltersResults()
    {
        var client = _factory.CreateClient();

        var html = await GetDecodedHtmlAsync(client, "/Cards?keyword=牛肉&mealType=Lunch");

        Assert.Contains("清燉牛肉麵", html);
        Assert.DoesNotContain("鮪魚蛋餅", html);
        Assert.DoesNotContain("壽喜燒鍋", html);
    }

    [Fact]
    public async Task GetCards_WithNoMatches_ShowsEmptyMessage()
    {
        var client = _factory.CreateClient();

        var html = await GetDecodedHtmlAsync(client, "/Cards?keyword=不存在");

        Assert.Contains("查無符合條件的餐點卡牌。", html);
    }

    [Fact]
    public async Task GetCardDetails_WithExistingId_ShowsFullDescription()
    {
        var client = _factory.CreateClient();

        var html = await GetDecodedHtmlAsync(client, "/Cards/11111111-1111-1111-1111-111111111111");

        Assert.Contains("鮪魚蛋餅", html);
        Assert.Contains("附近早餐店的鮪魚蛋餅", html);
        Assert.Contains("早餐", html);
    }

    [Fact]
    public async Task GetCardDetails_WithMissingId_ReturnsNotFoundPage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Cards/99999999-9999-9999-9999-999999999999");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("找不到餐點卡牌。", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _library.Dispose();
    }

    private static async Task<string> GetDecodedHtmlAsync(HttpClient client, string requestUri)
    {
        return WebUtility.HtmlDecode(await client.GetStringAsync(requestUri));
    }

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
            return new TempCardLibrary(Directory.CreateTempSubdirectory("cardpicker-search-web-tests-").FullName);
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
