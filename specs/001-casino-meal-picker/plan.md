# 實作計畫: 餐點抽卡網站2

**分支**: `001-casino-meal-picker` | **日期**: 2026-05-11 | **規格**: [spec.md](./spec.md)
**輸入**: 來自 `specs/001-casino-meal-picker/spec.md` 的功能規格，並參考 `markdownFolder/tempPlan.md` 的技術背景大綱

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## 摘要

本功能要把預設 ASP.NET Core Razor Pages 專案擴充為單機單人使用的餐點抽卡網站。使用者可以選擇早餐、午餐或晚餐，透過具有賭場老虎機意象的投幣、拉桿與揭示流程取得等機率餐點推薦；也可以瀏覽、搜尋、新增、編輯與刪除本機餐點卡牌庫。

技術方向維持 Razor Pages、Bootstrap 5、jQuery 與本機 JSON 檔案，不引入資料庫服務。業務規則集中在可測試服務層，Razor PageModel 僅負責頁面協調、模型繫結與使用者回饋。持久化使用 `System.Text.Json` 讀寫 `CardPicker2/data/cards.json`，並以原子寫入保護卡牌庫一致性。

## 技術背景

**語言/版本**: C# 14 / .NET 10.0，目標框架 `net10.0`，ASP.NET Core Razor Pages
**主要相依性**: ASP.NET Core Razor Pages、Bootstrap 5、jQuery、jQuery Validation、System.Text.Json、Serilog.AspNetCore、Serilog.Sinks.Console、Serilog.Sinks.File
**儲存方式**: 單一本機 JSON 文字檔；執行期路徑為 `{ContentRootPath}/data/cards.json`，repo 內規劃位置為 `CardPicker2/data/cards.json`
**測試**: xUnit + Moq 作為單元測試；`Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory<Program>` 作為整合測試；必要時以 TestServer 或可替換服務補足表單與錯誤情境
**目標平台**: 單機桌面與行動瀏覽器，支援 Chrome、Firefox、Safari、Edge
**專案類型**: ASP.NET Core Razor Pages web application
**效能目標**: Page handler/API p95 < 200ms；FCP < 1.5 秒；LCP < 2.5 秒；主要頁面切換、搜尋與抽卡啟動互動回應 < 1 秒
**限制條件**: 單一請求記憶體 < 100MB；單機單人本機使用；不使用專案資料庫軟體；所有使用者面向文件與訊息採繁體中文；正式環境 HTTPS Only + HSTS + CSP；所有公開 API 補齊 XML 文件註解，含需要示例的 `<example>` 或 `<code>`；靜態資源沿用 `MapStaticAssets` / `WithStaticAssets`
**規模/範圍**: 首次啟動預載早餐、午餐、晚餐各至少 3 張卡牌；主要資料量為數十到數百張卡牌；支援 CRUD、搜尋、抽卡、空狀態與復原錯誤提示

## 憲章檢查

*GATE: Phase 0 research 前 MUST 通過；Phase 1 design 後 MUST 重新檢查。*

### 初始閘門，Phase 0 前

| Gate | 憲章要求 | 狀態 | 證據或例外理由 |
|------|----------|------|----------------|
| 文件語言 | 使用者面向文件 MUST 使用繁體中文 zh-TW | PASS | `spec.md`、本計畫、`research.md`、`data-model.md`、`quickstart.md` 與 UI 契約皆以繁體中文撰寫。 |
| 程式碼品質 | 設計 MUST 符合 `.editorconfig`、C# 14 與可維護性要求 | PASS | 現有專案啟用 `net10.0`、Nullable Reference Types、Implicit Usings；計畫將領域模型與服務分層，避免 PageModel 承載業務規則。 |
| 測試優先 | 行為變更 MUST 先定義失敗測試 | PASS | `tasks.md` 階段必須先建立單元與整合測試，再實作服務、PageModel 與 UI。 |
| UX 一致性 | 使用 Bootstrap 5、`site.css`、RWD、可操作錯誤回饋與 WCAG 2.1 AA 目標 | PASS | UI 契約要求 Bootstrap 元件、繁體中文文案、可鍵盤操作、減少動態效果與清楚空狀態。 |
| 效能 | 主要頁面與互動 MUST 有效能預算與量測方式 | PASS | 技術背景定義 p95/FCP/LCP/互動回應預算，`quickstart.md` 提供量測步驟。 |
| 可觀察性 | 關鍵事件、驗證失敗與錯誤 MUST 有結構化日誌 | PASS | 計畫導入 Serilog console + rolling file，記錄啟動、資料檔狀態、驗證失敗、寫入失敗與抽卡操作。 |
| 安全 | 輸入驗證、Anti-Forgery、秘密管理、HTTPS/HSTS/CSP MUST 被處理 | PASS | Razor Pages form post 使用 Anti-Forgery；Data Annotations 與服務層驗證輸入；正式環境保留 HTTPS/HSTS 並新增 CSP middleware。 |
| 資料完整性 | 卡牌數量、範圍、狀態轉換與結果一致性 MUST 可驗證 | PASS | JSON 儲存服務採原子寫入；新增/編輯/刪除完整成功或完整失敗；抽卡僅從現有餐別卡牌等機率選出。 |

### Phase 1 設計後複查

| Gate | 狀態 | 設計證據 |
|------|------|----------|
| 文件語言 | PASS | Phase 0/1 產物全部使用繁體中文；程式碼識別字保留英文。 |
| 程式碼品質 | PASS | `Models` 定義資料與輸入模型，`Services` 定義持久化、搜尋、驗證與抽卡服務，`Pages` 僅協調使用者流程。 |
| 測試優先 | PASS | `research.md` 與 `quickstart.md` 明確要求先寫服務單元測試與 Razor Pages 整合測試，再實作功能。 |
| UX 一致性 | PASS | `contracts/ui-contract.md` 定義首頁抽卡流程、列表搜尋、詳情、建立、編輯、刪除確認、阻斷復原錯誤與 reduced-motion 行為。 |
| 效能 | PASS | 卡牌資料量以數十到數百張為主，搜尋與抽卡在記憶體集合內完成；I/O 使用 async file API 並避免同步阻塞。 |
| 可觀察性 | PASS | 服務層將在啟動載入、缺檔重建、腐敗檔案阻斷、驗證失敗、抽卡成功與寫入錯誤記錄結構化事件。 |
| 安全 | PASS | 契約不暴露 JSON API；所有狀態變更經 Razor form post、Anti-Forgery 與伺服器端驗證；CSP 限制 script/style 來源。 |
| 資料完整性 | PASS | `data-model.md` 定義不可變 ID、餐別列舉、重複判斷正規化規則、抽卡狀態轉換與 corrupted JSON 保留規則。 |

**Complexity Review**: 無 FAIL 或 WAIVED；目前不需要額外複雜度豁免。

## 專案結構

### 文件，本功能

```text
specs/001-casino-meal-picker/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ui-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md              # Phase 2 輸出，由 /speckit-tasks 產生
```

### 原始碼，repository root

```text
CardPicker2/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── data/
│   └── cards.json
├── Models/
│   ├── CardLibraryDocument.cs
│   ├── DrawOperationState.cs
│   ├── DrawResult.cs
│   ├── MealCard.cs
│   ├── MealCardInputModel.cs
│   ├── MealType.cs
│   └── SearchCriteria.cs
├── Services/
│   ├── CardLibraryLoadResult.cs
│   ├── CardLibraryOptions.cs
│   ├── CardLibraryService.cs
│   ├── DuplicateCardDetector.cs
│   ├── ICardLibraryService.cs
│   ├── IMealCardRandomizer.cs
│   ├── MealCardRandomizer.cs
│   └── SeedMealCards.cs
├── Pages/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   ├── Cards/
│   │   ├── Index.cshtml
│   │   ├── Index.cshtml.cs
│   │   ├── Details.cshtml
│   │   ├── Details.cshtml.cs
│   │   ├── Create.cshtml
│   │   ├── Create.cshtml.cs
│   │   ├── Edit.cshtml
│   │   └── Edit.cshtml.cs
│   └── Shared/
│       └── _Layout.cshtml
└── wwwroot/
    ├── css/
    │   └── site.css
    └── js/
        └── site.js

tests/
├── CardPicker2.UnitTests/
│   ├── Services/
│   │   ├── CardLibraryServiceTests.cs
│   │   ├── DuplicateCardDetectorTests.cs
│   │   └── MealCardRandomizerTests.cs
│   └── Models/
│       └── MealCardInputModelTests.cs
└── CardPicker2.IntegrationTests/
    ├── Pages/
    │   ├── CardManagementPageTests.cs
    │   ├── DrawPageTests.cs
    │   └── SearchPageTests.cs
    └── SecurityHeadersTests.cs
```

**結構決策**: 採用 Razor Pages 的 page-first 結構，因為功能以首頁抽卡、卡牌列表、詳情與表單管理為主要導航單位。可重複使用的業務邏輯放在 `Services`，資料與輸入約束放在 `Models`，讓單元測試可以不啟動 Web host 即驗證資料完整性。UI 契約不新增外部 JSON API，避免在本機單人網站中引入不必要的 API surface；若後續需要行動端或遠端整合，再另行規格化 API。

## 複雜度追蹤

目前無憲章違反或例外。
