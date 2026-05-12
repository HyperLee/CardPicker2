using Microsoft.Playwright;

namespace CardPicker2.IntegrationTests.Browser;

public sealed class ThemeModeStateIntegrityTests : IClassFixture<ThemeBrowserFixture>
{
    private readonly ThemeBrowserFixture _fixture;

    public ThemeModeStateIntegrityTests(ThemeBrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ThemeChange_DoesNotClearDrawResultOrModifyCardLibraryFile()
    {
        var page = await _fixture.CreateDesktopPageAsync();

        await page.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");
        var before = await File.ReadAllTextAsync(_fixture.CardLibraryFilePath);
        await page.GetByText("早餐", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await page.GetByLabel("投幣確認").CheckAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "拉桿開始抽卡" }).ClickAsync();
        await page.GetByText("抽卡結果").WaitForAsync();

        var resultText = await page.Locator(".draw-result").InnerTextAsync();
        await page.GetByText("暗黑模式").ClickAsync();

        await ThemeModeBrowserTests.WaitForThemeAsync(page, "dark", "dark", 1000);
        Assert.Equal(resultText, await page.Locator(".draw-result").InnerTextAsync());
        Assert.Equal(before, await File.ReadAllTextAsync(_fixture.CardLibraryFilePath));
    }

    [Fact]
    public async Task StorageSync_DoesNotClearSearchQueryCreateFormInputOrValidationMessage()
    {
        var context = await _fixture.CreateContextAsync("chromium");
        var homePage = await context.NewPageAsync();
        var searchPage = await context.NewPageAsync();
        var createPage = await context.NewPageAsync();

        await homePage.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/");
        await searchPage.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/Cards?keyword=牛肉&mealType=Lunch");
        await createPage.GotoAsync($"{ThemeBrowserFixture.BaseUrl}/Cards/Create");

        await createPage.GetByLabel("餐點名稱").FillAsync("測試義大利麵");
        await createPage.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "新增卡牌" }).ClickAsync();
        await createPage.GetByText("請選擇早餐、午餐或晚餐。").First.WaitForAsync();

        await homePage.GetByText("暗黑模式").ClickAsync();

        await ThemeModeBrowserTests.WaitForThemeAsync(searchPage, "dark", "dark", 2000);
        await ThemeModeBrowserTests.WaitForThemeAsync(createPage, "dark", "dark", 2000);
        Assert.Contains("keyword=%E7%89%9B%E8%82%89", searchPage.Url);
        Assert.Equal("測試義大利麵", await createPage.GetByLabel("餐點名稱").InputValueAsync());
        Assert.True(await createPage.GetByText("請選擇早餐、午餐或晚餐。").First.IsVisibleAsync());
    }
}