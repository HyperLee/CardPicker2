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

        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
    }
}