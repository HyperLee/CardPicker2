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
    [InlineData("/draws")]
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
    [InlineData("/Cards")]
    [InlineData("/Cards?tags=%E4%BE%BF%E7%95%B6&priceRange=Low")]
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
}
