using CardPicker2.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class ThemeModeNonHomePageTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-theme-nonhome-tests-");
    private readonly WebApplicationFactory<Program> _factory;

    public ThemeModeNonHomePageTests()
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
    [MemberData(nameof(NonHomePages))]
    public async Task GetNonHomePage_DoesNotRenderThemeModeSelector(string path)
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync(path);

        ThemeModeHtmlAssertions.AssertNoThemeSelector(html);
    }

    public static TheoryData<string> NonHomePages()
    {
        return ThemeControlledSurfaceData.NonHomePagePaths();
    }

    public void Dispose()
    {
        _factory.Dispose();
        _library.Dispose();
    }
}