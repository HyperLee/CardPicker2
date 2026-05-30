using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace CardPicker2.IntegrationTests;

public sealed class RouteSurfaceTests
{
    [Theory]
    [InlineData("/api/draw")]
    [InlineData("/api/draw-statistics")]
    [InlineData("/api/cards")]
    [InlineData("/api/metadata")]
    [InlineData("/api/filters")]
    [InlineData("/api/card-metadata")]
    [InlineData("/api/rotation-cooldown")]
    [InlineData("/api/rotation")]
    [InlineData("/api/cooldown")]
    [InlineData("/api/draw-rotation")]
    [InlineData("/api/preferences")]
    [InlineData("/api/card-preferences")]
    [InlineData("/api/favorites")]
    [InlineData("/api/draw-eligibility")]
    [InlineData("/draws")]
    [InlineData("/rotation-cooldown")]
    [InlineData("/preferences")]
    [InlineData("/favorites")]
    public async Task DrawFeature_DoesNotExposeExternalJsonApiEndpoints(string path)
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/?avoidRecentRepeats=true&recentDrawCount=3")]
    [InlineData("/?drawMode=Random&avoidRecentRepeats=false&recentDrawCount=0")]
    [InlineData("/Cards")]
    [InlineData("/Cards?tags=%E4%BE%BF%E7%95%B6&priceRange=Low")]
    [InlineData("/Cards?favoriteFilter=FavoritesOnly&drawEligibilityFilter=DrawableOnly")]
    [InlineData("/Cards?favoriteFilter=NotFavoritesOnly&drawEligibilityFilter=ExcludedOnly")]
    [InlineData("/Cards/Create")]
    [InlineData("/Cards/Edit/11111111-1111-1111-1111-111111111111")]
    public async Task PublicSurface_RemainsRazorPagesReturningHtml(string path)
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/?handler=Preference")]
    [InlineData("/Cards?handler=Preference")]
    [InlineData("/Cards/11111111-1111-1111-1111-111111111111?handler=Preference")]
    public async Task PreferenceHandlerGetRequests_RemainHtmlRazorPageRequests(string path)
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var getResponse = await client.GetAsync(path);

        getResponse.EnsureSuccessStatusCode();
        Assert.Equal("text/html", getResponse.Content.Headers.ContentType?.MediaType);
    }
}
