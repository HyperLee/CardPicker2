using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class ThemeModePageTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-theme-page-tests-");
    private readonly WebApplicationFactory<Program> _factory;

    public ThemeModePageTests()
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

    [Fact]
    public async Task GetHome_RendersThemeModeSelectorContract()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        ThemeModeHtmlAssertions.AssertHomeThemeSelector(html);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _library.Dispose();
    }
}