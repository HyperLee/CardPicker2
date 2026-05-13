using System.Net;
using System.Text.RegularExpressions;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed partial class DrawHistoryPersistenceTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-history-persistence-tests-");

    [Fact]
    public async Task SuccessfulDraw_PersistsHistoryAndStatisticsAcrossFactoryRebuild()
    {
        using (var firstFactory = CreateFactory())
        {
            var client = firstFactory.CreateClient();
            var token = await GetAntiForgeryTokenAsync(client);
            var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["drawMode"] = nameof(DrawMode.Random),
                ["coinInserted"] = "true",
                ["drawOperationId"] = Guid.NewGuid().ToString()
            }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var secondFactory = CreateFactory();
        var secondClient = secondFactory.CreateClient();
        var html = WebUtility.HtmlDecode(await secondClient.GetStringAsync("/"));

        Assert.Contains("總成功抽取次數", html);
        Assert.Contains(">1<", html);
        Assert.Contains("100%", html);
    }

    public void Dispose()
    {
        _library.Dispose();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
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

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/");
        var match = AntiForgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Anti-forgery token should be present.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiForgeryTokenRegex();
}
