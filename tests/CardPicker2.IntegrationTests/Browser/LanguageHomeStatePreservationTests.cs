using System.Net;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Browser;

public sealed class LanguageHomeStatePreservationTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-language-state-tests-");
    private readonly WebApplicationFactory<Program> _factory;

    public LanguageHomeStatePreservationTests()
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
    public async Task GetHome_WithResultCardIdAndEnglishCookie_ReRendersSameResultInEnglish()
    {
        var client = _factory.CreateClient();
        client.AddCultureCookie("en-US");

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/?mealType=Breakfast&coinInserted=true&resultCardId=11111111-1111-1111-1111-111111111111"));

        Assert.Contains("Tuna Egg Pancake", html);
        Assert.Contains("Breakfast", html);
        Assert.Contains("data-result-card-id=\"11111111-1111-1111-1111-111111111111\"", html);
        Assert.Contains("checked=\"checked\"", html);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _library.Dispose();
    }
}
