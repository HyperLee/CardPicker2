using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class DrawModeLocalizationInvariantTests : IDisposable
{
    private static readonly Guid CardId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task SwitchingCulture_ChangesTextButNotStatisticsIdentityCountsOrProbability()
    {
        await _factory.WriteLibraryDocumentAsync(CreateDocument(), JsonOptions);
        var zhClient = _factory.CreateClientForCulture("zh-TW");
        var enClient = _factory.CreateClientForCulture("en-US");

        var zhHtml = WebUtility.HtmlDecode(await zhClient.GetStringAsync("/"));
        var enHtml = WebUtility.HtmlDecode(await enClient.GetStringAsync("/"));

        Assert.Contains("統計早餐", zhHtml);
        Assert.Contains("Stats Breakfast", enHtml);
        Assert.Contains(">1<", zhHtml);
        Assert.Contains(">1<", enHtml);
        Assert.Contains("100%", zhHtml);
        Assert.Contains("100%", enHtml);
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
                new MealCard(
                    CardId,
                    MealType.Breakfast,
                    new Dictionary<string, MealCardLocalizedContent>
                    {
                        [SupportedLanguage.ZhTw.CultureName] = new("統計早餐", "中文描述"),
                        [SupportedLanguage.EnUs.CultureName] = new("Stats Breakfast", "English description")
                    })
            },
            DrawHistory = new[]
            {
                new DrawHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    OperationId = Guid.NewGuid(),
                    DrawMode = DrawMode.Random,
                    CardId = CardId,
                    MealTypeAtDraw = MealType.Breakfast,
                    SucceededAtUtc = DateTimeOffset.UtcNow
                }
            }
        };
    }
}
