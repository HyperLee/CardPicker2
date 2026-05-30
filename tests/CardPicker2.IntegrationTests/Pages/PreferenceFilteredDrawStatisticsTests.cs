using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class PreferenceFilteredDrawStatisticsTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task PreferenceExclusionUpdate_DoesNotAppendDrawHistory()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document());
        var before = await ReadDocumentAsync();
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var content = await _factory.CreatePreferenceContentAsync(
            client,
            MetadataFilterTestData.VegetarianLunchCardId,
            targetIsExcludedFromDraw: true,
            tokenPath: "/Cards");

        await client.PostAsync("/Cards?handler=Preference", content);

        var after = await ReadDocumentAsync();
        Assert.Equal(before.DrawHistory.Count, after.DrawHistory.Count);
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
