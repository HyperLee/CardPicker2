using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class RotationCooldownSettingsTests
{
    [Fact]
    public void Default_EnablesAvoidRecentRepeatsWithRecentDrawCountThree()
    {
        var settings = RotationCooldownSettings.Default;

        Assert.True(settings.AvoidRecentRepeats);
        Assert.Equal(3, settings.RecentDrawCount);
        Assert.True(settings.IsActive);
        Assert.True(settings.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void RecentDrawCount_WithinRange_IsValid(int recentDrawCount)
    {
        var settings = new RotationCooldownSettings(true, recentDrawCount);

        Assert.True(settings.IsValid);
    }

    [Fact]
    public void RecentDrawCountZero_DisablesRotation()
    {
        var settings = new RotationCooldownSettings(true, 0);

        Assert.True(settings.AvoidRecentRepeats);
        Assert.False(settings.IsActive);
        Assert.True(settings.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void RecentDrawCount_OutsideRange_IsRejected(int recentDrawCount)
    {
        var settings = new RotationCooldownSettings(true, recentDrawCount);

        Assert.False(settings.IsValid);
        Assert.Equal("Rotation.Validation.InvalidRecentDrawCount", settings.ValidationKey);
    }

    [Fact]
    public void ValidationKey_WhenSettingsAreValid_IsNull()
    {
        var settings = new RotationCooldownSettings(false, 10);

        Assert.True(settings.IsValid);
        Assert.Null(settings.ValidationKey);
    }

    [Fact]
    public void TryCreate_WithMissingValuesAndDefaultsEnabled_CreatesDefaultSettings()
    {
        var created = RotationCooldownSettings.TryCreate(
            avoidRecentRepeats: null,
            recentDrawCount: null,
            out var settings,
            out var validationKey);

        Assert.True(created);
        Assert.Equal(RotationCooldownSettings.Default, settings);
        Assert.Null(validationKey);
    }

    [Fact]
    public void TryCreate_WithSubmittedValues_CreatesNormalizedSettings()
    {
        var created = RotationCooldownSettings.TryCreate(
            avoidRecentRepeats: false,
            recentDrawCount: 0,
            out var settings,
            out var validationKey);

        Assert.True(created);
        Assert.False(settings.AvoidRecentRepeats);
        Assert.Equal(0, settings.RecentDrawCount);
        Assert.False(settings.IsActive);
        Assert.Null(validationKey);
    }

    [Fact]
    public void TryCreate_WhenRecentDrawCountCannotBind_RejectsValue()
    {
        var created = RotationCooldownSettings.TryCreate(
            avoidRecentRepeats: true,
            recentDrawCount: null,
            out var settings,
            out var validationKey,
            useDefaultsForMissing: false);

        Assert.False(created);
        Assert.Equal(RotationCooldownSettings.Default, settings);
        Assert.Equal("Rotation.Validation.InvalidRecentDrawCount", validationKey);
    }
}
