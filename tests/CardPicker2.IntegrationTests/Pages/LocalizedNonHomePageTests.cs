using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class LocalizedNonHomePageTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-localized-nonhome-tests-");
    private readonly WebApplicationFactory<Program> _factory;

    public LocalizedNonHomePageTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    _library.Configure(services);
                });
            });
    }

    [Theory]
    [InlineData("/Cards/11111111-1111-1111-1111-111111111111", "Tuna Egg Pancake", "Edit card")]
    [InlineData("/Privacy", "Privacy", "local JSON file")]
    [InlineData("/Error", "Something went wrong", "Request ID")]
    public async Task NonHomePages_WithEnglishCookie_RenderEnglishCopy(string path, string expectedPrimaryText, string expectedSecondaryText)
    {
        var client = _factory.CreateClient();
        client.AddCultureCookie("en-US");

        var html = WebUtility.HtmlDecode(await client.GetStringAsync(path));

        Assert.Contains(expectedPrimaryText, html);
        Assert.Contains(expectedSecondaryText, html);
        Assert.Contains("Current language: English", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _library.Dispose();
    }
}
