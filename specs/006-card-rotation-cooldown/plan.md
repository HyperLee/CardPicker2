# 實作計畫: 餐點輪替防重複抽卡

**分支**: `006-card-rotation-cooldown` | **日期**: 2026-05-19 | **規格**: [spec.md](./spec.md)
**Feature 目錄**: `specs/006-card-rotation-cooldown`
**輸入**: 來自 `specs/006-card-rotation-cooldown/spec.md` 的功能規格，並審核使用者提供的 005 技術背景，判斷可沿用項目、不足項目與是否有更適合的替代技術。

**註記**: 本文件由 `/speckit-plan` 工作流產生。006 是 005「餐點條件篩選抽卡」之後的增量功能，因此沿用 003 雙語語系、004 抽卡模式/歷史/統計/刪除保留，以及 005 metadata filtered candidate pool。本功能只在既有候選池之後新增「最近 N 次成功抽卡排除」層，不引入加權、偏好分數、保底、長期黑名單或外部資料介面。

## 摘要

本功能讓首頁抽卡可預設啟用「避免最近重複」，並讓使用者在本次抽卡調整最近成功抽卡排除次數 N。系統先依既有 005 規則建立候選池：正常模式先限制餐別，隨機模式使用全部有效卡牌，再套用 metadata filters；之後才依最近 N 筆已持久化成功抽卡歷史，以不可變 card ID 從候選池排除近期卡牌。排除後的候選池仍以 uniform random index 等機率抽卡。若原始候選池非空但輪替排除後為空，系統不得自動放寬條件，必須提示使用者降低 N、關閉防重複或調整條件。

005 的技術棧整體可沿用，不需要替換 Razor Pages、單一本機 JSON、System.Text.Json、Serilog、xUnit/Moq、WebApplicationFactory 或既有 browser automation。需要補上的不足是 006 專屬領域模型與服務邊界：新增輪替設定、近期排除集合、輪替快照、輪替後候選池、空候選池原因區分、replay 使用持久快照，以及對應 UI/資源文字/測試。JSON root 維持 schema v4；`DrawHistoryRecord` 新增 optional `rotationSnapshot`，既有缺少快照的成功歷史仍合法並持續參與統計與最近 N 次排除。

## 技術背景

**語言/版本**: C# 14 / .NET 10.0；本機 SDK 已確認為 `10.0.100`；目標框架 `net10.0`；ASP.NET Core Razor Pages。Microsoft 目前支援政策顯示 .NET 10 是 active LTS，生產/CI SHOULD 使用最新 10.0.x patch，但本功能不變更 TargetFramework。  
**主要相依性**: ASP.NET Core Razor Pages、ASP.NET Core Localization middleware、Bootstrap 5、jQuery、jQuery Validation、System.Text.Json、Serilog.AspNetCore、Serilog.Sinks.Console、Serilog.Sinks.File；不新增資料庫、不新增外部 JSON API framework、不新增 SPA framework。  
**儲存方式**: 單一本機 JSON 文字檔 `{ContentRootPath}/data/cards.json`，repo 內為 `CardPicker2/data/cards.json`。沿用 005 schema v4 root：`schemaVersion`、`cards`、`drawHistory`；每筆成功 `DrawHistoryRecord` 可有 optional `rotationSnapshot`。讀取 v1/v2/v3/v4 時既有 migration 保持；v4 中缺少 `rotationSnapshot` 不視為 corrupted。corrupted/unreadable/unsupported 原檔必須保留並封鎖操作。寫入維持同目錄 temp file、flush、atomic replace。  
**測試**: xUnit + Moq 作為單元測試；`Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory<Program>` 作為整合測試；既有 Playwright browser automation 測試專案是必要驗證面，覆蓋 RWD、reduced motion、語系與主題切換後防重複設定/結果摘要狀態、FCP/LCP web-vitals smoke check 與安全標頭。  
**目標平台**: 單機桌面與行動瀏覽器，支援 Chrome、Firefox、Safari、Edge。  
**專案類型**: ASP.NET Core Razor Pages web application。  
**效能目標**: 以至少 150 張有效卡牌與 1,000 筆成功抽卡歷史的本機 JSON fixture 驗證時，首頁 GET、首頁 metadata + rotation filtered draw POST、統計投影與 `/Cards` filtered search 的 Page handler p95 < 200ms；FCP < 1.5 秒；LCP < 2.5 秒；首頁防重複控制、抽卡提交、空候選池提示與統計更新回應 < 1 秒。  
**限制條件**: 單一請求記憶體 < 100MB；單機單人本機使用；不使用專案資料庫軟體；規格、計畫、研究、資料模型、快速入門、任務清單與開發交付文件採繁體中文；產品執行時文字依目前核准語系呈現；正式環境 HTTPS Only + HSTS + CSP；所有新增或變更的公開 C# model/service API 必須補齊 XML 文件註解，且每個公開 C# model/service API 註解都包含 `<example>` 與 `<code>`；靜態資源沿用 `MapStaticAssets` / `WithStaticAssets`；不新增付費、下注、點數、賭金、稀有度、保底、權重、偏好加成、長期黑名單或價值分級規則。  
**規模/範圍**: 首頁新增防重複控制與輪替摘要；服務層新增 cooldown settings validation、recent-history projection、candidate exclusion、rotation snapshot persistence 與 replay projection；既有 metadata filters、draw history、statistics、deleted-card retention、bilingual runtime UI 與 theme 行為不得回歸。

## 技術棧審核結論

| 項目 | 結論 | 理由 |
|------|------|------|
| ASP.NET Core Razor Pages | 保留 | 公開介面仍是頁面、表單、query string、hidden field 與 Razor render；不需要 Minimal API、controller JSON API 或 SPA。 |
| .NET 10 / C# 14 | 保留 | 符合憲章與 repo；.NET 10 是 active LTS。CI/部署應維持最新 10.0.x patch，但 feature 不需要升級目標框架。 |
| 單一本機 JSON | 保留 | 抽卡結果、歷史與輪替快照必須同一原子寫入；資料量足以用本機檔案與 in-memory projection 處理。 |
| schema v4 | 保留並延伸 optional history 欄位 | 006 不新增 root 集合；缺少 `rotationSnapshot` 的既有成功歷史仍有效，因此不需要 v5 強制 migration。 |
| System.Text.Json | 保留 | 目前 schema migration、enum conversion 與 optional 欄位足夠；若 performance fixture 顯示 serialization 熱點，可再導入 source-generation context，這是補強不是替換。 |
| Serilog + `ILogger` | 保留 | 已符合結構化日誌要求；新增輪替套用、空候選池、invalid N、replay snapshot 與 write failure 日誌即可。 |
| xUnit/Moq/WebApplicationFactory/browser automation | 保留並擴充 | 需要新增輪替規則、history ordering、idempotency、UI state、RWD/reduced motion/security/performance 覆蓋。 |
| 新資料庫、外部 API、SPA、推薦引擎 | 不新增 | 會擴大公開契約與一致性成本，且違反單機 JSON 與等機率候選池規則。 |
| 需要補上的技術單元 | 新增模型與服務，不新增平台 | `RotationCooldownSettings`、`RotationSnapshot`、`DrawRotationCooldownService`、`RotationCandidatePool`、輪替資源文字與 browser tests。 |

## 憲章檢查

*閘門: 階段 0 研究前必須通過；階段 1 設計後必須重新檢查。*

### 初始閘門，階段 0 前

| 閘門 | 憲章要求 | 狀態 | 證據或例外理由 |
|------|----------|------|----------------|
| 文件語言 | 開發文件必須使用繁體中文；runtime UI 依核准語系呈現 | 通過 | 本計畫與階段 0/1 產物使用繁體中文；新增 runtime 文案補齊 `zh-TW` 與 `en-US` resource。 |
| 程式碼品質 | C# 14、NRT、`.editorconfig`、清楚邊界與 XML 文件註解 | 通過 | 輪替設定、快照與候選池排除放入 `Models/`、`Services/`；PageModels 僅協調 binding、ModelState、redirect 與使用者回饋；新增/變更 public model/service API 補 XML `<example>`/`<code>`。 |
| 測試優先 | 行為、資料規則、驗證邏輯與使用者流程變更必須先寫失敗測試 | 通過 | `tasks.md` 必須先建立 cooldown validation、recent history ordering、candidate exclusion、empty-after-rotation、snapshot persistence、idempotent replay、localization、security、RWD 與 performance 測試，再實作。 |
| UX 一致性 | Bootstrap 5、`site.css`、RWD、可操作錯誤回饋與 WCAG 2.1 AA | 通過 | 防重複 toggle、N control、輪替摘要、空候選池提示與結果區沿用既有 Bootstrap 與 `site.css`，並在 390x844、768x1024、1366x768 驗證。 |
| 效能 | 主要頁面與核心互動必須有效能預算 | 通過 | 近期排除只讀取同一 JSON load 結果中的 history，取最近最多 10 筆成功紀錄形成 `HashSet<Guid>`，不新增 I/O 或外部服務。 |
| 可觀察性 | 結構化日誌記錄關鍵事件且不得洩漏敏感資料 | 通過 | 記錄 invalid N、rotation applied、empty-after-rotation、replay snapshot、draw success、write failure；不得記錄完整 JSON、完整 tag list、stack trace 到 UI 或系統提示。 |
| 安全 | Server validation、Anti-Forgery、HTTPS/HSTS/CSP | 通過 | `POST /?handler=Draw` 維持 Anti-Forgery；N 值與 toggle 由 server 驗證；production HSTS/CSP 不降級。 |
| 資料完整性 | 卡牌、抽卡結果與多步驟操作必須正確、原子且可驗證 | 通過 | 防重複只縮小候選池；快照與成功 history 同一原子寫入；replay 使用已保存結果與快照，不重新計算。 |

### 階段 1 設計後複查

| 閘門 | 狀態 | 設計證據 |
|------|------|----------|
| 文件語言 | 通過 | `research.md`、`data-model.md`、`quickstart.md` 與 `contracts/ui-contract.md` 均為繁體中文。 |
| 程式碼品質 | 通過 | `data-model.md` 定義輪替設定、近期成功範圍、排除集合、輪替前/後候選池、輪替快照與 `DrawHistoryRecord` optional extension。 |
| 測試優先 | 通過 | `quickstart.md` 明確列出先跑失敗測試的 filter；unit/integration/browser/performance/security 測試範圍已定義。 |
| UX 一致性 | 通過 | UI 契約要求防重複控制、N 值、摘要、空候選池提示、語系/主題切換後狀態與 reduced motion 都可操作且不溢出。 |
| 效能 | 通過 | 設計只在已載入 document 上做 in-memory `Take(N)`、`HashSet<Guid>` 與候選池 filtering；N 上限 10；不新增持久化快取。 |
| 可觀察性 | 通過 | 契約列出允許記錄事件與禁止內容，並區分 validation failure、empty base pool、empty after rotation 與 replay。 |
| 安全 | 通過 | 所有 state-changing forms 維持 Anti-Forgery；新增表單值經 server-side enum/int/bool 驗證；CSP 不新增外部來源。 |
| 資料完整性 | 通過 | 先套用 005 base/metadata candidate pool，再套用 006 recent exclusion；history ordering、deleted card exclusion、snapshot persistence 與 idempotent replay 都有模型與契約。 |

**複雜度審查**: 無失敗或豁免項目；不需要憲章例外。

## 專案結構

### 文件，本功能

```text
specs/006-card-rotation-cooldown/
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
│   ├── RotationCooldownSettings.cs    # avoid recent repeats + N validation
│   ├── RotationSnapshot.cs            # persisted minimal summary on successful history
│   ├── RotationCandidatePool.cs       # pre/post rotation pool projection
│   ├── DrawHistoryRecord.cs           # optional RotationSnapshot
│   ├── DrawOperation.cs               # submitted RotationCooldownSettings
│   ├── DrawResult.cs                  # result/empty/replay rotation summary
│   └── CardLibraryDocument.cs         # schema v4 remains current
├── Services/
│   ├── DrawRotationCooldownService.cs # recent N history -> exclusion set -> post-rotation pool
│   ├── DrawCandidatePoolBuilder.cs    # existing 005 base + metadata candidate pool
│   ├── CardLibraryService.cs          # idempotent draw, snapshot append, empty reason, replay
│   ├── DrawStatisticsService.cs       # unchanged formulas; missing snapshot remains valid
│   └── MealCardLocalizationService.cs # summary display projection only
├── Pages/
│   ├── Index.cshtml                   # 防重複控制、N 值、摘要、空候選池提示
│   ├── Index.cshtml.cs                # binding、ModelState、service coordination
│   └── Shared/
│       └── _Layout.cshtml             # 語系與主題入口維持
├── Resources/
│   ├── SharedResource.zh-TW.resx      # cooldown labels、messages、summary
│   └── SharedResource.en-US.resx
└── wwwroot/
    ├── css/
    │   └── site.css                   # cooldown controls、summary chips、responsive constraints
    └── js/
        └── site.js                    # transient UI state/reduced motion; 不決定抽卡結果

tests/
├── CardPicker2.UnitTests/
│   ├── Models/
│   │   ├── RotationCooldownSettingsTests.cs
│   │   └── RotationSnapshotTests.cs
│   └── Services/
│       ├── DrawRotationCooldownServiceTests.cs
│       ├── CardLibraryRotationDrawTests.cs
│       ├── DrawIdempotencyRotationTests.cs
│       └── DrawStatisticsRotationCompatibilityTests.cs
└── CardPicker2.IntegrationTests/
    ├── Pages/
    │   ├── RotationCooldownDrawPageTests.cs
    │   ├── RotationCooldownLocalizationTests.cs
    │   └── RotationCooldownReplayTests.cs
    ├── Browser/
    │   └── RotationCooldownResponsiveAccessibilityTests.cs
    └── Performance/
        └── RotationCooldownPerformanceTests.cs
```

**結構決策**: 採用既有 Razor Pages 單專案結構。輪替防重複是抽卡候選池的業務規則，必須放在 model/service 層；首頁 PageModel 只處理 binding、ModelState、anti-forgery protected POST 與顯示狀態。統計公式不新增 aggregate，不新增外部 JSON API。`cards.json` 仍是唯一持久化來源，不導入資料庫。

## 複雜度追蹤

目前無憲章違反或例外。
