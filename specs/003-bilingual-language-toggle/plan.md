# 實作計畫: 雙語語系切換

**分支**: `003-bilingual-language-toggle` | **日期**: 2026-05-13 | **規格**: [spec.md](./spec.md)
**輸入**: 來自 `specs/003-bilingual-language-toggle/spec.md` 的功能規格，並納入使用者提供的 .NET/Razor Pages 技術背景

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## 摘要

本功能在 shared layout 提供繁體中文與英文語系切換，讓首頁、卡牌庫、卡牌詳情、建立、編輯、刪除確認、隱私權與錯誤頁都能依目前語系呈現導覽、按鈕、表單、驗證訊息、狀態訊息、餐別名稱與餐點內容。繁體中文仍是無偏好、無效偏好與 cookie 不可用時的安全預設；英文模式使用英文 UI 與英文餐點內容，既有缺少英文內容的卡牌以繁體中文 fallback 顯示並提示補齊翻譯。

技術方向採用 ASP.NET Core localization middleware、`IStringLocalizer`/resource files、ASP.NET Core culture cookie 與 Razor Pages 表單 handler。不新增外部 JSON API 或資料庫。餐點卡牌 JSON 從目前 schema v1 的單語 `name`/`description` 擴充為 schema v2 的每卡雙語 localizations；讀取 v1 時以繁體中文內容載入並標示英文缺漏，缺檔時建立含完整雙語種子資料的 v2 文件，寫入仍維持同目錄暫存檔與原子替換。

## 技術背景

**語言/版本**: C# 14 / .NET 10.0，目標框架 `net10.0`，ASP.NET Core Razor Pages  
**主要相依性**: ASP.NET Core Razor Pages、ASP.NET Core Localization middleware、`IStringLocalizer`、ResourceManager `.resx`、Bootstrap 5、jQuery、jQuery Validation、System.Text.Json、Serilog.AspNetCore、Serilog.Sinks.Console、Serilog.Sinks.File  
**儲存方式**: 語系偏好使用 ASP.NET Core culture cookie `.AspNetCore.Culture`，僅接受 `zh-TW` 與 `en-US`；餐點卡牌仍使用單一本機 JSON 文字檔 `{ContentRootPath}/data/cards.json`，repo 內為 `CardPicker2/data/cards.json`，schema v2 儲存雙語餐點內容  
**測試**: xUnit + Moq 作為單元測試；`Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory<Program>` 作為整合測試；必要時以 TestServer、可替換服務與 browser automation 驗證 cookie、語系切換、RWD、可及性與狀態保留  
**目標平台**: 單機桌面與行動瀏覽器，支援 Chrome、Firefox、Safari、Edge  
**專案類型**: ASP.NET Core Razor Pages web application  
**效能目標**: Page handler/API p95 < 200ms；FCP < 1.5 秒；LCP < 2.5 秒；主要頁面切換、搜尋、語系切換與抽卡啟動互動回應 < 1 秒  
**限制條件**: 單一請求記憶體 < 100MB；單機單人本機使用；不使用專案資料庫軟體；規格、計畫、研究、資料模型、快速入門、任務清單與開發交付文件採繁體中文；產品執行時 UI 可依偏好呈現繁體中文或英文；正式環境 HTTPS Only + HSTS + CSP；所有公開 API 補齊 XML 文件註解，含需要示例的 `<example>` 或 `<code>`；靜態資源沿用 `MapStaticAssets` / `WithStaticAssets`  
**規模/範圍**: 語系切換入口出現在 shared layout 的所有主要頁面；語系影響 UI 文字、DataAnnotations/ModelState 訊息、餐別顯示、搜尋可見名稱、抽卡結果與卡牌管理表單；不導入自動翻譯服務、不改變抽卡機率或卡牌 ID

## 憲章檢查

*GATE: Phase 0 research 前 MUST 通過；Phase 1 design 後 MUST 重新檢查。*

### 初始閘門，Phase 0 前

| Gate | 憲章要求 | 狀態 | 證據或例外理由 |
|------|----------|------|----------------|
| 文件語言 | 開發文件 MUST 使用繁體中文；產品執行時 UI 可依支援語系呈現 | PASS | 已同步治理為「文件維持 zh-TW，runtime UI 支援 zh-TW/en-US」。本計畫與 Phase 0/1 產物使用繁體中文。 |
| 程式碼品質 | 設計 MUST 符合 `.editorconfig`、C# 14、NRT 與可維護性要求 | PASS | 語系與卡牌本地化邏輯放入 `Models/` 與 `Services/`；PageModels 只協調 binding、localizer、redirect 與使用者回饋。公開模型/服務補 XML 文件註解。 |
| 測試優先 | 行為、資料規則、驗證邏輯或使用者流程變更 MUST 先寫失敗測試 | PASS | `tasks.md` 必須先建立語系 cookie、resource lookup、雙語資料驗證、schema v1/v2 載入、搜尋、抽卡、狀態保留與安全標頭測試，再實作。 |
| UX 一致性 | Bootstrap 5、`site.css`、RWD、可操作錯誤回饋與 WCAG 2.1 AA | PASS | shared layout 語系切換、目前語系狀態、雙語表單欄位與 fallback 提示都使用既有 Bootstrap 元件與 responsive constraints。 |
| 效能 | 主要頁面與核心互動 MUST 有效能預算 | PASS | 語系選擇只設定 cookie 並重新 render/局部更新可見文字；卡牌本地化為本機記憶體投影，不增加外部 I/O。 |
| 可觀察性 | 關鍵事件、驗證失敗、錯誤與不可復原狀態 MUST 有結構化日誌 | PASS | 計畫記錄語系 cookie 無效 fallback、translation missing、schema migration/write failure、validation failure 與 draw success，且不記錄原始秘密、完整資料檔或未清理輸入。 |
| 安全 | 輸入驗證、Anti-Forgery、秘密管理、HTTPS/HSTS/CSP MUST 被處理 | PASS | 語系切換使用 state-changing POST 與 Anti-Forgery；cookie 值白名單；Razor HTML encoding、HSTS 與 production CSP 維持。 |
| 資料完整性 | 卡牌與抽卡結果 MUST 正確、一致且可驗證 | PASS | v1 讀取以 in-memory migration 保留 ID/餐別；v2 寫入原子完成；語系只改變顯示投影，不改變抽卡 pool、機率、已刪除狀態或 result card ID。 |

### Phase 1 設計後複查

| Gate | 狀態 | 設計證據 |
|------|------|----------|
| 文件語言 | PASS | `research.md`、`data-model.md`、`quickstart.md` 與 `contracts/ui-contract.md` 都以繁體中文撰寫；culture code 與 resource key 僅作為技術識別值。 |
| 程式碼品質 | PASS | `data-model.md` 定義 `SupportedLanguage`、`LanguagePreference`、`LocalizedMealContent`、`LocalizedMealCardView`、schema v1/v2 與狀態轉換；UI 契約限制 PageModel 與 service 職責。 |
| 測試優先 | PASS | `quickstart.md` 明確要求先跑 `dotnet test CardPicker2.sln --filter Language` 取得失敗測試，涵蓋 cookie、resource、DataAnnotations、搜尋、抽卡、fallback 與 duplicate detection。 |
| UX 一致性 | PASS | UI 契約要求 shared layout 持續可見語系切換、目前語系可辨識、桌面/行動不溢出，且 fallback prompt 可操作。 |
| 效能 | PASS | 搜尋與抽卡使用目前語系投影；無外部翻譯服務；JSON schema migration 僅在本機檔案讀寫路徑發生。 |
| 可觀察性 | PASS | 合約列出允許記錄事件與禁止內容；translation missing 使用 card ID、meal type、locale 與 message key，不記錄完整描述。 |
| 安全 | PASS | `POST /Language?handler=Set`、卡牌 create/edit/delete 與 draw 都維持 Anti-Forgery；invalid culture cookie fallback 到 zh-TW；CSP/HSTS 測試保留。 |
| 資料完整性 | PASS | schema v1/v2 載入、fallback display、雙語 required validation、跨語系 duplicate detection 與 draw identity invariants 都有資料模型與契約。 |

**Complexity Review**: 無 FAIL 或 WAIVED；治理已更新為允許 runtime bilingual UI，因此不需要憲章例外。

## 專案結構

### 文件，本功能

```text
specs/003-bilingual-language-toggle/
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
├── Program.cs                         # localization services、RequestLocalizationOptions、middleware order、CSP 保留
├── Models/
│   ├── SupportedLanguage.cs           # zh-TW/en-US 白名單與顯示名稱
│   ├── LanguagePreference.cs          # culture cookie 狀態與 fallback
│   ├── MealCard.cs                    # schema v2 雙語 localizations
│   ├── MealCardLocalizedContent.cs    # 單一語系 name/description
│   ├── MealCardInputModel.cs          # zh-TW/en-US 建立與編輯欄位
│   ├── LocalizedMealCardView.cs       # 目前語系顯示投影與 fallback flag
│   └── SearchCriteria.cs              # keyword + meal type + current language
├── Resources/
│   ├── SharedResource.zh-TW.resx      # 預設/繁中 UI、驗證與訊息 key
│   └── SharedResource.en-US.resx      # 英文 UI、驗證與訊息 key
├── Services/
│   ├── LanguagePreferenceService.cs   # cookie 建立、驗證與 returnUrl 安全處理
│   ├── MealCardLocalizationService.cs # 可見內容投影、fallback 與 missing translation 判斷
│   ├── CardLibraryService.cs          # schema v1/v2 讀取、v2 寫入、雙語驗證
│   ├── DuplicateCardDetector.cs       # 兩語系可見 name+description duplicate detection
│   └── SeedMealCards.cs               # 雙語種子資料
├── Pages/
│   ├── Language.cshtml                # 無主要 UI 的 Razor Page handler；POST 設定 culture cookie
│   ├── Language.cshtml.cs
│   ├── Index.cshtml                   # 首頁 localized UI、draw result ID preservation
│   ├── Index.cshtml.cs
│   ├── Privacy.cshtml
│   ├── Error.cshtml
│   ├── Cards/
│   │   ├── Index.cshtml               # 目前語系搜尋與結果投影
│   │   ├── Details.cshtml             # fallback prompt
│   │   ├── Create.cshtml              # 雙語欄位
│   │   ├── Edit.cshtml                # 雙語欄位與 missing translation prompt
│   │   └── _CardForm.cshtml
│   └── Shared/
│       ├── _Layout.cshtml             # shared language switcher、html lang
│       └── _LanguageSwitcher.cshtml   # anti-forgery form + progressive enhancement hooks
└── wwwroot/
    ├── css/
    │   └── site.css                   # language switcher、fallback badge、雙語表單 RWD
    └── js/
        └── site.js                    # 狀態保留輔助；不保存秘密或卡牌資料

tests/
├── CardPicker2.UnitTests/
│   ├── Models/
│   │   └── MealCardInputModelTests.cs
│   └── Services/
│       ├── LanguagePreferenceServiceTests.cs
│       ├── MealCardLocalizationServiceTests.cs
│       ├── CardLibraryLocalizationTests.cs
│       └── DuplicateCardDetectorTests.cs
└── CardPicker2.IntegrationTests/
    ├── Pages/
    │   ├── LanguageSwitchPageTests.cs
    │   ├── LocalizedDrawPageTests.cs
    │   ├── LocalizedSearchPageTests.cs
    │   └── LocalizedCardManagementPageTests.cs
    ├── Browser/
    │   └── LanguageStatePreservationTests.cs
    └── SecurityHeadersTests.cs
```

**結構決策**: 採用既有 Razor Pages 單專案結構。語系選擇是 server-readable runtime preference，放在 ASP.NET Core localization middleware 與 shared layout；餐點雙語內容是核心資料規則，放在 `Models/` 與 `Services/`，不放在 PageModel。卡牌 JSON 仍是唯一持久化來源，不新增資料庫或外部 API。

## 複雜度追蹤

目前無憲章違反或例外。
