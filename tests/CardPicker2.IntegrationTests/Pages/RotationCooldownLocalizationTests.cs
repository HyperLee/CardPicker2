using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class RotationCooldownLocalizationTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task ReplayAcrossLanguagesKeepsCardAndSnapshotCounts()
    {
        var zhClient = _factory.CreateClientForCulture(SupportedLanguage.ZhTw.CultureName);
        var operationId = Guid.NewGuid();
        using var firstContent = await _factory.CreateFilteredDrawContentAsync(
            zhClient,
            drawMode: nameof(DrawMode.Random),
            mealType: null,
            drawOperationId: operationId,
            avoidRecentRepeats: true,
            recentDrawCount: "3");

        var firstResponse = await zhClient.PostAsync("/?handler=Draw", firstContent);
        var firstHtml = WebUtility.HtmlDecode(await firstResponse.Content.ReadAsStringAsync());

        var enClient = _factory.CreateClientForCulture(SupportedLanguage.EnUs.CultureName);
        using var replayContent = await _factory.CreateSameOperationReplayContentAsync(
            enClient,
            operationId,
            drawMode: nameof(DrawMode.Random),
            mealType: null,
            avoidRecentRepeats: false,
            recentDrawCount: "0");
        var replayResponse = await enClient.PostAsync("/?handler=Draw", replayContent);
        var replayHtml = WebUtility.HtmlDecode(await replayResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Contains("輪替後候選池", firstHtml);
        Assert.Contains("After cooldown", replayHtml);
        Assert.Contains("3", firstHtml);
        Assert.Contains("3", replayHtml);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
