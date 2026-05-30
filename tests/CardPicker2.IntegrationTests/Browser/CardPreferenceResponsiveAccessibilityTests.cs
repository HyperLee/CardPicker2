using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.IntegrationTests.Pages;
using CardPicker2.Models;

namespace CardPicker2.IntegrationTests.Browser;

public sealed class CardPreferenceResponsiveAccessibilityTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory =
        DrawFeatureWebApplicationFactory.CreateWithDeterministicRandomizer(0);

    [Fact]
    public async Task ResultPreferenceControls_RenderSemanticTargetStateForms()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document());
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/", await _factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch)));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        PreferenceHtmlAssertions.AssertPreferenceControls(html);
        Assert.Contains("aria-pressed", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Preference.", html, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
