## 技術背景

**語言/版本**: C# 14 / .NET 10.0，目標框架 `net10.0`，ASP.NET Core Razor Pages
**主要相依性**: ASP.NET Core Razor Pages、Bootstrap 5、jQuery、jQuery Validation、System.Text.Json、Serilog.AspNetCore、Serilog.Sinks.Console、Serilog.Sinks.File
**儲存方式**: 單一本機 JSON 文字檔；執行期路徑為 `{ContentRootPath}/data/cards.json`，repo 內規劃位置為 `CardPicker2/data/cards.json`
**測試**: xUnit + Moq 作為單元測試；`Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory<Program>` 作為整合測試；必要時以 TestServer 或可替換服務補足表單與錯誤情境
**目標平台**: 單機桌面與行動瀏覽器，支援 Chrome、Firefox、Safari、Edge
**專案類型**: ASP.NET Core Razor Pages web application
**效能目標**: Page handler/API p95 < 200ms；FCP < 1.5 秒；LCP < 2.5 秒；主要頁面切換、搜尋與抽卡啟動互動回應 < 1 秒
**限制條件**: 單一請求記憶體 < 100MB；單機單人本機使用；不使用專案資料庫軟體；所有使用者面向文件與訊息採繁體中文；正式環境 HTTPS Only + HSTS + CSP；所有公開 API 補齊 XML 文件註解，含需要示例的 `<example>` 或 `<code>`；靜態資源沿用 `MapStaticAssets` / `WithStaticAssets`