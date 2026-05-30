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
            "Cards.ResultCount",
            "Form.Metadata.Legend"
        };

        foreach (var key in requiredKeys)
        {
            Assert.Contains(key, zhKeys);
            Assert.Contains(key, enKeys);
        }
    }

    [Fact]
    public void SharedResources_ContainRotationCooldownLabelsValidationSummariesAndEmptyStates()
    {
        var zhKeys = ReadKeys("CardPicker2/Resources/SharedResource.zh-TW.resx");
        var enKeys = ReadKeys("CardPicker2/Resources/SharedResource.en-US.resx");
        var requiredKeys = new[]
        {
            "Rotation.Label.AvoidRecentRepeats",
            "Rotation.Label.RecentDrawCount",
            "Rotation.Hint.RecentDrawCountRange",
            "Rotation.Hint.ZeroDisables",
            "Rotation.Summary.Applied",
            "Rotation.Summary.Disabled",
            "Rotation.Summary.PreRotationCount",
            "Rotation.Summary.ExcludedCount",
            "Rotation.Summary.PostRotationCount",
            "Rotation.Empty.AfterCooldown",
            "Rotation.Validation.InvalidRecentDrawCount",
            "Rotation.History.MissingSnapshot",
            "Rotation.Log.InvalidSettings",
            "Rotation.Log.Applied",
            "Rotation.Log.ReplayMissingSnapshot"
        };

        foreach (var key in requiredKeys)
        {
            Assert.Contains(key, zhKeys);
            Assert.Contains(key, enKeys);
        }
    }

    [Fact]
    public void SharedResources_ContainPreferenceLabelsFiltersActionsAndMessages()
    {
        var zhResources = ReadResources("CardPicker2/Resources/SharedResource.zh-TW.resx");
        var enResources = ReadResources("CardPicker2/Resources/SharedResource.en-US.resx");
        var requiredKeys = new[]
        {
            "Preference.Badges.Aria",
            "Preference.Badge.Favorite",
            "Preference.Badge.Drawable",
            "Preference.Badge.Excluded",
            "Preference.Action.ExcludeFromDraw",
            "Preference.Action.IncludeInDraw",
            "Preference.Action.AddFavorite",
            "Preference.Action.RemoveFavorite",
            "Preference.Filter.Favorite.Label",
            "Preference.Filter.Favorite.All",
            "Preference.Filter.Favorite.FavoritesOnly",
            "Preference.Filter.Favorite.NotFavoritesOnly",
            "Preference.Filter.DrawEligibility.Label",
            "Preference.Filter.DrawEligibility.All",
            "Preference.Filter.DrawEligibility.DrawableOnly",
            "Preference.Filter.DrawEligibility.ExcludedOnly",
            "Preference.EmptyPool",
            "Preference.Update.Succeeded",
            "Preference.Update.NotFound",
            "Preference.Update.Deleted",
            "Preference.Validation.InvalidTarget",
            "Preference.Update.WriteFailed"
        };

        foreach (var key in requiredKeys)
        {
            AssertTranslated(zhResources, key);
            AssertTranslated(enResources, key);
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

    private static IReadOnlyDictionary<string, string> ReadResources(string path)
    {
        var document = XDocument.Load(Path.Combine(GetRepositoryRoot(), path));
        return document.Root!
            .Elements("data")
            .ToDictionary(
                element => element.Attribute("name")?.Value ?? string.Empty,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static void AssertTranslated(IReadOnlyDictionary<string, string> resources, string key)
    {
        Assert.True(resources.TryGetValue(key, out var value), $"Missing resource key {key}.");
        Assert.False(string.IsNullOrWhiteSpace(value), $"Resource key {key} must have non-empty text.");
        Assert.NotEqual(key, value);
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
