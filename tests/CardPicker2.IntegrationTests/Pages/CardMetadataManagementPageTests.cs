using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

using Microsoft.AspNetCore.Mvc.Testing;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class CardMetadataManagementPageTests : IDisposable
{
    private readonly DrawFeatureWebApplicationFactory _factory = new();

    [Fact]
    public async Task CreateGet_ShowsMetadataFields()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/Cards/Create");

        MetadataFilterHtmlAssertions.AssertCardFormMetadataFields(html);
    }

    [Fact]
    public async Task CreatePost_WithMetadata_PersistsAndShowsMetadataOnDetails()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await _factory.GetAntiForgeryTokenAsync(client, "/Cards/Create");

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.NameZhTw"] = "測試便當",
            ["Input.DescriptionZhTw"] = "測試描述",
            ["Input.NameEnUs"] = "Test Bento",
            ["Input.DescriptionEnUs"] = "Test description",
            ["Input.MealType"] = nameof(MealType.Lunch),
            ["Input.TagsInput"] = "便當, 外帶",
            ["Input.PriceRange"] = nameof(PriceRange.Low),
            ["Input.PreparationTimeRange"] = nameof(PreparationTimeRange.Quick),
            ["Input.DietaryPreferences"] = nameof(DietaryPreference.TakeoutFriendly),
            ["Input.SpiceLevel"] = nameof(SpiceLevel.None)
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var details = WebUtility.HtmlDecode(await _factory.CreateClient().GetStringAsync(response.Headers.Location!.ToString()));
        MetadataFilterHtmlAssertions.AssertDetailsMetadataSection(details, "便當", "外帶", "低價位", "快速", "適合外帶", "不辣");
    }

    [Fact]
    public async Task EditGet_LoadsExistingMetadataIntoFields()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.SchemaV4Document());
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/Cards/Edit/{MetadataFilterTestData.VegetarianLunchCardId}"));

        Assert.Contains("value=\"蔬食, 便當, Bento\"", html);
        Assert.Contains("value=\"Medium\" selected", html);
        Assert.Contains("value=\"Quick\" selected", html);
        Assert.Contains("value=\"Vegetarian\"", html);
        Assert.Contains("checked", html);
    }

    [Fact]
    public async Task CreatePost_WithValidationFailure_PreservesMetadataInput()
    {
        var client = _factory.CreateClient();
        var token = await _factory.GetAntiForgeryTokenAsync(client, "/Cards/Create");

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.NameZhTw"] = "",
            ["Input.TagsInput"] = "便當, 外帶",
            ["Input.PriceRange"] = nameof(PriceRange.High)
        }));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("便當, 外帶", html);
        Assert.Contains("value=\"High\" selected", html);
    }

    [Fact]
    public async Task Details_WithMissingMetadata_ShowsNotSetState()
    {
        await _factory.WriteLibraryDocumentAsync(MetadataFilterTestData.SchemaV4Document());
        var client = _factory.CreateClient();

        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/Cards/{MetadataFilterTestData.MissingMetadataDinnerCardId}"));

        MetadataFilterHtmlAssertions.AssertDetailsMetadataSection(html, "未設定");
    }

    [Fact]
    public async Task CreatePost_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NameZhTw"] = "測試",
            ["Input.DescriptionZhTw"] = "測試",
            ["Input.NameEnUs"] = "Test",
            ["Input.DescriptionEnUs"] = "Test",
            ["Input.MealType"] = nameof(MealType.Lunch)
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
