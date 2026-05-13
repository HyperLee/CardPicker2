using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using CardPicker2.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Infrastructure;

public sealed partial class DrawFeatureWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly TempCardLibrary _library;
    private readonly string? _environmentName;

    public DrawFeatureWebApplicationFactory(string? environmentName = null)
    {
        _library = TempCardLibrary.Create("cardpicker-draw-feature-tests-");
        _environmentName = environmentName;
    }

    public string LibraryDirectoryPath => _library.DirectoryPath;

    public string LibraryFilePath => _library.FilePath;

    public HttpClient CreateClientForCulture(string cultureName)
    {
        var client = CreateClient();
        client.AddCultureCookie(cultureName);
        return client;
    }

    public async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path = "/")
    {
        var html = await client.GetStringAsync(path);
        var match = AntiForgeryTokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("Anti-forgery token was not found in the rendered page.");
        }

        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    public Task WriteLibraryJsonAsync(string json)
    {
        return File.WriteAllTextAsync(LibraryFilePath, json);
    }

    public Task WriteLibraryDocumentAsync(object document, JsonSerializerOptions? options = null)
    {
        return File.WriteAllTextAsync(
            LibraryFilePath,
            JsonSerializer.Serialize(document, options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    public void DeleteLibraryFile()
    {
        if (File.Exists(LibraryFilePath))
        {
            File.Delete(LibraryFilePath);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(_environmentName))
        {
            builder.UseEnvironment(_environmentName);
        }

        builder.ConfigureTestServices(services =>
        {
            services.Configure<CardLibraryOptions>(options =>
            {
                options.LibraryFilePath = LibraryFilePath;
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _library.Dispose();
        }

        base.Dispose(disposing);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex AntiForgeryTokenRegex();
}
