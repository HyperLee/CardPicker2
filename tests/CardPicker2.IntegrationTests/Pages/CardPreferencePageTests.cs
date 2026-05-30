using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class CardPreferencePageTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task CardLibraryAndDetails_RenderExcludedStateAndTargetStateForms()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document(
            excludedCardId: MetadataFilterTestData.VegetarianLunchCardId));
        var client = _factory.CreateClient();

        var listHtml = WebUtility.HtmlDecode(await client.GetStringAsync("/Cards"));
        var detailsHtml = WebUtility.HtmlDecode(await client.GetStringAsync($"/Cards/{MetadataFilterTestData.VegetarianLunchCardId}"));

        PreferenceHtmlAssertions.AssertPreferenceBadges(listHtml, "已排除抽卡");
        PreferenceHtmlAssertions.AssertPreferenceControls(listHtml);
        PreferenceHtmlAssertions.AssertPreferenceBadges(detailsHtml, "已排除抽卡");
        PreferenceHtmlAssertions.AssertPreferenceControls(detailsHtml);
    }

    [Fact]
    public async Task CardLibraryPreferencePost_UpdatesExcludedStateAndReturnsToList()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document());
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var content = await _factory.CreatePreferenceContentAsync(
            client,
            MetadataFilterTestData.VegetarianLunchCardId,
            targetIsExcludedFromDraw: true,
            tokenPath: "/Cards");

        var response = await client.PostAsync("/Cards?handler=Preference", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var html = WebUtility.HtmlDecode(await _factory.CreateClient().GetStringAsync("/Cards"));
        Assert.Contains("已排除抽卡", html, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
