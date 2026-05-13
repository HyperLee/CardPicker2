# 實作計畫: 抽卡模式與機率統計

**分支**: `004-draw-mode-statistics` | **日期**: 2026-05-13 | **規格**: [spec.md](./spec.md)
**輸入**: 來自 `specs/004-draw-mode-statistics/spec.md` 的功能規格，並納入使用者提供的 .NET/Razor Pages 技術背景；其中資料檔 repo 路徑修正為實際專案路徑 `CardPicker2/data/cards.json`

**註記**: 本範本由 `/speckit-plan` 命令填寫；執行流程請參考 `.specify/templates/plan-template.md`。

## 摘要

本功能在既有首頁老虎機式餐點抽卡上新增「正常模式」與「隨機模式」。正常模式延續現有餐別抽卡流程，必須選擇早餐、午餐或晚餐，並只從該餐別有效卡牌中等機率抽取；隨機模式不要求餐別，直接從全部有效餐點卡牌的大池中等機率抽取。每次成功抽卡必須以不可變卡牌 ID 產生且只產生一筆持久抽卡歷史，統計表則由成功歷史重建總成功抽取次數、單一卡牌抽中次數與歷史機率。

技術方向沿用 ASP.NET Core Razor Pages、Bootstrap 5、jQuery/jQuery Validation、System.Text.Json、Serilog、既有雙語 localization 與單一本機 JSON 檔。`CardPicker2/data/cards.json` 從目前雙語卡牌 schema v2 擴充為 schema v3，新增抽卡歷史與卡牌狀態；讀取 v1/v2 時保留既有資料並在下一次成功寫入時以原子方式升級。PageModels 只負責 binding、ModelState、redirect 與使用者回饋；抽卡模式、候選池、idempotency、歷史紀錄、統計投影、刪除後歷史保留與檔案寫入集中在 `Services/`。

## 技術背景

**語言/版本**: C# 14 / .NET 10.0，實際 SDK `10.0.100`，目標框架 `net10.0`，ASP.NET Core Razor Pages
**主要相依性**: ASP.NET Core Razor Pages、ASP.NET Core Localization middleware、Bootstrap 5、jQuery、jQuery Validation、System.Text.Json、Serilog.AspNetCore、Serilog.Sinks.Console、Serilog.Sinks.File；不新增資料庫或外部 API framework
**儲存方式**: 單一本機 JSON 文字檔 `{ContentRootPath}/data/cards.json`，repo 內為 `CardPicker2/data/cards.json`；schema v3 根文件包含 `schemaVersion`、`cards` 與 `drawHistory`；寫入維持同目錄 temp file、flush、atomic replace；corrupted/unreadable/unsupported 原檔必須保留並封鎖操作
**測試**: xUnit + Moq 作為單元測試；`Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory<Program>` 作為整合測試；必要時使用 TestServer、可替換服務與 browser automation 驗證 Anti-Forgery、idempotent repeat、RWD、reduced motion 與安全標頭
**目標平台**: 單機桌面與行動瀏覽器，支援 Chrome、Firefox、Safari、Edge
**專案類型**: ASP.NET Core Razor Pages web application
**效能目標**: Page handler/API p95 < 200ms；FCP < 1.5 秒；LCP < 2.5 秒；主要頁面切換、搜尋、模式切換、抽卡提交與統計更新回應 < 1 秒
**限制條件**: 單一請求記憶體 < 100MB；單機單人本機使用；不使用專案資料庫軟體；規格、計畫、研究、資料模型、快速入門、任務清單與開發交付文件採繁體中文；產品執行時文字依目前核准語系呈現；正式環境 HTTPS Only + HSTS + CSP；所有新增或變更的公開 API 必須補齊 XML 文件註解，且每個公開 API 註解都包含 `<example>` 與 `<code>`；靜態資源沿用 `MapStaticAssets` / `WithStaticAssets`；不新增付費、下注、點數、賭金、稀有度或價值分級規則
**規模/範圍**: 首頁新增模式選擇、抽卡操作 ID、總成功抽取次數與統計表；服務層新增抽卡歷史、統計投影、候選池與刪除後歷史保留；卡牌庫管理需排除 deleted card 進入後續抽卡池；不建立外部 JSON API，不變更既有雙語語系切換治理

## 憲章檢查

*閘門: 階段 0 研究前必須通過；階段 1 設計後必須重新檢查。*

### 初始閘門，階段 0 前

| 閘門 | 憲章要求 | 狀態 | 證據或例外理由 |
|------|----------|------|----------------|
| 文件語言 | 開發文件必須使用繁體中文；runtime UI 依核准語系呈現 | 通過 | 本計畫與階段 0/1 產物使用繁體中文；程式識別字可維持英文。 |
| 程式碼品質 | C# 14、NRT、`.editorconfig`、清楚邊界與 XML 文件註解 | 通過 | 新增模型與服務放在 `Models/`、`Services/`；PageModels 不承載公平性、統計或 persistence 規則；所有新增或變更的公開 API 需補 XML 文件註解，且每個公開 API 註解都包含 `<example>`/`<code>`。 |
| 測試優先 | 行為、資料規則、驗證邏輯與使用者流程變更必須先寫失敗測試 | 通過 | `tasks.md` 必須先建立 draw mode、candidate pool、公平性、history persistence、idempotency、statistics、delete retention、security headers 與 reduced motion 測試，再實作。 |
| UX 一致性 | Bootstrap 5、`site.css`、RWD、可操作錯誤回饋與 WCAG 2.1 AA | 通過 | 模式選擇、統計表、空狀態、deleted badge 與鼓勵文案都使用既有設計語言；390x844、768x1024、1366x768 需驗證不溢出。 |
| 效能 | 主要頁面與核心互動必須有效能預算 | 通過 | 統計由本機 JSON 歷史在服務層投影；單機資料量小，仍需避免重複 I/O 與同步阻塞；handler p95 < 200ms。 |
| 可觀察性 | 結構化日誌記錄關鍵事件且不得洩漏敏感資料 | 通過 | 記錄 startup/load、schema migration、draw success、repeat replay、validation failure、write failure、blocked recovery；不得記錄完整 JSON、秘密、stack trace 或系統提示。 |
| 安全 | Server validation、Anti-Forgery、HTTPS/HSTS/CSP | 通過 | `POST /?handler=Draw`、card create/edit/delete 與 language/theme forms 維持 Anti-Forgery；production HSTS/CSP 不降級。 |
| 資料完整性 | 卡牌、抽卡結果與多步驟操作必須正確、原子且可驗證 | 通過 | 抽卡結果與歷史紀錄同一原子寫入；寫入失敗不得宣告成功；同一 operation id 重複提交只 replay 原結果。 |

### 階段 1 設計後複查

| 閘門 | 狀態 | 設計證據 |
|------|------|----------|
| 文件語言 | 通過 | `research.md`、`data-model.md`、`quickstart.md` 與 `contracts/ui-contract.md` 均為繁體中文。 |
| 程式碼品質 | 通過 | `data-model.md` 定義 `DrawMode`、`DrawOperation`、`DrawHistoryRecord`、`CardDrawStatistic`、`DrawStatisticsSummary`、schema v3 與狀態轉換；契約限制 PageModel 與 service 職責。 |
| 測試優先 | 通過 | `quickstart.md` 明確列出先跑失敗測試的 filter 與測試範圍；`tasks.md` 產生時須保持測試任務在實作前。 |
| UX 一致性 | 通過 | UI 契約要求正常/隨機模式、統計表、空狀態、deleted 狀態、reduced motion 與禁用狀態在桌面/行動可操作且不誤導機率。 |
| 效能 | 通過 | 統計不持久化 aggregate，避免寫入時維護多份狀態；單次 request 由歷史投影，必要時在服務層同一 load 結果中重用資料。 |
| 可觀察性 | 通過 | 契約列出允許記錄事件與禁止內容；repeat replay 與 write failure 都有明確 log level。 |
| 安全 | 通過 | 所有 state-changing forms 維持 Anti-Forgery；CSP/HSTS 測試保留；錯誤與復原訊息不得包含內部資料。 |
| 資料完整性 | 通過 | schema v3、per-process file lock、operation id unique、atomic append history、deleted card retention 與 statistics projection 都有資料模型與契約。 |

**複雜度審查**: 無失敗或豁免項目；不需要憲章例外。

## 專案結構

### 文件，本功能

```text
specs/004-draw-mode-statistics/
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
├── Program.cs                         # DI、RequestLocalization、Serilog、HSTS/CSP、MapStaticAssets
├── Models/
│   ├── DrawMode.cs                    # Normal / Random
│   ├── DrawOperation.cs               # operation id、mode、meal type、coin state
│   ├── DrawHistoryRecord.cs           # 成功抽卡持久歷史
│   ├── CardDrawStatistic.cs           # 單列統計投影
│   ├── DrawStatisticsSummary.cs       # 總成功次數 + rows
│   ├── CardStatus.cs                  # Active / Deleted
│   ├── MealCard.cs                    # 既有雙語卡牌，擴充狀態欄位
│   ├── CardLibraryDocument.cs         # schema v3: cards + drawHistory
│   ├── DrawResult.cs                  # mode、operation id、replay flag、localized card
│   └── SearchCriteria.cs              # 搜尋僅涵蓋 active cards
├── Services/
│   ├── ICardLibraryService.cs         # draw、history、statistics、card mutation contracts
│   ├── CardLibraryService.cs          # JSON load/write、schema migration、idempotent draw、delete retention
│   ├── CardLibraryFileCoordinator.cs  # per-process SemaphoreSlim，序列化 read-modify-write
│   ├── DrawCandidatePoolBuilder.cs    # normal/random candidate pool
│   ├── DrawStatisticsService.cs       # history + current cards -> statistics projection
│   ├── MealCardRandomizer.cs          # 等機率 index selection
│   ├── DuplicateCardDetector.cs       # active card create/edit duplicate rules
│   └── MealCardLocalizationService.cs # 目前語系顯示投影與 fallback
├── Pages/
│   ├── Index.cshtml                   # 模式選擇、coin/start、結果、總次數、統計表
│   ├── Index.cshtml.cs                # binding、ModelState、service coordination
│   ├── Cards/
│   │   ├── Index.cshtml               # active card browsing/search
│   │   ├── Details.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   └── Delete.cshtml              # delete 後不再進候選池；有歷史則 retained as deleted
│   └── Shared/
│       └── _Layout.cshtml             # 既有語系與主題入口維持
└── wwwroot/
    ├── css/
    │   └── site.css                   # mode control、statistics table、deleted badge、responsive constraints
    └── js/
        └── site.js                    # reduced motion 與重複點擊 UI guard；不決定抽卡結果

tests/
├── CardPicker2.UnitTests/
│   ├── Models/
│   │   ├── DrawModeTests.cs
│   │   └── DrawHistoryRecordTests.cs
│   └── Services/
│       ├── DrawCandidatePoolBuilderTests.cs
│       ├── CardLibraryDrawModeTests.cs
│       ├── DrawIdempotencyTests.cs
│       ├── DrawStatisticsServiceTests.cs
│       └── CardDeletionRetentionTests.cs
└── CardPicker2.IntegrationTests/
    ├── Pages/
    │   ├── DrawModePageTests.cs
    │   ├── DrawStatisticsPageTests.cs
    │   ├── DrawIdempotencyPageTests.cs
    │   └── DeletedCardStatisticsPageTests.cs
    ├── Browser/
    │   └── DrawModeResponsiveAccessibilityTests.cs
    └── SecurityHeadersTests.cs
```

**結構決策**: 採用既有 Razor Pages 單專案結構。抽卡模式、公平性、歷史、統計與刪除後保留都是核心業務規則，放在 `Models/` 與 `Services/`；首頁 PageModel 僅協調 request/response。統計表是 Razor Pages HTML 公開介面，不新增外部 JSON API。卡牌 JSON 仍是唯一持久化來源，不導入資料庫。

## 複雜度追蹤

目前無憲章違反或例外。
