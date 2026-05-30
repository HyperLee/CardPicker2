using System.Net;
using System.Text.RegularExpressions;

using CardPicker2.IntegrationTests.Infrastructure;
using CardPicker2.Models;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CardPicker2.IntegrationTests;

public sealed class SecurityHeadersTests
{
    [Fact]
    public async Task ProductionResponses_IncludeHstsAndContentSecurityPolicy()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/");

        AssertProductionSecurityHeaders(response);
        AssertContentSecurityPolicyAllowsThemeBootstrap(GetContentSecurityPolicy(response));
    }

    [Fact]
    public async Task ProductionContentSecurityPolicy_UsesExplicitThemeBootstrapScriptAllowance()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/");
        var contentSecurityPolicy = GetContentSecurityPolicy(response);

        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", contentSecurityPolicy);
        Assert.Matches(@"script-src[^;]*('sha256-|nonce-)", contentSecurityPolicy);
    }

    [Fact]
    public async Task ProductionContentSecurityPolicy_DoesNotAddExternalSourcesForDrawFeature()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/");
        var contentSecurityPolicy = GetContentSecurityPolicy(response);

        Assert.DoesNotContain("https:", contentSecurityPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http:", contentSecurityPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connect-src", contentSecurityPolicy, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/?tags=便當&dietaryPreferences=TakeoutFriendly")]
    [InlineData("/?avoidRecentRepeats=true&recentDrawCount=3")]
    [InlineData("/?drawMode=Random&avoidRecentRepeats=false&recentDrawCount=0")]
    [InlineData("/Cards?tags=便當&priceRange=Low")]
    [InlineData("/Cards?favoriteFilter=FavoritesOnly&drawEligibilityFilter=DrawableOnly")]
    [InlineData("/Cards/Create")]
    public async Task ProductionMetadataSurfaces_IncludeSecurityHeaders(string path)
    {
        await using var factory = DrawFeatureWebApplicationFactory.CreateProduction();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync(path);

        AssertProductionSecurityHeaders(response);
        AssertContentSecurityPolicyAllowsThemeBootstrap(GetContentSecurityPolicy(response));
    }

    [Fact]
    public async Task HomeSecurityForms_RenderAntiForgeryForRotationDrawAndLanguageSwitch()
    {
        await using var factory = new DrawFeatureWebApplicationFactory();
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/?avoidRecentRepeats=true&recentDrawCount=3");

        Assert.Contains("data-language-preserve-form=\"home-draw\"", html, StringComparison.Ordinal);
        Assert.Contains("data-language-switcher", html, StringComparison.Ordinal);
        Assert.Contains("data-theme-mode-selector", html, StringComparison.Ordinal);
        Assert.True(
            Regex.Matches(html, "name=\"__RequestVerificationToken\"", RegexOptions.IgnoreCase).Count >= 2,
            "Home must render anti-forgery tokens for the rotation draw form and language switch form.");
    }

    [Fact]
    public async Task PreferenceForms_RenderAntiForgeryTokensOnHomeLibraryAndDetails()
    {
        await using var factory = DrawFeatureWebApplicationFactory.CreateWithDeterministicRandomizer(0);
        await factory.WriteLibraryDocumentAsync(MetadataFilterTestData.PreferenceAwareSchemaV5Document());
        var client = factory.CreateClient();
        var drawResponse = await client.PostAsync("/", await factory.CreateFilteredDrawContentAsync(
            client,
            drawMode: nameof(DrawMode.Normal),
            mealType: nameof(MealType.Lunch)));
        var homeHtml = await drawResponse.Content.ReadAsStringAsync();
        var libraryHtml = await client.GetStringAsync("/Cards");
        var detailsHtml = await client.GetStringAsync($"/Cards/{MetadataFilterTestData.VegetarianLunchCardId}");

        AssertPreferenceFormsAreProtected(homeHtml, minimumTokenCount: 4);
        AssertPreferenceFormsAreProtected(libraryHtml, minimumTokenCount: 3);
        AssertPreferenceFormsAreProtected(detailsHtml, minimumTokenCount: 4);
    }

    [Theory]
    [InlineData("/?handler=Preference")]
    [InlineData("/Cards?handler=Preference")]
    [InlineData("/Cards/22222222-2222-2222-2222-222222222223?handler=Preference")]
    public async Task PostPreference_WithoutAntiForgeryToken_ReturnsBadRequest(string path)
    {
        await using var factory = new DrawFeatureWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CardId"] = MetadataFilterTestData.VegetarianLunchCardId.ToString(),
            ["TargetIsFavorite"] = "true"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostRotationDraw_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        await using var factory = new DrawFeatureWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["DrawMode"] = nameof(DrawMode.Random),
            ["CoinInserted"] = "true",
            ["DrawOperationId"] = Guid.NewGuid().ToString(),
            ["AvoidRecentRepeats"] = "true",
            ["RecentDrawCount"] = "3"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostLanguageSwitch_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        await using var factory = new DrawFeatureWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/Language?handler=Set", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["culture"] = SupportedLanguage.EnUs.CultureName,
            ["returnUrl"] = "/?avoidRecentRepeats=true&recentDrawCount=3"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDraw_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        await using var factory = new DrawFeatureWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/?handler=Draw", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["DrawMode"] = nameof(DrawMode.Random),
            ["CoinInserted"] = "true",
            ["DrawOperationId"] = Guid.NewGuid().ToString()
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCreateMetadataCard_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        await using var factory = new DrawFeatureWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/Cards/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NameZhTw"] = "測試",
            ["Input.DescriptionZhTw"] = "測試",
            ["Input.NameEnUs"] = "Test",
            ["Input.DescriptionEnUs"] = "Test",
            ["Input.MealType"] = nameof(MealType.Lunch),
            ["Input.TagsInput"] = "便當"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostEditMetadataCard_WithoutAntiForgeryToken_ReturnsBadRequest()
    {
        await using var factory = new DrawFeatureWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/Cards/Edit/11111111-1111-1111-1111-111111111111", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NameZhTw"] = "測試",
            ["Input.DescriptionZhTw"] = "測試",
            ["Input.NameEnUs"] = "Test",
            ["Input.DescriptionEnUs"] = "Test",
            ["Input.MealType"] = nameof(MealType.Breakfast),
            ["Input.TagsInput"] = "早餐"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    internal static string GetContentSecurityPolicy(HttpResponseMessage response)
    {
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        return response.Headers.GetValues("Content-Security-Policy").Single();
    }

    internal static void AssertProductionSecurityHeaders(HttpResponseMessage response)
    {
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
        Assert.Contains("default-src 'self'", GetContentSecurityPolicy(response));
    }

    internal static void AssertContentSecurityPolicyAllowsThemeBootstrap(string contentSecurityPolicy)
    {
        Assert.True(
            contentSecurityPolicy.Contains("script-src", StringComparison.Ordinal)
            && (contentSecurityPolicy.Contains("'unsafe-inline'", StringComparison.Ordinal)
                || contentSecurityPolicy.Contains("'nonce-", StringComparison.Ordinal)
                || contentSecurityPolicy.Contains("'sha256-", StringComparison.Ordinal)),
            "Production CSP must explicitly allow the audited theme bootstrap script while retaining script-src restrictions.");
    }

    private static void AssertPreferenceFormsAreProtected(string html, int minimumTokenCount)
    {
        Assert.Contains("data-card-preference-controls", html, StringComparison.Ordinal);
        Assert.Contains("data-preference-action-form", html, StringComparison.Ordinal);
        Assert.True(
            Regex.Matches(html, "name=\"__RequestVerificationToken\"", RegexOptions.IgnoreCase).Count >= minimumTokenCount,
            "Preference forms must render anti-forgery tokens alongside existing state-changing forms.");
    }
}
