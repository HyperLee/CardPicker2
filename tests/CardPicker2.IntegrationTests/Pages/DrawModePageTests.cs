using System.Net;
using System.Text.RegularExpressions;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed partial class DrawModePageTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task GetHome_ShowsModeControlsAndHiddenOperationId()
    {
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("drawMode", html);
        Assert.Contains("drawOperationId", html);
        Assert.Contains("正常模式", html);
        Assert.Contains("隨機模式", html);
        Assert.Matches(DrawOperationIdRegex(), html);
    }

    [Fact]
    public async Task PostDraw_WithNormalMode_RevealsSelectedMealTypeResult()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetAntiForgeryTokenAsync(client);
        var operationId = Guid.NewGuid().ToString();

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DrawMode"] = nameof(DrawMode.Normal),
            ["MealType"] = nameof(MealType.Breakfast),
            ["CoinInserted"] = "true",
            ["DrawOperationId"] = operationId
        }));

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("抽卡結果", html);
        Assert.Contains("正常模式", html);
        Assert.Contains("早餐", html);
    }

    [Fact]
    public async Task PostDraw_WithRandomMode_DoesNotRequireMealType()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetAntiForgeryTokenAsync(client);

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DrawMode"] = nameof(DrawMode.Random),
            ["CoinInserted"] = "true",
            ["DrawOperationId"] = Guid.NewGuid().ToString()
        }));

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("抽卡結果", html);
        Assert.Contains("隨機模式", html);
    }

    [Fact]
    public async Task PostDraw_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["DrawMode"] = nameof(DrawMode.Random),
            ["CoinInserted"] = "true",
            ["DrawOperationId"] = Guid.NewGuid().ToString()
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHome_WhenLibraryIsBlocked_DisablesDrawModeForm()
    {
        await _factory.WriteLibraryJsonAsync("{");
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

        Assert.Contains("卡牌庫檔案", html);
        Assert.Contains("disabled", html);
        Assert.DoesNotContain("name=\"DrawOperationId\" value=\"00000000-0000-0000-0000-000000000000\"", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [GeneratedRegex("name=\"drawOperationId\"[^>]*value=\"(?<operationId>[0-9a-fA-F-]{36})\"")]
    private static partial Regex DrawOperationIdRegex();
}
