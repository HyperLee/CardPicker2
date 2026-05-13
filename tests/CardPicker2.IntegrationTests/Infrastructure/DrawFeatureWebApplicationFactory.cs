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

    public static DrawFeatureWebApplicationFactory CreateProduction()
    {
        return new DrawFeatureWebApplicationFactory("Production");
    }

    public HttpClient CreateClientForCulture(string cultureName)
    {
        var client = CreateClient();
        client.AddCultureCookie(cultureName);
        return client;
    }

    public HttpClient CreateClientForCulture(
        string cultureName,
        WebApplicationFactoryClientOptions options)
    {
        var client = CreateClient(options);
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

    public async Task<FormUrlEncodedContent> CreateFilteredDrawContentAsync(
        HttpClient client,
        string drawMode = "Normal",
        string? mealType = "Lunch",
        bool coinInserted = true,
        Guid? drawOperationId = null,
        string? priceRange = null,
        string? preparationTimeRange = null,
        IEnumerable<string>? dietaryPreferences = null,
        string? maxSpiceLevel = null,
        IEnumerable<string>? tags = null,
        string tokenPath = "/")
    {
        var token = await GetAntiForgeryTokenAsync(client, tokenPath);
        return new FormUrlEncodedContent(CreateFilteredDrawPayload(
            token,
            drawMode,
            mealType,
            coinInserted,
            drawOperationId,
            priceRange,
            preparationTimeRange,
            dietaryPreferences,
            maxSpiceLevel,
            tags));
    }

    public static IReadOnlyList<KeyValuePair<string, string>> CreateFilteredDrawPayload(
        string antiForgeryToken,
        string drawMode = "Normal",
        string? mealType = "Lunch",
        bool coinInserted = true,
        Guid? drawOperationId = null,
        string? priceRange = null,
        string? preparationTimeRange = null,
        IEnumerable<string>? dietaryPreferences = null,
        string? maxSpiceLevel = null,
        IEnumerable<string>? tags = null)
    {
        var payload = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", antiForgeryToken),
            new("DrawMode", drawMode),
            new("CoinInserted", coinInserted ? "true" : "false"),
            new("DrawOperationId", (drawOperationId ?? Guid.NewGuid()).ToString())
        };

        AddOptional(payload, "MealType", mealType);
        AddOptional(payload, "PriceRange", priceRange);
        AddOptional(payload, "PreparationTimeRange", preparationTimeRange);
        AddOptional(payload, "MaxSpiceLevel", maxSpiceLevel);
        AddRepeated(payload, "DietaryPreferences", dietaryPreferences);
        AddRepeated(payload, "Tags", tags);

        return payload;
    }

    public static string CreateFilterQuery(
        string? keyword = null,
        string? mealType = null,
        string? priceRange = null,
        string? preparationTimeRange = null,
        IEnumerable<string>? dietaryPreferences = null,
        string? maxSpiceLevel = null,
        IEnumerable<string>? tags = null)
    {
        var query = new List<KeyValuePair<string, string>>();

        AddOptional(query, "keyword", keyword);
        AddOptional(query, "mealType", mealType);
        AddOptional(query, "priceRange", priceRange);
        AddOptional(query, "preparationTimeRange", preparationTimeRange);
        AddOptional(query, "maxSpiceLevel", maxSpiceLevel);
        AddRepeated(query, "dietaryPreferences", dietaryPreferences);
        AddRepeated(query, "tags", tags);

        return string.Join("&", query.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    public static string CreateCardsPathWithFilters(
        string? keyword = null,
        string? mealType = null,
        string? priceRange = null,
        string? preparationTimeRange = null,
        IEnumerable<string>? dietaryPreferences = null,
        string? maxSpiceLevel = null,
        IEnumerable<string>? tags = null)
    {
        var query = CreateFilterQuery(
            keyword,
            mealType,
            priceRange,
            preparationTimeRange,
            dietaryPreferences,
            maxSpiceLevel,
            tags);

        return string.IsNullOrEmpty(query) ? "/Cards" : $"/Cards?{query}";
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

    private static void AddOptional(
        ICollection<KeyValuePair<string, string>> values,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(new KeyValuePair<string, string>(key, value));
        }
    }

    private static void AddRepeated(
        ICollection<KeyValuePair<string, string>> values,
        string key,
        IEnumerable<string>? repeatedValues)
    {
        if (repeatedValues is null)
        {
            return;
        }

        foreach (var value in repeatedValues.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            values.Add(new KeyValuePair<string, string>(key, value));
        }
    }
}
