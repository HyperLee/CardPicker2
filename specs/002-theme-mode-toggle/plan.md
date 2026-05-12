# 實作計畫: 網站主題模式切換

**分支**: `002-theme-mode-toggle` | **日期**: 2026-05-12 | **規格**: [spec.md](./spec.md)
**輸入**: 來自 `specs/002-theme-mode-toggle/spec.md` 的功能規格，並參考 `markdownFolder/tempPlan2.md` 的技術背景大綱

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## 摘要

本功能在餐點抽卡網站首頁新增「亮色模式」、「暗黑模式」與「跟隨系統」三種主題模式選擇。使用者只能在首頁變更主題偏好；選定後整個網站的首頁、隱私權頁、錯誤頁、卡牌庫、詳情與卡牌管理頁面都必須套用一致有效主題。

技術方向維持既有 ASP.NET Core Razor Pages 架構，不新增外部 JSON API 或資料庫。主題偏好屬於同一瀏覽器與裝置上的前端偏好，以瀏覽器 `localStorage` 保存選取模式字串 `light`、`dark` 或 `system`；實際有效主題在執行時由選取模式與 `prefers-color-scheme` 推導。頁面透過 Bootstrap 5.3 `data-bs-theme`、全站 CSS token 與 `site.js` 套用主題，並在 shared layout 的 `<head>` 內於樣式載入前執行最小 bootstrap script，以降低亮暗主題閃爍風險。

本功能不得修改 `CardPicker2/data/cards.json`、餐點卡牌模型、抽卡隨機規則、搜尋條件或尚未送出的表單資料。所有使用者可見文案與文件維持繁體中文。

## 技術背景

**語言/版本**: C# 14 / .NET 10.0，目標框架 `net10.0`，ASP.NET Core Razor Pages
**主要相依性**: ASP.NET Core Razor Pages、Bootstrap 5.3.3、jQuery、jQuery Validation、System.Text.Json、Serilog.AspNetCore、Serilog.Sinks.Console、Serilog.Sinks.File、瀏覽器 `localStorage`、`matchMedia('(prefers-color-scheme: dark)')` 與 `storage` event
**儲存方式**: 主題偏好僅保存於同一瀏覽器與裝置的 `localStorage` key `cardpicker.theme.mode`，值僅能為 `light`、`dark` 或 `system`；餐點卡牌仍使用既有 `{ContentRootPath}/data/cards.json`
**測試**: xUnit + Moq 作為單元測試；`Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory<Program>` 作為 Razor Pages 整合測試；主題首次套用、跨分頁同步與系統偏好變更使用 browser automation 驗證，優先以 Microsoft Playwright for .NET 放在整合測試專案
**目標平台**: 單機桌面與行動瀏覽器，支援 Chrome、Firefox、Safari、Edge
**專案類型**: ASP.NET Core Razor Pages web application
**效能目標**: Page handler/API p95 < 200ms；FCP < 1.5 秒；LCP < 2.5 秒；主題切換後 1 秒內呈現一致有效主題；同站已開啟分頁 2 秒內同步；首頁首次可見呈現前套用有效主題
**限制條件**: 單一請求記憶體 < 100MB；單機單人本機使用；不使用專案資料庫軟體；所有使用者面向文件與訊息採繁體中文；正式環境 HTTPS Only + HSTS + CSP；所有公開 API 補齊 XML 文件註解，含需要示例的 `<example>` 或 `<code>`；靜態資源沿用 `MapStaticAssets` / `WithStaticAssets`
**規模/範圍**: 主題控制只出現在首頁；有效主題套用到所有目前可由使用者直接瀏覽的站內頁面，包括 `/`、`/Privacy`、`/Error`、`/Cards`、`/Cards/{id}`、`/Cards/Create` 與 `/Cards/Edit/{id}`

## 憲章檢查

*GATE: Phase 0 research 前 MUST 通過；Phase 1 design 後 MUST 重新檢查。*

### 初始閘門，Phase 0 前

| Gate | 憲章要求 | 狀態 | 證據或例外理由 |
|------|----------|------|----------------|
| 文件語言 | 使用者面向文件 MUST 使用繁體中文 zh-TW | PASS | `spec.md`、本計畫與後續 `research.md`、`data-model.md`、`quickstart.md`、UI 契約皆使用繁體中文。 |
| 程式碼品質 | 設計 MUST 符合 `.editorconfig`、C# 14 與可維護性要求 | PASS | 功能以 shared layout、`site.css`、`site.js` 與薄 PageModel 協調完成；不新增未必要的伺服器模型或服務。若新增測試 helper 或公開型別，遵守 NRT 與 XML 文件註解要求。 |
| 測試優先 | 行為變更 MUST 先定義失敗測試 | PASS | `tasks.md` 階段必須先建立主題選擇 HTML、偏好驗證、CSS contract、跨頁套用與 browser 行為測試，再實作 UI/JS/CSS。 |
| UX 一致性 | 使用 Bootstrap 5、`site.css`、RWD、可操作錯誤回饋與 WCAG 2.1 AA 目標 | PASS | 計畫使用 Bootstrap 5.3 color mode、首頁 fieldset/radio 控制、可見焦點與全站 CSS token；規格要求 WCAG 2.2 AA，嚴於憲章最低 WCAG 2.1 AA。 |
| 效能 | 主要頁面與互動 MUST 有效能預算與量測方式 | PASS | 技術背景定義 FCP/LCP、主題切換、跨分頁同步與首次套用預算；quickstart 將包含 browser timing 驗證。 |
| 可觀察性 | 關鍵事件、驗證失敗與錯誤 MUST 有結構化日誌 | PASS | 主題偏好為瀏覽器本機狀態，不傳送到伺服器；伺服器既有 Serilog 不記錄偏好值。前端保存失敗以安全預設與非敏感 console 診斷處理，不阻斷核心流程。 |
| 安全 | 輸入驗證、Anti-Forgery、秘密管理、HTTPS/HSTS/CSP MUST 被處理 | PASS | 主題切換不是伺服器狀態變更，不需要表單 POST；既有抽卡/卡牌表單仍使用 Anti-Forgery。production CSP 需允許經審核的 head bootstrap script（hash/nonce 或等效策略），不記錄或暴露秘密值。 |
| 資料完整性 | 卡牌數量、範圍、狀態轉換與結果一致性 MUST 可驗證 | PASS | 主題切換只改變視覺呈現，不讀寫 `cards.json`，不得改變餐點卡牌、抽卡結果、搜尋條件或未送出表單。主題偏好僅接受三個固定值，無效值回到 `system`。 |

### Phase 1 設計後複查

| Gate | 狀態 | 設計證據 |
|------|------|----------|
| 文件語言 | PASS | Phase 0/1 產物全部以繁體中文撰寫，模式值 `light`、`dark`、`system` 僅作為技術識別值。 |
| 程式碼品質 | PASS | `data-model.md` 將主題狀態界定為 browser preference 與 DOM state；`contracts/ui-contract.md` 明確限制控制項位置與全站套用規則。 |
| 測試優先 | PASS | `quickstart.md` 定義先寫整合測試與 browser behavior 測試，覆蓋首頁控制、非首頁無控制、localStorage 驗證、system 模式、跨分頁同步與無資料污染。 |
| UX 一致性 | PASS | UI 契約要求首頁主題控制可滑鼠、觸控與鍵盤操作，所有主要頁面在三種模式下符合 WCAG 2.2 AA 對比、可見焦點與 responsive 不溢出。 |
| 效能 | PASS | head bootstrap script 僅執行固定值讀取與 attribute 設定；切換以 CSS custom properties 完成，不重載頁面、不觸發伺服器 round trip。 |
| 可觀察性 | PASS | Client-only 保存失敗有非敏感診斷與安全 fallback；伺服器不新增敏感日誌 surface，既有 Serilog 繼續覆蓋卡牌與安全標頭流程。 |
| 安全 | PASS | CSP 將 head bootstrap script 納入明確允許清單；localStorage 內容視為不可信，讀取時白名單驗證；Razor 預設 HTML encoding 不變。 |
| 資料完整性 | PASS | 主題模型明確禁止寫入餐點 JSON；切換主題不得清除表單、驗證訊息、搜尋條件或已揭示結果。 |

**Complexity Review**: 無 FAIL 或 WAIVED；目前不需要額外複雜度豁免。

## 專案結構

### 文件，本功能

```text
specs/002-theme-mode-toggle/
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
├── Program.cs                         # production CSP 若需 hash/nonce 允許 head bootstrap script
├── Pages/
│   ├── Index.cshtml                   # 首頁唯一主題模式控制項
│   ├── Index.cshtml.cs                # 如需傳遞首頁狀態，僅做頁面協調
│   ├── Privacy.cshtml                 # 套用主題，不顯示控制項
│   ├── Error.cshtml                   # 套用主題，不顯示控制項
│   ├── Cards/
│   │   ├── Index.cshtml               # 套用主題，不顯示控制項
│   │   ├── Details.cshtml             # 套用主題，不顯示控制項
│   │   ├── Create.cshtml              # 套用主題，不顯示控制項
│   │   └── Edit.cshtml                # 套用主題，不顯示控制項
│   └── Shared/
│       └── _Layout.cshtml             # 首次可見呈現前套用 data-bs-theme
└── wwwroot/
    ├── css/
    │   └── site.css                   # light/dark/system 有效主題 token、焦點與 RWD 樣式
    └── js/
        └── site.js                    # 主題讀寫、system 監聽、storage event 同步與既有抽卡互動

tests/
├── CardPicker2.UnitTests/
│   └── Models/ or Services/           # 若新增 C# theme helper，先補單元測試；純 JS 則不新增 C# 單元測試
└── CardPicker2.IntegrationTests/
    ├── Pages/
    │   └── ThemeModePageTests.cs      # Razor HTML contract 與首頁/非首頁控制項檢查
    ├── Browser/
    │   └── ThemeModeBrowserTests.cs   # 首次套用、localStorage、system、跨分頁與 responsive/contrast 檢查
    └── SecurityHeadersTests.cs        # production CSP 仍存在並允許必要主題 script
```

**結構決策**: 採用既有 Razor Pages 與 shared layout，因為主題是全站呈現 concern，不需要新增 controller、外部 API 或 server-side persistence。首頁控制項放在 `Pages/Index.cshtml` 以符合「其餘分頁不提供該選項」；全站套用邏輯集中在 `_Layout.cshtml`、`site.css` 與 `site.js`，避免每個頁面複製主題邏輯。Browser-only 偏好狀態不放進 `Models/` 或 `Services/`，以免與餐點卡牌持久化邊界混淆。

## 複雜度追蹤

目前無憲章違反或例外。
