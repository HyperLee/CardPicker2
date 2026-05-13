using System.Net;
using System.Text.Json;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class LocalizedSearchPageTests : IDisposable
{
    private readonly TempCardLibrary _library = TempCardLibrary.Create("cardpicker-localized-search-tests-");
    private readonly WebApplicationFactory<Program> _factory;

    public LocalizedSearchPageTests()
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
    public async Task CardsIndex_WithEnglishCookie_RendersEnglishSearchAndCardContent()
    {
        var client = _factory.CreateClient();
        client.AddCultureCookie("en-US");

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Cards?keyword=Tuna"));

        Assert.Contains("Card Library", html);
        Assert.Contains("Meal name keyword", html);
        Assert.Contains("Tuna Egg Pancake", html);
        Assert.DoesNotContain("鮪魚蛋餅", html);
    }

    [Fact]
    public async Task CardsIndex_WithEnglishCookie_SearchesFallbackVisibleNameWhenEnglishIsMissing()
    {
        await File.WriteAllTextAsync(_library.FilePath, JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            cards = new[]
            {
                new
                {
                    id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    name = "早餐卡",
                    mealType = "Breakfast",
                    description = "早餐描述"
                }
            }
        }));
        var client = _factory.CreateClient();
        client.AddCultureCookie("en-US");

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Cards?keyword=早餐"));

        Assert.Contains("早餐卡", html);
        Assert.Contains("Needs English translation", html);
    }

    [Fact]
    public async Task CardsIndex_WithEnglishCookie_RendersMetadataFiltersAndBadgesInEnglish()
    {
        await File.WriteAllTextAsync(
            _library.FilePath,
            JsonSerializer.Serialize(MetadataFilterTestData.SchemaV4Document()));
        var client = _factory.CreateClient();
        client.AddCultureCookie("en-US");
        var path = DrawFeatureWebApplicationFactory.CreateCardsPathWithFilters(
            mealType: nameof(MealType.Lunch),
            dietaryPreferences: new[] { nameof(DietaryPreference.TakeoutFriendly) },
            tags: new[] { "Bento" });

        var html = WebUtility.HtmlDecode(await client.GetStringAsync(path));

        Assert.Contains("Price range", html);
        Assert.Contains("Takeout friendly", html);
        Assert.Contains("Mushroom Vegetable Bento", html);
        Assert.Contains("Bento", html);
        MetadataFilterHtmlAssertions.AssertNoUntranslatedMetadataKeys(html);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _library.Dispose();
    }
}
