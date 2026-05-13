# 實作計畫: 餐點條件篩選抽卡

**分支**: `005-card-metadata-filtered-draw` | **日期**: 2026-05-14 | **規格**: [spec.md](./spec.md)
**Feature 目錄**: `specs/005-card-metadata-filtered-draw`  
**輸入**: 來自 `specs/005-card-metadata-filtered-draw/spec.md` 的功能規格，並審核使用者提供的 004 技術背景是否足以支援本功能。

**註記**: 本文件由 `/speckit-plan` 工作流產生。005 是 004「抽卡模式與機率統計」之後的增量功能，因此沿用 004 的 draw mode、`DrawOperationId`、`drawHistory`、統計投影、已刪除卡牌歷史保留與雙語治理，但新增餐點決策資訊與篩選候選池。

## 摘要

本功能讓使用者在首頁抽卡前套用餐點條件，並在卡牌庫以相同條件瀏覽與搜尋卡牌。條件包含自訂標籤、價格區間、準備或等待時間區間、飲食偏好與辣度。正常模式仍先依餐別建立候選池，再套用所有決策條件；隨機模式不要求餐別，直接從全部有效卡牌套用條件。篩選只縮小候選池，不改變候選池內等機率 `1/N` 規則，也不得影響既有成功歷史與統計口徑。

技術棧整體夠用，不需要替換 ASP.NET Core Razor Pages、單一本機 JSON、System.Text.Json、Serilog、xUnit/Moq 或 WebApplicationFactory。需要補強的是本功能的領域模型與服務邊界：將 `cards.json` 從 schema v3 升級為 schema v4，在每張卡牌加入 optional `decisionMetadata`；新增篩選條件模型、metadata 正規化與驗證、候選池篩選器、卡牌庫篩選投影與對應 UI 契約。PageModels 仍只負責 binding、ModelState、redirect 與使用者回饋；metadata 驗證、篩選與公平候選池建構放在 `Models/` 與 `Services/`。

## 技術背景

**語言/版本**: C# 14 / .NET 10.0；本機 SDK 已確認為 `10.0.100`；目標框架 `net10.0`；ASP.NET Core Razor Pages。  
**主要相依性**: ASP.NET Core Razor Pages、ASP.NET Core Localization middleware、Bootstrap 5、jQuery、jQuery Validation、System.Text.Json、Serilog.AspNetCore、Serilog.Sinks.Console、Serilog.Sinks.File；不新增資料庫、不新增外部 JSON API framework、不新增 SPA framework。  
**儲存方式**: 單一本機 JSON 文字檔 `{ContentRootPath}/data/cards.json`，repo 內為 `CardPicker2/data/cards.json`。005 將文件升級為 schema v4：root 保留 `schemaVersion`、`cards`、`drawHistory`；每張 card 可有 `decisionMetadata`。讀取 v1/v2/v3 時在記憶體補齊 v4 預設值；下一次成功寫入時以 v4 原子保存。corrupted/unreadable/unsupported 原檔必須保留並封鎖操作。寫入維持同目錄 temp file、flush、atomic replace。  
**測試**: xUnit + Moq 作為單元測試；`Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory<Program>` 作為整合測試；既有 Playwright browser automation 測試專案是本功能的必要驗證面，覆蓋 RWD、reduced motion、語系與主題切換後篩選狀態保留、FCP/LCP web-vitals smoke check 與安全標頭。
**目標平台**: 單機桌面與行動瀏覽器，支援 Chrome、Firefox、Safari、Edge。  
**專案類型**: ASP.NET Core Razor Pages web application。  
**效能目標**: 以至少 150 張有效卡牌與 1,000 筆抽卡歷史的本機 JSON fixture 驗證時，首頁 GET、首頁 filtered draw POST、`/Cards` filtered search 與 metadata projection 的 Page handler p95 < 200ms；FCP < 1.5 秒；LCP < 2.5 秒；首頁篩選、卡牌庫篩選、抽卡提交與統計更新回應 < 1 秒。
**限制條件**: 單一請求記憶體 < 100MB；單機單人本機使用；不使用專案資料庫軟體；規格、計畫、研究、資料模型、快速入門、任務清單與開發交付文件採繁體中文；產品執行時文字依目前核准語系呈現；正式環境 HTTPS Only + HSTS + CSP；所有新增或變更的公開 C# model/service API 必須補齊 XML 文件註解，且每個公開 C# model/service API 註解都包含 `<example>` 與 `<code>`；靜態資源沿用 `MapStaticAssets` / `WithStaticAssets`；不新增付費、下注、點數、賭金、稀有度、保底、權重、偏好加成或價值分級規則。
**規模/範圍**: 首頁新增篩選控制與篩選後抽卡；卡牌庫新增組合篩選；create/edit/details 顯示與維護決策資訊；服務層新增 metadata validation、tag normalization、filtered candidate pool 與 schema v4 migration；既有 draw history、statistics、deleted card retention、bilingual runtime UI 與 theme 行為不得回歸。

## 技術棧審核結論

| 項目 | 結論 | 理由 |
|------|------|------|
| ASP.NET Core Razor Pages | 保留 | 功能仍是頁面、表單、query string 與 Razor render；不需要 MVC、Minimal API 或 SPA。 |
| .NET 10 / C# 14 | 保留 | 符合憲章與本機 SDK；.NET 10 為目前支援中的 LTS。 |
| 單一本機 JSON | 保留但升級 schema | metadata 是卡牌的一部分，和 cards/drawHistory 放在同一文件才能維持原子寫入與資料一致性。 |
| System.Text.Json | 保留 | 已用於 schema v1/v2/v3 migration；足以處理 v4 optional metadata 與 enum string conversion。 |
| Serilog + ILogger | 保留 | 已滿足結構化日誌需求；新增篩選、validation 與 schema migration 日誌即可。 |
| xUnit/Moq/WebApplicationFactory | 保留 | 足以覆蓋 metadata 規則、篩選候選池、Razor Pages 表單與 Anti-Forgery。 |
| 新資料庫或外部 API | 不新增 | 超出單機 JSON 產品邊界，也會增加一致性與安全面。 |
| 需要新增的技術單元 | 新增領域模型與服務，不新增平台 | `MealCardDecisionMetadata`、`CardFilterCriteria`、`MealCardMetadataValidator`、`MealCardFilterService`、metadata localization resource keys 與 CSS/JS progressive enhancement。 |

## 憲章檢查

*閘門: 階段 0 研究前必須通過；階段 1 設計後必須重新檢查。*

### 初始閘門，階段 0 前

| 閘門 | 憲章要求 | 狀態 | 證據或例外理由 |
|------|----------|------|----------------|
| 文件語言 | 開發文件必須使用繁體中文；runtime UI 依核准語系呈現 | 通過 | 本計畫與階段 0/1 產物使用繁體中文；runtime 文案補齊 `zh-TW` 與 `en-US` resource。 |
| 程式碼品質 | C# 14、NRT、`.editorconfig`、清楚邊界與 XML 文件註解 | 通過 | metadata、criteria、filter 與 validation 放入 `Models/`、`Services/`；PageModels 不承載核心篩選或公平性規則；所有新增/變更公開 C# model/service API 補 XML 註解與 `<example>`/`<code>`。 |
| 測試優先 | 行為、資料規則、驗證邏輯與使用者流程變更必須先寫失敗測試 | 通過 | `tasks.md` 必須先建立 metadata validation、tag normalization、schema v4 migration、filtered draw、filtered search、empty pool、localization 與 security/RWD 測試，再實作；最終品質閘門必須產出 critical business logic 覆蓋率證據，達 80% 以上或在本計畫記錄例外。 |
| UX 一致性 | Bootstrap 5、`site.css`、RWD、可操作錯誤回饋與 WCAG 2.1 AA | 通過 | 篩選控制、條件摘要、空候選池、無結果、metadata badges 與 create/edit 表單沿用既有 Bootstrap 與 `site.css`。 |
| 效能 | 主要頁面與核心互動必須有效能預算 | 通過 | 篩選在載入後的 in-memory card collection 上執行；不新增外部 I/O；150 張有效卡牌與 1,000 筆抽卡歷史 fixture 下 handler p95 < 200ms，並以 browser automation 檢查 FCP/LCP。 |
| 可觀察性 | 結構化日誌記錄關鍵事件且不得洩漏敏感資料 | 通過 | 記錄 schema v4 migration、metadata validation failure、filtered pool empty、filtered draw success、write failure；不得記錄完整 JSON 或未清理使用者輸入。 |
| 安全 | Server validation、Anti-Forgery、HTTPS/HSTS/CSP | 通過 | `POST /?handler=Draw`、card create/edit/delete、language/theme forms 維持 Anti-Forgery；production HSTS/CSP 不降級。 |
| 資料完整性 | 卡牌、抽卡結果與多步驟操作必須正確、原子且可驗證 | 通過 | metadata 更新與 card 更新同一原子寫入；篩選只縮小候選池；空候選池與驗證失敗不新增 history/statistics。 |

### 階段 1 設計後複查

| 閘門 | 狀態 | 設計證據 |
|------|------|----------|
| 文件語言 | 通過 | `research.md`、`data-model.md`、`quickstart.md` 與 `contracts/ui-contract.md` 均為繁體中文。 |
| 程式碼品質 | 通過 | `data-model.md` 定義 schema v4、metadata enums、criteria、filter service、draw/search contracts 與 state invariants。 |
| 測試優先 | 通過 | `quickstart.md` 明確列出先跑失敗測試的 filter；metadata、schema、draw、search、localization、security、RWD 與 coverage evidence 都有測試範圍。 |
| UX 一致性 | 通過 | UI 契約要求首頁/卡牌庫/表單/詳情的 metadata 控制與摘要可鍵盤操作、可清除、雙語完整且不溢出。 |
| 效能 | 通過 | 不持久化篩選快取，不引入 DB；同一次 load 結果在服務層產生 filter/search/draw 投影；performance fixture 與 browser automation 驗證 handler p95、FCP、LCP 與 1 秒互動更新預算。 |
| 可觀察性 | 通過 | 契約列出允許記錄事件與禁止內容；空候選池、metadata validation、schema migration 與 write failure 有明確 log level。 |
| 安全 | 通過 | 所有 state-changing forms 維持 Anti-Forgery；metadata 顯示保留 Razor HTML encoding；CSP 不新增外部來源。 |
| 資料完整性 | 通過 | v3 -> v4 migration、metadata optional semantics、invalid enum blocking、tag normalization、filtered candidate pool 與 draw history invariants 都有資料模型與契約。 |

**複雜度審查**: 無失敗或豁免項目；不需要憲章例外。

## 專案結構

### 文件，本功能

```text
specs/005-card-metadata-filtered-draw/
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
│   ├── MealCardDecisionMetadata.cs    # optional tags、price、time、dietary preferences、spice
│   ├── PriceRange.cs                  # Low / Medium / High
│   ├── PreparationTimeRange.cs        # Quick / Standard / Long
│   ├── DietaryPreference.cs           # Vegetarian / Light / HeavyFlavor / TakeoutFriendly
│   ├── SpiceLevel.cs                  # None / Mild / Medium / Hot
│   ├── CardFilterCriteria.cs          # 首頁與卡牌庫共用篩選條件
│   ├── FilterSummary.cs               # 目前條件顯示投影
│   ├── MealCard.cs                    # schema v4: 新增 DecisionMetadata
│   ├── MealCardInputModel.cs          # create/edit metadata 欄位
│   ├── SearchCriteria.cs              # keyword、meal type、metadata filters、current language
│   ├── DrawOperation.cs               # draw mode、meal type、coin、operation id、filters
│   ├── DrawResult.cs                  # 成功/失敗結果 + metadata/filter summary
│   └── CardLibraryDocument.cs         # schema v4: cards + drawHistory
├── Services/
│   ├── ICardLibraryService.cs         # filtered draw/search 與 card mutation contracts
│   ├── CardLibraryService.cs          # schema v4 migration、metadata persistence、filtered draw/search
│   ├── MealCardMetadataValidator.cs   # enum 值、tag normalization、input rules
│   ├── MealCardFilterService.cs       # active cards + criteria -> matching cards
│   ├── DrawCandidatePoolBuilder.cs    # normal/random base pool + metadata filter
│   ├── DuplicateCardDetector.cs       # duplicate rules 不包含 metadata
│   └── MealCardLocalizationService.cs # metadata display names 與 fallback projection
├── Pages/
│   ├── Index.cshtml                   # 模式、餐別、metadata filters、coin/start、result、statistics
│   ├── Index.cshtml.cs                # binding、ModelState、service coordination
│   ├── Cards/
│   │   ├── Index.cshtml               # keyword、meal type、metadata filters、clear filters
│   │   ├── Details.cshtml             # metadata summary
│   │   ├── Create.cshtml              # metadata inputs
│   │   ├── Edit.cshtml                # metadata inputs
│   │   └── _CardForm.cshtml           # reusable bilingual + metadata form fields
│   └── Shared/
│       └── _Layout.cshtml             # 語系與主題入口維持
├── Resources/
│   ├── SharedResource.zh-TW.resx      # metadata labels、options、messages
│   └── SharedResource.en-US.resx
└── wwwroot/
    ├── css/
    │   └── site.css                   # filter panel、chips、metadata badges、responsive constraints
    └── js/
        └── site.js                    # filter state preservation、client UX guard；不決定抽卡結果

tests/
├── CardPicker2.UnitTests/
│   ├── Models/
│   │   ├── MealCardDecisionMetadataTests.cs
│   │   └── CardFilterCriteriaTests.cs
│   └── Services/
│       ├── MealCardMetadataValidatorTests.cs
│       ├── MealCardFilterServiceTests.cs
│       ├── DrawCandidatePoolFilterTests.cs
│       ├── CardLibraryMetadataPersistenceTests.cs
│       └── CardLibraryFilteredDrawTests.cs
└── CardPicker2.IntegrationTests/
    ├── Pages/
    │   ├── FilteredDrawPageTests.cs
    │   ├── FilteredCardLibraryPageTests.cs
    │   ├── CardMetadataManagementPageTests.cs
    │   └── FilterLocalizationStateTests.cs
    ├── Browser/
    │   └── MetadataFilterResponsiveAccessibilityTests.cs
    └── SecurityHeadersTests.cs
```

**結構決策**: 採用既有 Razor Pages 單專案結構。metadata 是餐點卡牌的一部分，持久化在 `MealCard`；篩選條件是首頁抽卡與卡牌庫搜尋共用的業務規則，放入 shared model/service；PageModel 僅協調 request/response。統計表與篩選後結果仍是 Razor Pages HTML 公開介面，不新增外部 JSON API。卡牌 JSON 仍是唯一持久化來源，不導入資料庫。

## 複雜度追蹤

目前無憲章違反或例外。
