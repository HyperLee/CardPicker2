# 實作計畫: 餐點收藏與手動排除抽卡

**分支**: `007-card-preference-controls` | **日期**: 2026-05-30 | **規格**: [spec.md](./spec.md)  
**Feature 目錄**: `specs/007-card-preference-controls`  
**輸入**: 來自 `specs/007-card-preference-controls/spec.md` 的功能規格，並審核使用者提供的既有技術背景，判斷可沿用項目、不足項目與是否需要替換技術。

**註記**: 本文件由 `/speckit-plan` 工作流產生。007 是 003 雙語語系、004 抽卡模式/歷史/統計/刪除保留、005 metadata filtered candidate pool 與 006 近期輪替防重複之後的增量功能。本功能新增長期收藏與手動排除抽卡狀態；不新增推薦權重、收藏加權、外部 API、資料庫、SPA 或長期多使用者偏好同步。

## 摘要

本功能讓使用者可對每張未刪除餐點卡牌保存兩個長期偏好狀態：收藏與排除抽卡。收藏只用於卡牌庫辨識、篩選與抽卡結果區快速整理，不得影響候選池、抽中機率、統計分母或輪替規則。排除抽卡是可逆偏好，不等於刪除；被排除的 active card 仍預設出現在卡牌庫與詳情頁，並可搜尋、編輯、取消排除或刪除，但不得進入任何未來正常模式、隨機模式、metadata filtered draw 或近期輪替防重複候選池。

既有技術棧足以支援本功能，應保留 ASP.NET Core Razor Pages、單一本機 JSON、System.Text.Json、Serilog、xUnit/Moq、WebApplicationFactory 與既有 Playwright/browser automation 測試。需要補上的不是平台，而是領域模型與服務邊界：將 `cards.json` 從 schema v4 升級為 schema v5，在每張 `MealCard` 加入 `CardPreferenceState`；新增偏好狀態更新模型、偏好篩選條件、可抽集合建構規則、結果區偏好操作、對應雙語 resource keys 與測試。

## 技術背景

**語言/版本**: C# 14 / .NET 10.0；目標框架 `net10.0`；ASP.NET Core Razor Pages。官方 .NET 支援政策顯示 .NET 10 是 active LTS；CI/部署 SHOULD 使用最新 10.0.x patch，但本功能不變更 `TargetFramework`。  
**主要相依性**: ASP.NET Core Razor Pages、ASP.NET Core Localization middleware、Bootstrap 5、jQuery、jQuery Validation、System.Text.Json、Serilog.AspNetCore、Serilog.Sinks.Console、Serilog.Sinks.File、xUnit、Moq、Microsoft.AspNetCore.Mvc.Testing、Microsoft.Playwright/Deque.AxeCore.Playwright（既有 browser 驗證）。不新增資料庫、不新增外部 JSON API framework、不新增 SPA framework。  
**儲存方式**: 單一本機 JSON 文字檔 `{ContentRootPath}/data/cards.json`，repo 內為 `CardPicker2/data/cards.json`。007 將文件升級為 schema v5：root 保留 `schemaVersion`、`cards`、`drawHistory`；每張 card 新增 `preferences`，包含 `isFavorite` 與 `isExcludedFromDraw`。讀取 v1/v2/v3/v4 時以安全預設值補齊 preferences：未收藏且未排除；下一次成功寫入以 v5 原子保存。corrupted/unreadable/unsupported 原檔必須保留並封鎖操作；寫入維持同目錄 temp file、flush、atomic replace。  
**測試**: xUnit + Moq 作為單元測試；`Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory<Program>` 作為整合測試；既有 Playwright browser automation 是必要驗證面，覆蓋 RWD、reduced motion、語系/主題切換後偏好狀態、結果區操作、FCP/LCP smoke check、Axe/WCAG smoke 與安全標頭。  
**目標平台**: 單機桌面與行動瀏覽器，支援 Chrome、Firefox、Safari、Edge。  
**專案類型**: ASP.NET Core Razor Pages web application。  
**效能目標**: 以至少 150 張有效卡牌與 1,000 筆成功抽卡歷史的本機 JSON fixture 驗證時，首頁 GET、含偏好排除的 metadata + rotation draw POST、卡牌庫偏好篩選、偏好狀態更新與統計投影的 Page handler p95 < 200ms；FCP < 1.5 秒；LCP < 2.5 秒；使用者操作後主要內容應在 1 秒內更新。  
**限制條件**: 單一請求記憶體 < 100MB；單機單人本機使用；不使用專案資料庫軟體；規格、計畫、研究、資料模型、快速入門、任務清單與開發交付文件採繁體中文；產品執行時文字依目前核准語系呈現；正式環境 HTTPS Only + HSTS + CSP；所有新增或變更的 public C# model/service API 必須補 XML 文件註解，且每個註解都包含 `<example>` 與 `<code>`；靜態資源沿用 `MapStaticAssets` / `WithStaticAssets`；不新增付費、下注、點數、賭金、稀有度、保底、權重、收藏加權、推薦分數或自動學習規則。  
**規模/範圍**: 卡牌庫與詳情頁新增收藏/排除狀態顯示、切換與偏好篩選；首頁抽卡結果區新增對剛抽中卡牌的收藏/排除操作；服務層新增 preference persistence、target-state mutation、pre-filter draw exclusion、search preference filters 與 schema v5 migration；既有 metadata filters、recent rotation、draw history、statistics、deleted-card retention、bilingual runtime UI 與 theme 行為不得回歸。

## 技術棧審核結論

| 項目 | 結論 | 理由 |
|------|------|------|
| ASP.NET Core Razor Pages | 保留 | 公開介面仍是頁面、表單、query string、hidden field 與 Razor render；不需要 Minimal API、controller JSON API 或 SPA。 |
| .NET 10 / C# 14 | 保留 | 符合憲章與 repo；.NET 10 是 active LTS。不要採用 ASP.NET Core 11 preview API。 |
| Bootstrap 5 + jQuery Validation | 保留 | 既有 form workflow、validation 與 responsive UI 足夠；新增偏好控制可用 Bootstrap button group、checkbox/toggle、badge/chips 實作。 |
| 單一本機 JSON | 保留並升級 schema | 偏好狀態綁定 card ID，和 card/drawHistory 放在同一文件可維持原子更新與候選池一致性。 |
| System.Text.Json | 保留 | 目前 schema migration、enum/string conversion 與 optional/default 欄位足夠；資料量不需要換 serializer。 |
| Serilog + `ILogger` | 保留 | 已滿足結構化日誌要求；新增 preference update、excluded candidate filtering、empty-after-preference 與 write failure 日誌即可。 |
| xUnit/Moq/WebApplicationFactory/Playwright | 保留並擴充 | 需要新增 preference state、schema v5、candidate exclusion、search filters、target-state idempotency、結果區操作、localization、security 與 RWD 覆蓋。 |
| 新資料庫、外部 API、SPA、推薦引擎 | 不新增 | 會擴大公開契約與一致性成本，且違反單機 JSON、Razor Pages UI 與等機率候選池規則。 |
| 需要補上的技術單元 | 新增模型與服務，不新增平台 | `CardPreferenceState`、`CardPreferenceUpdateInputModel`、`CardPreferenceCriteria`、`FavoriteFilter`、`DrawEligibilityFilter`、`PreferenceMutationResult`、偏好 resource keys 與 browser tests。 |

## 憲章檢查

*閘門: 階段 0 研究前必須通過；階段 1 設計後必須重新檢查。*

### 初始閘門，階段 0 前

| 閘門 | 憲章要求 | 狀態 | 證據或例外理由 |
|------|----------|------|----------------|
| 文件語言 | 開發文件必須使用繁體中文；runtime UI 依核准語系呈現 | 通過 | 本計畫與階段 0/1 產物使用繁體中文；新增 runtime 文案補齊 `zh-TW` 與 `en-US` resource。 |
| 程式碼品質 | C# 14、NRT、`.editorconfig`、清楚邊界與 XML 文件註解 | 通過 | 偏好狀態、偏好篩選與候選池排除放入 `Models/`、`Services/`；PageModels 僅協調 binding、ModelState、redirect 與使用者回饋；新增/變更 public model/service API 補 XML `<example>`/`<code>`。 |
| 測試優先 | 行為、資料規則、驗證邏輯與使用者流程變更必須先寫失敗測試 | 通過 | `tasks.md` 必須先建立 preference state、schema v5 migration、target-state mutation、excluded draw、favorite filter、draw eligibility filter、result actions、localization、security、RWD 與 performance 測試，再實作。 |
| UX 一致性 | Bootstrap 5、`site.css`、RWD、可操作錯誤回饋與 WCAG 2.1 AA | 通過 | 收藏/排除按鈕、狀態 badge、卡牌庫篩選、結果區操作與空候選池提示沿用既有設計語言，並在 390x844、768x1024、1366x768 驗證。 |
| 效能 | 主要頁面與核心互動必須有效能預算 | 通過 | 偏好判斷是已載入 card collection 的 bool 篩選；不新增外部 I/O；150 張卡牌/1,000 筆 history 仍在既有效能預算內。 |
| 可觀察性 | 結構化日誌記錄關鍵事件且不得洩漏敏感資料 | 通過 | 記錄 preference update、invalid target state、excluded pool count、empty-after-preference、result action、write failure；不得記錄完整 JSON、完整描述、tag list 原文、stack trace 到 UI 或系統提示。 |
| 安全 | Server validation、Anti-Forgery、HTTPS/HSTS/CSP | 通過 | 收藏/排除、取消收藏/排除與抽卡都使用 Anti-Forgery；card ID 與 target state 由 server 驗證；production HSTS/CSP 不降級。 |
| 資料完整性 | 卡牌、抽卡結果與多步驟操作必須正確、原子且可驗證 | 通過 | 偏好更新是 target-state idempotent mutation；排除只縮小候選池；收藏不影響候選池、history 或 statistics；寫入失敗不得局部更新。 |

### 階段 1 設計後複查

| 閘門 | 狀態 | 設計證據 |
|------|------|----------|
| 文件語言 | 通過 | `research.md`、`data-model.md`、`quickstart.md` 與 `contracts/ui-contract.md` 均為繁體中文。 |
| 程式碼品質 | 通過 | `data-model.md` 定義偏好狀態、target-state update、偏好篩選條件、schema v5、候選池順序與結果區操作不變條件。 |
| 測試優先 | 通過 | `quickstart.md` 明確列出先跑失敗測試的 filter；unit/integration/browser/performance/security 測試範圍已定義。 |
| UX 一致性 | 通過 | UI 契約要求卡牌庫/詳情/結果區狀態可鍵盤操作、非僅靠顏色、雙語完整、手機不溢出，並保留已揭示結果。 |
| 效能 | 通過 | 設計只在已載入 document 上做 bool 篩選與 search criteria projection；不新增持久化快取或資料庫。 |
| 可觀察性 | 通過 | 契約列出允許記錄事件與禁止內容，並區分 preference empty pool、metadata empty pool 與 rotation empty pool。 |
| 安全 | 通過 | 所有 state-changing forms 維持 Anti-Forgery；新增表單值經 server-side Guid/bool validation；CSP 不新增外部來源。 |
| 資料完整性 | 通過 | 排除在 metadata 與 rotation 前生效；target-state 重複提交不反轉；收藏與排除不改變 draw history、statistics、rotation snapshot 或 card identity。 |

**複雜度審查**: 無失敗或豁免項目；不需要憲章例外。

## 專案結構

### 文件，本功能

```text
specs/007-card-preference-controls/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ui-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md              # 階段 2 輸出，由 /speckit-tasks 產生
```

### 原始碼，儲存庫根目錄

```text
CardPicker2/
├── Program.cs                         # 既有 DI、Localization、Serilog、HSTS/CSP、MapStaticAssets
├── Models/
│   ├── CardPreferenceState.cs         # isFavorite + isExcludedFromDraw
│   ├── CardPreferenceUpdateInputModel.cs
│   ├── CardPreferenceCriteria.cs
│   ├── FavoriteFilter.cs
│   ├── DrawEligibilityFilter.cs
│   ├── PreferenceMutationResult.cs
│   ├── MealCard.cs                    # schema v5: 新增 Preferences
│   ├── SearchCriteria.cs              # keyword、meal type、metadata filters、preference filters、language
│   ├── DrawOperation.cs               # 維持 mode/filter/rotation；候選池 builder 會排除 excluded cards
│   ├── DrawResult.cs                  # 成功結果包含 current preference projection
│   └── CardLibraryDocument.cs         # schema v5: cards + drawHistory
├── Services/
│   ├── ICardLibraryService.cs         # preference mutation/search/draw contracts
│   ├── CardLibraryService.cs          # schema v5 migration、target-state preference mutation、excluded draw
│   ├── DrawCandidatePoolBuilder.cs    # active + not excluded base pool，再套用 mode/metadata
│   ├── MealCardFilterService.cs       # metadata filters 維持；不處理 preference
│   ├── CardPreferenceFilterService.cs # 卡牌庫 preference filters 與 active drawable projection
│   ├── DrawRotationCooldownService.cs # 維持 006 規則，接收已排除 preference 後的候選池
│   └── MealCardLocalizationService.cs # preference badges/messages display projection only
├── Pages/
│   ├── Index.cshtml                   # 結果區收藏/排除操作、empty-after-preference 提示
│   ├── Index.cshtml.cs                # binding、ModelState、service coordination
│   ├── Cards/
│   │   ├── Index.cshtml               # 收藏/可抽狀態篩選、狀態 badge、列表操作
│   │   ├── Details.cshtml             # preference summary + target-state forms
│   │   ├── Create.cshtml              # 新卡預設未收藏且未排除
│   │   ├── Edit.cshtml                # 編輯內容不重置 preference
│   │   └── _CardPreferenceControls.cshtml
│   └── Shared/
│       └── _Layout.cshtml             # 語系與主題入口維持
├── Resources/
│   ├── SharedResource.zh-TW.resx      # preference labels、messages、filters、badges
│   └── SharedResource.en-US.resx
└── wwwroot/
    ├── css/
    │   └── site.css                   # preference controls、badges、filter panel、responsive constraints
    └── js/
        └── site.js                    # transient UI state/reduced motion；不決定偏好或抽卡結果

tests/
├── CardPicker2.UnitTests/
│   ├── Models/
│   │   ├── CardPreferenceStateTests.cs
│   │   └── CardPreferenceCriteriaTests.cs
│   └── Services/
│       ├── CardLibraryPreferencePersistenceTests.cs
│       ├── CardLibraryPreferenceMutationTests.cs
│       ├── DrawCandidatePoolPreferenceTests.cs
│       ├── CardPreferenceFilterServiceTests.cs
│       └── DrawStatisticsPreferenceCompatibilityTests.cs
└── CardPicker2.IntegrationTests/
    ├── Pages/
    │   ├── CardPreferencePageTests.cs
    │   ├── PreferenceFilteredCardLibraryPageTests.cs
    │   ├── PreferenceFilteredDrawPageTests.cs
    │   └── PreferenceResultActionTests.cs
    ├── Browser/
    │   └── CardPreferenceResponsiveAccessibilityTests.cs
    └── Performance/
        └── CardPreferencePerformanceTests.cs
```

**結構決策**: 採用既有 Razor Pages 單專案結構。偏好狀態是 card identity 綁定的業務資料，放在 `MealCard` 的 schema v5 欄位；候選池排除、target-state mutation 與偏好篩選是核心業務規則，放在 service 層；首頁與卡牌頁 PageModel 僅處理 binding、ModelState、Anti-Forgery protected POST、redirect 與顯示狀態。統計公式不新增 aggregate，不新增外部 JSON API。`cards.json` 仍是唯一持久化來源，不導入資料庫。

## 複雜度追蹤

目前無憲章違反或例外。
