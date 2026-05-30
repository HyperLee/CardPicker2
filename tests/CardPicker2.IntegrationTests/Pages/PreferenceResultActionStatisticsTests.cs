using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class PreferenceResultActionStatisticsTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory =
        DrawFeatureWebApplicationFactory.CreateWithDeterministicRandomizer(0);

    [Fact]
    public async Task ResultPreferencePost_DoesNotAppendHistoryOrChangeRotationSnapshot()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document());
        var client = _factory.CreateClient();
        var operationId = Guid.NewGuid();
        await client.PostAsync("/", await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch),
            drawOperationId: operationId,
            avoidRecentRepeats: true,
            recentDrawCount: "3"));
        var before = await ReadDocumentAsync();
        var cardId = Assert.Single(before.DrawHistory).CardId;

        await client.PostAsync("/?handler=Preference", await _factory.CreatePreferenceContentAsync(
            client,
            cardId,
            targetIsExcludedFromDraw: true,
            drawOperationId: operationId,
            resultCardId: cardId,
            tokenPath: "/"));

        var after = await ReadDocumentAsync();
        var beforeHistory = Assert.Single(before.DrawHistory);
        var afterHistory = Assert.Single(after.DrawHistory);
        Assert.Equal(beforeHistory.Id, afterHistory.Id);
        Assert.NotNull(beforeHistory.RotationSnapshot);
        Assert.NotNull(afterHistory.RotationSnapshot);
        Assert.Equal(beforeHistory.RotationSnapshot.AvoidRecentRepeats, afterHistory.RotationSnapshot.AvoidRecentRepeats);
        Assert.Equal(beforeHistory.RotationSnapshot.RecentDrawCount, afterHistory.RotationSnapshot.RecentDrawCount);
        Assert.Equal(beforeHistory.RotationSnapshot.PreRotationCandidateCount, afterHistory.RotationSnapshot.PreRotationCandidateCount);
        Assert.Equal(beforeHistory.RotationSnapshot.ExcludedCandidateCount, afterHistory.RotationSnapshot.ExcludedCandidateCount);
        Assert.Equal(beforeHistory.RotationSnapshot.PostRotationCandidateCount, afterHistory.RotationSnapshot.PostRotationCandidateCount);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<CardLibraryDocument> ReadDocumentAsync()
    {
        var json = await File.ReadAllTextAsync(_factory.LibraryFilePath);
        return JsonSerializer.Deserialize<CardLibraryDocument>(json, JsonOptions)!;
    }
}
