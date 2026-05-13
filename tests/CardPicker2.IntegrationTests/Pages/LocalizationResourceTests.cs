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

    [Fact]
    public void SharedResources_ContainMetadataFilterLabelsOptionsValidationAndEmptyStates()
    {
        var zhKeys = ReadKeys("CardPicker2/Resources/SharedResource.zh-TW.resx");
        var enKeys = ReadKeys("CardPicker2/Resources/SharedResource.en-US.resx");
        var requiredKeys = new[]
        {
            "Metadata.Label.Tags",
            "Metadata.Label.PriceRange",
            "Metadata.Label.PreparationTimeRange",
            "Metadata.Label.DietaryPreferences",
            "Metadata.Label.SpiceLevel",
            "Metadata.Option.NotSet",
            "Metadata.PriceRange.Low",
            "Metadata.PriceRange.Medium",
            "Metadata.PriceRange.High",
            "Metadata.PreparationTimeRange.Quick",
            "Metadata.PreparationTimeRange.Standard",
            "Metadata.PreparationTimeRange.Long",
            "Metadata.DietaryPreference.Vegetarian",
            "Metadata.DietaryPreference.Light",
            "Metadata.DietaryPreference.HeavyFlavor",
            "Metadata.DietaryPreference.TakeoutFriendly",
            "Metadata.SpiceLevel.None",
            "Metadata.SpiceLevel.Mild",
            "Metadata.SpiceLevel.Medium",
            "Metadata.SpiceLevel.Hot",
            "Metadata.Validation.InvalidEnum",
            "Metadata.Validation.InvalidTag",
            "Metadata.Filter.EmptyPool",
            "Metadata.Filter.EmptySearch",
            "Metadata.Filter.Summary",
            "Home.Filters.Legend",
            "Home.Filters.TagsPlaceholder",
            "Home.Filters.ActiveConditions",
            "Home.Filters.Clear",
            "Home.Result.MetadataSummary",
            "Cards.ResultCount"
        };

        foreach (var key in requiredKeys)
        {
            Assert.Contains(key, zhKeys);
            Assert.Contains(key, enKeys);
        }
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
