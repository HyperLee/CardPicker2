using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed partial class CardLibraryLocalizationPersistenceTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-localized-persistence-tests-");
    private readonly WebApplicationFactory<Program> _factory;

    public CardLibraryLocalizationPersistenceTests()
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
    public async Task EditPost_FromSchemaV1Fallback_WritesCurrentSchemaWithEnglishContent()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        await File.WriteAllTextAsync(_library.FilePath, JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            cards = new[]
            {
                new
                {
                    id,
                    name = "早餐卡",
                    mealType = "Breakfast",
                    description = "早餐描述"
                }
            }
        }));
        var client = CreateClientWithoutRedirect();
        client.AddCultureCookie("en-US");
        var token = await GetAntiForgeryTokenAsync(client, $"/Cards/Edit/{id}");

        var response = await client.PostAsync($"/Cards/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.NameZhTw"] = "早餐卡",
            ["Input.DescriptionZhTw"] = "早餐描述",
            ["Input.NameEnUs"] = "Breakfast Card",
            ["Input.DescriptionEnUs"] = "Breakfast description",
            ["Input.MealType"] = "Breakfast"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var json = await File.ReadAllTextAsync(_library.FilePath);

        Assert.Contains($"\"schemaVersion\": {CardPicker2.Models.CardLibraryDocument.CurrentSchemaVersion}", json);
        Assert.Contains("\"drawHistory\": []", json);
        Assert.Contains("Breakfast Card", json);
        Assert.Contains("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", json);
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
