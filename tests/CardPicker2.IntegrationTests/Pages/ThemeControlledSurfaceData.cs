namespace CardPicker2.IntegrationTests.Pages;

public static class ThemeControlledSurfaceData
{
    public const string SeedCardId = "11111111-1111-1111-1111-111111111111";

    public static TheoryData<string> NonHomePagePaths()
    {
        return new TheoryData<string>
        {
            "/Privacy",
            "/Error",
            "/Cards",
            $"/Cards/{SeedCardId}",
            "/Cards/Create",
            $"/Cards/Edit/{SeedCardId}"
        };
    }

    public static IReadOnlyList<string> MainPagePaths { get; } =
    [
        "/",
        "/Privacy",
        "/Error",
        "/Cards",
        $"/Cards/{SeedCardId}",
        "/Cards/Create",
        $"/Cards/Edit/{SeedCardId}"
    ];
}