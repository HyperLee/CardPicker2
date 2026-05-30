using System.Text.Json;
using System.Text.Json.Serialization;

using CardPicker2.Models;
using CardPicker2.Services;

using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Infrastructure;

public sealed class TempCardLibrary : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private TempCardLibrary(string directoryPath)
    {
        DirectoryPath = directoryPath;
        FilePath = Path.Combine(directoryPath, "cards.json");
    }

    public string DirectoryPath { get; }

    public string FilePath { get; }

    public static TempCardLibrary Create(string prefix = "cardpicker-theme-tests-")
    {
        return new TempCardLibrary(Directory.CreateTempSubdirectory(prefix).FullName);
    }

    public static async Task<TempCardLibrary> CreateWithSchemaV5Async(
        object? document = null,
        string prefix = "cardpicker-preference-tests-")
    {
        var library = Create(prefix);
        await library.WriteDocumentAsync(document ?? MetadataFilterTestData.PreferenceAwareSchemaV5Document());
        return library;
    }

    public void Configure(IServiceCollection services)
    {
        services.Configure<CardLibraryOptions>(options =>
        {
            options.LibraryFilePath = FilePath;
        });
    }

    public Task WriteDocumentAsync(object document)
    {
        return File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(document, JsonOptions));
    }

    public Task<string> ReadJsonAsync()
    {
        return File.ReadAllTextAsync(FilePath);
    }

    public async Task<CardLibraryDocument> ReadDocumentAsync()
    {
        var json = await ReadJsonAsync();
        return JsonSerializer.Deserialize<CardLibraryDocument>(json, JsonOptions)!;
    }

    public void Dispose()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }

        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath);
        }
    }
}
