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
