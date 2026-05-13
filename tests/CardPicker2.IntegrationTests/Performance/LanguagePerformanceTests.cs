using System.Diagnostics;

using CardPicker2.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Mvc.Testing;

namespace CardPicker2.IntegrationTests.Performance;

[Collection(NonParallelPerformanceCollection.Name)]
public sealed class LanguagePerformanceTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Cards")]
    [InlineData("/Cards/Create")]
    public async Task LocalizedPages_RenderWithinOneSecond(string path)
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        client.AddCultureCookie("en-US");

        var stopwatch = Stopwatch.StartNew();
        var response = await client.GetAsync(path);
        stopwatch.Stop();

        response.EnsureSuccessStatusCode();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"{path} rendered in {stopwatch.Elapsed}.");
    }
}
