using CardPicker2.Services;

using Microsoft.Extensions.DependencyInjection;

namespace CardPicker2.IntegrationTests.Infrastructure;

public sealed class TempCardLibrary : IDisposable
{
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

    public void Configure(IServiceCollection services)
    {
        services.Configure<CardLibraryOptions>(options =>
        {
            options.LibraryFilePath = FilePath;
        });
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
