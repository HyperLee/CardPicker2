using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class RotationCooldownReplayTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task PostDraw_ReplayOfLegacyHistoryMissingSnapshot_ShowsMissingSnapshotMessage()
    {
        var operationId = Guid.Parse("99999999-9999-9999-9999-999999999991");
        await _factory.WriteLibraryDocumentAsync(DocumentWithLegacyHistory(operationId), JsonOptions);
        var client = _factory.CreateClient();
        using var content = await _factory.CreateSameOperationReplayContentAsync(
            client,
            operationId,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch),
            avoidRecentRepeats: true,
            recentDrawCount: "10");

        var response = await client.PostAsync("/?handler=Draw", content);

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("已重顯同一次抽卡結果", html);
        RotationCooldownHtmlAssertions.AssertOldHistoryMissingSnapshotFallback(html, "此筆舊紀錄未保存輪替摘要");
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static object DocumentWithLegacyHistory(Guid operationId)
    {
        var cardId = Guid.Parse("22222222-2222-2222-2222-222222222221");
        return new
        {
            schemaVersion = CardLibraryDocument.CurrentSchemaVersion,
            cards = new[]
            {
                new
                {
                    id = cardId,
                    mealType = "Lunch",
                    status = "Active",
                    deletedAtUtc = (DateTimeOffset?)null,
                    localizations = new Dictionary<string, object>
                    {
                        [SupportedLanguage.ZhTw.CultureName] = new { name = "牛肉麵", description = "牛肉麵 描述" },
                        [SupportedLanguage.EnUs.CultureName] = new { name = "Beef Noodle Soup", description = "Beef Noodle Soup description" }
                    },
                    decisionMetadata = (object?)null
                }
            },
            drawHistory = new[]
            {
                new
                {
                    id = Guid.Parse("77777777-7777-7777-7777-777777777779"),
                    operationId,
                    drawMode = "Normal",
                    cardId,
                    mealTypeAtDraw = "Lunch",
                    succeededAtUtc = new DateTimeOffset(2026, 5, 13, 8, 0, 0, TimeSpan.Zero)
                }
            }
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
