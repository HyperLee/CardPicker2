using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed partial class DeletedCardStatisticsPageTests : IDisposable
{
    private static readonly Guid DeletedCandidateId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ActiveCandidateId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task DeletingCardWithHistory_KeepsDeletedStatisticsRow()
    {
        await _factory.WriteLibraryDocumentAsync(CreateDocument(), JsonOptions);
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var token = await GetAntiForgeryTokenAsync(client, $"/Cards/{DeletedCandidateId}");

        var response = await client.PostAsync($"/Cards/{DeletedCandidateId}?handler=Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ConfirmDelete"] = "true"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var html = WebUtility.HtmlDecode(await _factory.CreateClient().GetStringAsync("/"));
        Assert.Contains("歷史早餐", html);
        Assert.Contains("已刪除", html);
        Assert.Contains("100%", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static CardLibraryDocument CreateDocument()
    {
        return new CardLibraryDocument
        {
            SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            Cards = new[]
            {
                CreateCard(DeletedCandidateId, MealType.Breakfast, "歷史早餐", "History Breakfast"),
                CreateCard(ActiveCandidateId, MealType.Lunch, "有效午餐", "Active Lunch")
            },
            DrawHistory = new[]
            {
                new DrawHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    OperationId = Guid.NewGuid(),
                    DrawMode = DrawMode.Normal,
                    CardId = DeletedCandidateId,
                    MealTypeAtDraw = MealType.Breakfast,
                    SucceededAtUtc = DateTimeOffset.UtcNow
                }
            }
        };
    }

    private static MealCard CreateCard(Guid id, MealType mealType, string zhTwName, string enUsName)
    {
        return new MealCard(
            id,
            mealType,
            new Dictionary<string, MealCardLocalizedContent>
            {
                [SupportedLanguage.ZhTw.CultureName] = new(zhTwName, $"{zhTwName} 描述"),
                [SupportedLanguage.EnUs.CultureName] = new(enUsName, $"{enUsName} description")
            });
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
