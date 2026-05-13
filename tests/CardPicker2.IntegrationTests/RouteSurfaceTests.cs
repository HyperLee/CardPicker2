using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace CardPicker2.IntegrationTests;

public sealed class RouteSurfaceTests
{
    [Theory]
    [InlineData("/api/draw")]
    [InlineData("/api/draw-statistics")]
    [InlineData("/api/cards")]
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
    [InlineData("/Cards/Create")]
    public async Task PublicSurface_RemainsRazorPagesReturningHtml(string path)
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
