using System.Xml.Linq;

namespace CardPicker2.IntegrationTests.Pages;

public sealed class LocalizationResourceTests
{
    [Fact]
    public void SharedResources_HaveMatchingKeysForTraditionalChineseAndEnglish()
    {
        var zhKeys = ReadKeys("CardPicker2/Resources/SharedResource.zh-TW.resx");
        var enKeys = ReadKeys("CardPicker2/Resources/SharedResource.en-US.resx");

        Assert.Equal(zhKeys, enKeys);
        Assert.DoesNotContain(zhKeys, key => string.IsNullOrWhiteSpace(key));
    }

    private static SortedSet<string> ReadKeys(string path)
    {
        var document = XDocument.Load(Path.Combine(GetRepositoryRoot(), path));
        return new SortedSet<string>(document.Root!
            .Elements("data")
            .Select(element => element.Attribute("name")?.Value ?? string.Empty),
            StringComparer.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CardPicker2.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
