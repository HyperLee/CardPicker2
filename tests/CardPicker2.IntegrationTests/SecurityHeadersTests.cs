using System.Net;

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
    [InlineData("/Cards?tags=便當&priceRange=Low")]
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
}
