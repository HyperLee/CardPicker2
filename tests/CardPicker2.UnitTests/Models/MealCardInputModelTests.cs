using System.ComponentModel.DataAnnotations;

using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class MealCardInputModelTests
{
    [Fact]
    public void Validate_WithBlankRequiredFields_ReturnsTraditionalChineseErrors()
    {
        var input = new MealCardInputModel
        {
            Name = "   ",
            MealType = null,
            Description = ""
        };

        var results = Validate(input);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(MealCardInputModel.Name)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(MealCardInputModel.MealType)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(MealCardInputModel.Description)));
        Assert.All(results, result => Assert.Contains("請", result.ErrorMessage));
    }

    [Fact]
    public void Validate_WithUnsupportedMealType_ReturnsMealTypeError()
    {
        var input = new MealCardInputModel
        {
            NameZhTw = "鮪魚蛋餅",
            DescriptionZhTw = "附近早餐店的鮪魚蛋餅。",
            NameEnUs = "Tuna Egg Pancake",
            DescriptionEnUs = "A tuna egg pancake.",
            MealType = (MealType)999,
        };

        var results = Validate(input);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(MealCardInputModel.MealType)) &&
            result.ErrorMessage?.Contains("餐別", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Validate_WithMissingEnglishFields_ReturnsBilingualFieldErrors()
    {
        var input = new MealCardInputModel
        {
            NameZhTw = "鮪魚蛋餅",
            DescriptionZhTw = "附近早餐店的鮪魚蛋餅。",
            NameEnUs = "",
            DescriptionEnUs = " ",
            MealType = MealType.Breakfast
        };

        var results = Validate(input);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(MealCardInputModel.NameEnUs)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(MealCardInputModel.DescriptionEnUs)));
    }

    [Fact]
    public void ToDecisionMetadata_WithBlankMetadata_ReturnsNull()
    {
        var input = new MealCardInputModel
        {
            TagsInput = "  , "
        };

        Assert.Null(input.ToDecisionMetadata());
    }

    [Fact]
    public void ToDecisionMetadata_NormalizesTagsAndSelectedOptions()
    {
        var input = new MealCardInputModel
        {
            TagsInput = "  便當, Bento, 便當 ",
            PriceRange = PriceRange.Medium,
            PreparationTimeRange = PreparationTimeRange.Quick,
            DietaryPreferences = new List<DietaryPreference> { DietaryPreference.TakeoutFriendly, DietaryPreference.Vegetarian },
            SpiceLevel = SpiceLevel.Mild
        };

        var metadata = input.ToDecisionMetadata();

        Assert.NotNull(metadata);
        Assert.Equal(new[] { "便當", "Bento" }, metadata.Tags);
        Assert.Equal(PriceRange.Medium, metadata.PriceRange);
        Assert.Equal(PreparationTimeRange.Quick, metadata.PreparationTimeRange);
        Assert.Equal(new[] { DietaryPreference.Vegetarian, DietaryPreference.TakeoutFriendly }, metadata.DietaryPreferences);
        Assert.Equal(SpiceLevel.Mild, metadata.SpiceLevel);
    }

    [Fact]
    public void Validate_WithUnsupportedMetadataEnum_ReturnsMetadataError()
    {
        var input = new MealCardInputModel
        {
            NameZhTw = "測試餐點",
            DescriptionZhTw = "測試描述",
            NameEnUs = "Test Meal",
            DescriptionEnUs = "Test description",
            MealType = MealType.Lunch,
            PriceRange = (PriceRange)999
        };

        var results = Validate(input);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(MealCardInputModel.PriceRange)) &&
            result.ErrorMessage?.Contains("決策資訊", StringComparison.Ordinal) == true);
    }

    private static List<ValidationResult> Validate(MealCardInputModel input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results;
    }
}
