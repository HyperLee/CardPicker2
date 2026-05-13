# 任務: 抽卡模式與機率統計

**輸入**: `specs/004-draw-mode-statistics/` 的 `spec.md`、`plan.md`、`research.md`、`data-model.md`、`quickstart.md`、`contracts/ui-contract.md`，並保留 `specs/003-bilingual-language-toggle/` 的雙語 baseline。
**先決條件**: ASP.NET Core Razor Pages app、既有雙語卡牌 schema v2、xUnit/Moq/WebApplicationFactory/Playwright 測試專案。
**測試**: 憲章與本功能 quickstart 明確要求測試優先，因此每個行為故事都先建立失敗測試，再實作。
**組織方式**: 任務依使用者故事分組，讓 P1 可以作為 MVP 獨立完成與驗證，P2 與 P3 可在 foundation 完成後平行推進。

## 格式: `[ID] [P?] [Story] 任務描述`

- **[P]**: 可平行處理，因為任務修改不同檔案且不依賴同階段其他未完成任務。
- **[Story]**: 僅使用於使用者故事階段，對應 `spec.md` 的 US1、US2、US3。
- 每個任務描述都包含具體檔案路徑或驗證命令目標。

---

## 階段 1: 設定 (共用基礎設施)

**目的**: 確認目前專案可還原與建置，並建立後續測試可重用的資料與 WebApplicationFactory 輔助工具。

- [X] T001 執行 `dotnet restore CardPicker2.sln`，確認 `CardPicker2.sln` 目前可還原。
- [X] T002 執行 `dotnet build CardPicker2.sln`，記錄 `CardPicker2.sln` 實作前 build baseline。
- [X] T003 [P] 建立 draw feature 單元測試資料建構器於 `tests/CardPicker2.UnitTests/Services/DrawFeatureTestData.cs`，提供 active/deleted localized cards、schema v3 documents、draw history records 與 deterministic card IDs。
- [X] T004 [P] 建立 draw feature 整合測試 factory 於 `tests/CardPicker2.IntegrationTests/Infrastructure/DrawFeatureWebApplicationFactory.cs`，支援 temp `cards.json`、culture cookie、Anti-Forgery token 與 production environment 切換。

---

## 階段 2: 基礎建設 (阻斷性先決條件)

**目的**: 建立 schema v3、抽卡歷史、卡牌狀態、檔案協調與 migration 基礎；所有 user story 都依賴此階段。

**關鍵限制**: 未完成本階段前，不要開始 US1/US2/US3 的實作任務。

- [X] T005 [P] 新增 schema v3/migration 失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibrarySchemaVersionTests.cs`，覆蓋 v1/v2 in-memory migration、v3 validation、unsupported schema block、corrupted JSON preserve、missing file seed v3。
- [X] T006 [P] 新增檔案協調失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryFileCoordinatorTests.cs`，覆蓋並行 read-modify-write 序列化與 write failure 不覆寫原檔。
- [X] T007 [P] 建立抽卡模式模型於 `CardPicker2/Models/DrawMode.cs`，定義 `Normal` 與 `Random` 並補 XML 文件註解含 `<example>`/`<code>`。
- [X] T008 [P] 建立卡牌狀態模型於 `CardPicker2/Models/CardStatus.cs`，定義 `Active` 與 `Deleted` 並補 XML 文件註解含 `<example>`/`<code>`。
- [X] T009 [P] 建立抽卡操作輸入模型於 `CardPicker2/Models/DrawOperation.cs`，包含 `OperationId`、`Mode`、`MealType`、`CoinInserted`、`RequestedLanguage` 與驗證輔助屬性。
- [X] T010 [P] 建立成功抽卡歷史模型於 `CardPicker2/Models/DrawHistoryRecord.cs`，包含 immutable history ID、operation ID、draw mode、card ID、meal type at draw 與 UTC success time。
- [X] T011 [P] 建立單一卡牌統計列模型於 `CardPicker2/Models/CardDrawStatistic.cs`，包含 card ID、display name、meal type display、status、draw count、historical probability 與 display string。
- [X] T012 [P] 建立統計摘要模型於 `CardPicker2/Models/DrawStatisticsSummary.cs`，包含 total successful draws、rows、has history 與 stable status key。
- [X] T013 更新 schema 根文件於 `CardPicker2/Models/CardLibraryDocument.cs`，將 `CurrentSchemaVersion` 升級為 3 並新增 `DrawHistory` 集合，同時保留 legacy v1 與 bilingual v2 migration 常數。
- [X] T014 建立同 process 檔案協調器於 `CardPicker2/Services/CardLibraryFileCoordinator.cs`，用 `SemaphoreSlim` 包住 JSON read-modify-write critical section。
- [X] T015 更新種子資料建立於 `CardPicker2/Services/SeedMealCards.cs`，讓 missing file 建立 schema v3、每餐別至少 3 張 active bilingual cards，且 `drawHistory` 為空。
- [X] T016 更新 library load/write/validation 於 `CardPicker2/Services/CardLibraryService.cs`，支援 schema v1/v2/v3 migration、active/deleted 狀態驗證、history operation ID uniqueness、history card reference validation、unsupported/corrupted block 與原檔保留。
- [X] T017 更新 DI 註冊於 `CardPicker2/Program.cs`，註冊 `CardLibraryFileCoordinator` 與後續 draw/statistics 服務需要的 singleton/scoped lifetime。
- [X] T018 執行 `dotnet test CardPicker2.sln --filter "CardLibrarySchemaVersion|CardLibraryFileCoordinator"`，確認 `CardPicker2.sln` 的 foundation 測試從失敗轉為通過。

**檢查點**: schema v3 與檔案一致性 foundation 完成，user story implementation 可開始。

---

## 階段 3: 使用者故事 1 - 依抽卡模式取得公平結果 (優先級: P1) MVP

**目標**: 首頁支援正常模式與隨機模式；正常模式只從指定餐別 active cards 抽卡，隨機模式從全部 active cards 抽卡；首次成功抽卡只新增一筆歷史，同一 operation 重送 replay 原結果。

**獨立測試**: 使用具有早餐、午餐、晚餐 active cards 的測試資料，POST normal/random draw，驗證候選池、公平性標稱機率、empty/blocked/missing coin failure、idempotent replay 與 Anti-Forgery。

### 使用者故事 1 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T019 [P] [US1] 新增 `DrawMode` 與 `DrawOperation` 模型失敗測試於 `tests/CardPicker2.UnitTests/Models/DrawModeTests.cs`，覆蓋 invalid mode、normal missing meal type、random ignores meal type、empty operation ID。
- [ ] T020 [P] [US1] 新增候選池建構失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawCandidatePoolBuilderTests.cs`，覆蓋 normal 只含選定餐別 active cards、random 包含全部 active cards、deleted cards 永遠排除、每張候選卡標稱機率為 `1/N`。
- [ ] T021 [P] [US1] 新增服務抽卡模式失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryDrawModeTests.cs`，覆蓋 normal/random 成功、missing coin、invalid meal type、empty pool、blocked library、write failure 不新增 history。
- [ ] T022 [P] [US1] 新增 idempotency 失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawIdempotencyTests.cs`，覆蓋相同 `DrawOperationId` replay 同一 card ID 且 `DrawHistory` 筆數不增加。
- [ ] T023 [P] [US1] 新增首頁抽卡模式整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/DrawModePageTests.cs`，覆蓋 GET 顯示 mode controls/hidden operation ID、POST normal/random、Anti-Forgery、blocked state disable draw。
- [ ] T024 [US1] 執行 `dotnet test CardPicker2.sln --filter "DrawMode|DrawIdempotency"`，確認 `CardPicker2.sln` 的 US1 新測試在實作前失敗。

### 使用者故事 1 的實作

- [ ] T025 [P] [US1] 建立候選池服務於 `CardPicker2/Services/DrawCandidatePoolBuilder.cs`，將 normal/random membership 與 nominal probability 從 PageModel 與 randomizer 中分離。
- [ ] T026 [US1] 更新抽卡服務契約於 `CardPicker2/Services/ICardLibraryService.cs`，新增以 `DrawOperation` 執行 idempotent draw 的 method，保留或調整既有 `DrawAsync` 呼叫端相容性。
- [ ] T027 [US1] 更新抽卡結果模型於 `CardPicker2/Models/DrawResult.cs`，加入 `OperationId`、`DrawMode`、`RequestedMealType`、`IsReplay`、`StatusKey` 與 localized card projection。
- [ ] T028 [US1] 更新抽卡核心流程於 `CardPicker2/Services/CardLibraryService.cs`，在同一 coordinated read-modify-write 中驗證 operation、查找 existing history、建立候選池、呼叫 randomizer、append 一筆 `DrawHistoryRecord`、atomic write 成功後才回傳 success。
- [ ] T029 [US1] 更新首頁 PageModel 於 `CardPicker2/Pages/Index.cshtml.cs`，bind `DrawMode`、`DrawOperationId`、`MealType`、`CoinInserted`，GET 產生新 operation ID，POST 只協調 service call 與 ModelState。
- [ ] T030 [US1] 更新首頁 Razor UI 於 `CardPicker2/Pages/Index.cshtml`，新增正常/隨機模式控制、normal-only meal selector 狀態、hidden `DrawOperationId`、mode/result display 與 blocked-state disable 行為。
- [ ] T031 [P] [US1] 新增或更新繁中抽卡模式資源於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 mode labels、operation validation、empty pool、replay、write failure 與 mode-specific result text。
- [ ] T032 [P] [US1] 新增或更新英文抽卡模式資源於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保 US1 runtime UI 不顯示未翻譯 key。
- [ ] T033 [P] [US1] 更新前端抽卡輔助於 `CardPicker2/wwwroot/js/site.js`，加入 mode switch progressive enhancement、快速連點 UI guard 與 reduced-motion-independent submit state。
- [ ] T034 [P] [US1] 更新首頁 mode controls 樣式於 `CardPicker2/wwwroot/css/site.css`，確保 390x844、768x1024、1366x768 下 mode、meal、coin、lever 不重疊或水平溢出。
- [ ] T035 [US1] 執行 `dotnet test CardPicker2.sln --filter "DrawMode|DrawIdempotency|DrawPage"`，確認 `CardPicker2.sln` 的 US1 測試通過。

**檢查點**: US1 可作為 MVP 獨立展示，包含 normal/random、公平候選池與 idempotent draw history。

---

## 階段 4: 使用者故事 2 - 查看成功抽卡次數與卡牌歷史機率 (優先級: P2)

**目標**: 首頁顯示總成功抽卡次數與統計表；統計由成功歷史重建，失敗嘗試與 replay 不增加分母。

**獨立測試**: 使用已知 draw history document 載入首頁，驗證 total、per-card draw count、historical probability、zero-history empty state、active zero rows 與 persistence after restart。

### 使用者故事 2 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T036 [P] [US2] 新增統計投影失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawStatisticsServiceTests.cs`，覆蓋 50 筆已知成功歷史的 total count、per-card count、probability formula、zero-history null probability、active zero rows、deleted-with-history rows。
- [ ] T037 [P] [US2] 新增首頁統計整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/DrawStatisticsPageTests.cs`，覆蓋 50 筆已知成功歷史的總成功抽取次數、統計表欄位、無歷史空狀態、不顯示誤導性每卡 0%。
- [ ] T038 [P] [US2] 新增歷史持久化整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/DrawHistoryPersistenceTests.cs`，覆蓋成功 draw 後重建 factory/client 仍保留 total、card draw count 與 probability。
- [ ] T039 [US2] 執行 `dotnet test CardPicker2.sln --filter "DrawStatistics|DrawHistoryPersistence"`，確認 `CardPicker2.sln` 的 US2 新測試在實作前失敗。

### 使用者故事 2 的實作

- [ ] T040 [US2] 建立統計投影服務於 `CardPicker2/Services/DrawStatisticsService.cs`，由 `CardLibraryDocument.Cards` 與 `DrawHistory` 產生 `DrawStatisticsSummary`，不持久化 aggregate counters。
- [ ] T041 [US2] 更新服務契約於 `CardPicker2/Services/ICardLibraryService.cs`，新增 `GetDrawStatisticsAsync(SupportedLanguage language, CancellationToken)` 或等效 method。
- [ ] T042 [US2] 更新統計讀取流程於 `CardPicker2/Services/CardLibraryService.cs`，載入 schema v3 document 後投影 localized statistics，blocked library 回傳可呈現的 recovery state。
- [ ] T043 [US2] 更新首頁 PageModel 統計狀態於 `CardPicker2/Pages/Index.cshtml.cs`，GET、draw failure、draw success 與 replay 後都載入最新 `DrawStatisticsSummary`。
- [ ] T044 [US2] 更新首頁統計 Razor UI 於 `CardPicker2/Pages/Index.cshtml`，呈現總成功抽取次數、無歷史空狀態、統計表欄位、active/deleted status 與 localized probability display。
- [ ] T045 [P] [US2] 新增或更新繁中統計資源於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 total label、table headers、empty state、active/deleted status 與 percentage text。
- [ ] T046 [P] [US2] 新增或更新英文統計資源於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保統計區在 `en-US` 無未翻譯 key。
- [ ] T047 [P] [US2] 更新統計表 RWD 樣式於 `CardPicker2/wwwroot/css/site.css`，讓數字、百分比與狀態 badge 在 mobile/tablet/desktop 不重疊、不水平溢出。
- [ ] T048 [US2] 執行 `dotnet test CardPicker2.sln --filter "DrawStatistics|DrawHistoryPersistence"`，確認 `CardPicker2.sln` 的 US2 測試通過。

**檢查點**: US1 與 US2 可同時運作，成功 draw 會持久化 history 並立即更新首頁統計。

---

## 階段 5: 使用者故事 3 - 保留卡牌歷史並避免誤導性呈現 (優先級: P3)

**目標**: 有成功歷史的卡牌刪除後 retained as deleted，不再進候選池但保留統計列；改名/翻譯更新不切分統計；語系、動畫、reduced motion 與文案只影響呈現。

**獨立測試**: 讓卡牌先成功抽中，再刪除、改名、切換語系與啟用 reduced motion，驗證同一 card ID 統計延續、deleted card 不再被抽出、UI 不暗示機率改變。

### 使用者故事 3 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T049 [P] [US3] 新增卡牌刪除保留失敗測試於 `tests/CardPicker2.UnitTests/Services/CardDeletionRetentionTests.cs`，覆蓋無歷史 hard delete、有歷史 retained deleted、deleted card 排除候選池、search/edit/detail 排除 deleted、duplicate detection 忽略 deleted。
- [ ] T050 [P] [US3] 新增 deleted 統計頁整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/DeletedCardStatisticsPageTests.cs`，覆蓋曾抽中 deleted card 仍顯示統計列與 deleted badge，且不出現在未來 draw result。
- [ ] T051 [P] [US3] 新增語系不變性整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/DrawModeLocalizationInvariantTests.cs`，覆蓋 `zh-TW`/`en-US` 切換只改變文字，不改變 operation ID、card ID、draw count、probability 或 card status。
- [ ] T052 [P] [US3] 新增 RWD/reduced-motion/可及性 browser 失敗測試於 `tests/CardPicker2.IntegrationTests/Browser/DrawModeResponsiveAccessibilityTests.cs`，覆蓋 390x844、768x1024、1366x768、`prefers-reduced-motion: reduce`、鍵盤操作與 axe 基本檢查。
- [ ] T053 [P] [US3] 新增文案邊界失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawCopyBoundaryTests.cs`，掃描 `CardPicker2/Resources/SharedResource.zh-TW.resx` 與 `CardPicker2/Resources/SharedResource.en-US.resx` 不含保底、下一次機率更高、連抽加成、付費或價值分級暗示。
- [ ] T054 [US3] 執行 `dotnet test CardPicker2.sln --filter "CardDeletionRetention|DeletedCardStatistics|DrawModeLocalizationInvariant|DrawModeResponsiveAccessibility|DrawCopyBoundary"`，確認 `CardPicker2.sln` 的 US3 新測試在實作前失敗。

### 使用者故事 3 的實作

- [ ] T055 [US3] 更新卡牌模型生命週期於 `CardPicker2/Models/MealCard.cs`，新增 `Status`、`DeletedAtUtc`、active/deleted helper、normalize preserving lifecycle，並補 XML 文件註解含 `<example>`/`<code>`。
- [ ] T056 [US3] 更新重複偵測於 `CardPicker2/Services/DuplicateCardDetector.cs`，只比對 active cards，讓 retained deleted card 不阻擋新增同餐別同內容的新 active card。
- [ ] T057 [US3] 更新 card mutation/search/draw 流程於 `CardPicker2/Services/CardLibraryService.cs`，search/find/update 預設排除 deleted，delete 依 history hard delete 或 retained deleted，candidate pool 永遠只取 active。
- [ ] T058 [US3] 更新卡牌列表 PageModel 與 Razor UI 於 `CardPicker2/Pages/Cards/Index.cshtml.cs` 與 `CardPicker2/Pages/Cards/Index.cshtml`，確保一般 browsing/search 只顯示 active cards 並保留 blocked recovery 行為。
- [ ] T059 [US3] 更新卡牌 details/edit/delete PageModels 於 `CardPicker2/Pages/Cards/Details.cshtml.cs`、`CardPicker2/Pages/Cards/Edit.cshtml.cs`、`CardPicker2/Pages/Cards/Delete.cshtml.cs`，讓 deleted 或不存在目標回傳 not found/recovery message，不重新啟用 deleted card。
- [ ] T060 [US3] 更新卡牌 details/edit/delete Razor UI 於 `CardPicker2/Pages/Cards/Details.cshtml`、`CardPicker2/Pages/Cards/Edit.cshtml`、`CardPicker2/Pages/Cards/Delete.cshtml`，讓刪除成功/失敗與 recovery 訊息依目前語系呈現且不泄漏內部資料。
- [ ] T061 [US3] 更新 localized projection 於 `CardPicker2/Models/LocalizedMealCardView.cs` 與 `CardPicker2/Services/MealCardLocalizationService.cs`，加入 card status display/fallback 資訊供結果與統計列使用。
- [ ] T062 [US3] 更新首頁 result restore 於 `CardPicker2/Pages/Index.cshtml.cs`，依 operation/history/card ID 重顯既有成功結果，若對應 card retained deleted 則仍可顯示並標示狀態，不重新 randomize。
- [ ] T063 [P] [US3] 更新繁中文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，加入 deleted status、retained delete 成功/失敗、reduced motion 靜態揭示與不誤導機率的鼓勵文案。
- [ ] T064 [P] [US3] 更新英文文案於 `CardPicker2/Resources/SharedResource.en-US.resx`，加入 US3 對應文字並避免賭博式或機率提升暗示。
- [ ] T065 [P] [US3] 更新 reduced motion 與 responsive 樣式於 `CardPicker2/wwwroot/css/site.css`，確保 slot visual、結果卡、統計表與 deleted badge 在指定 viewport 不重疊。
- [ ] T066 [P] [US3] 更新 reduced motion 與互動輔助於 `CardPicker2/wwwroot/js/site.js`，在 `prefers-reduced-motion: reduce` 時跳過連續旋轉但不改變 submit payload 或結果身分。
- [ ] T067 [US3] 執行 `dotnet test CardPicker2.sln --filter "CardDeletionRetention|DeletedCardStatistics|DrawModeLocalizationInvariant|DrawModeResponsiveAccessibility|DrawCopyBoundary"`，確認 `CardPicker2.sln` 的 US3 測試通過。

**檢查點**: 所有使用者故事均可獨立驗證，且卡牌生命週期、語系與呈現層不影響抽卡公平性或統計。

---

## 階段 6: 潤飾與跨領域關注點

**目的**: 補齊效能、公開介面邊界、安全、日誌、公開 API 文件註解、完整驗證與手動檢查。

- [ ] T068 [P] 擴充安全標頭與 Anti-Forgery 測試於 `tests/CardPicker2.IntegrationTests/SecurityHeadersTests.cs`，覆蓋 production HSTS/CSP、`POST /?handler=Draw` Anti-Forgery、CSP 不新增不必要外部來源。
- [ ] T069 [P] 新增 draw/statistics/delete 結構化日誌測試於 `tests/CardPicker2.UnitTests/Services/DrawLoggingTests.cs`，覆蓋 draw success、repeat replay、validation failure、empty pool、blocked library、write failure、retained deleted 且不記錄完整 JSON 或秘密。
- [ ] T070 更新安全與日誌 wiring 於 `CardPicker2/Program.cs`，確認 production CSP/HSTS 保留、Serilog 設定不輸出敏感內容，且新增服務 lifetime 不造成 scoped/singleton 錯配。
- [ ] T071 [P] 更新公開 API 文件測試於 `tests/CardPicker2.UnitTests/Documentation/PublicApiDocumentationTests.cs`，要求新增或變更的 public models/services XML docs 包含 `<summary>`、`<example>` 與 `<code>`。
- [ ] T072 更新所有新增或變更公開 API 的 XML 文件註解，至少涵蓋 `CardPicker2/Models/DrawMode.cs`、`CardPicker2/Models/CardStatus.cs`、`CardPicker2/Models/DrawOperation.cs`、`CardPicker2/Models/DrawHistoryRecord.cs`、`CardPicker2/Models/CardDrawStatistic.cs`、`CardPicker2/Models/DrawStatisticsSummary.cs`、`CardPicker2/Models/DrawResult.cs`、`CardPicker2/Models/MealCard.cs`、`CardPicker2/Models/CardLibraryDocument.cs`、`CardPicker2/Services/ICardLibraryService.cs`、`CardPicker2/Services/CardLibraryService.cs`、`CardPicker2/Services/CardLibraryFileCoordinator.cs`、`CardPicker2/Services/DrawCandidatePoolBuilder.cs`、`CardPicker2/Services/DrawStatisticsService.cs`、`CardPicker2/Services/MealCardLocalizationService.cs`，並以 T071 測試防止漏補。
- [ ] T073 [P] 新增效能預算驗證於 `tests/CardPicker2.IntegrationTests/Performance/DrawModePerformanceTests.cs`，覆蓋首頁 GET、draw POST 與 statistics projection 在本機 JSON fixture 下 p95 < 200ms，並以 browser automation 或手動紀錄驗證 FCP < 1.5 秒、LCP < 2.5 秒、模式切換、搜尋、抽卡提交與統計更新回應 < 1 秒。
- [ ] T074 [P] 新增公開介面邊界測試於 `tests/CardPicker2.IntegrationTests/RouteSurfaceTests.cs`，確認本功能未新增面向外部系統的 JSON/API endpoint，公開介面維持 Razor Pages、表單、query strings、page handlers 與狀態碼。
- [ ] T075 執行 `dotnet build CardPicker2.sln`，確認 `CardPicker2.sln` 無新增 build warning 或 formatting/naming 違規。
- [ ] T076 執行 `dotnet test CardPicker2.sln`，確認 `CardPicker2.sln` 全部單元、整合、browser/security/performance/route-surface tests 通過。
- [ ] T077 依 `specs/004-draw-mode-statistics/quickstart.md` 完成手動或 browser automation 驗證，覆蓋 normal mode、random mode、重複提交、統計表、刪除後歷史保留、改名/翻譯更新、缺檔/corrupted 隔離資料、reduced motion、RWD、效能、安全與觀察性。

---

## 相依性與執行順序

### 階段相依性

- **階段 1 設定**: 無依賴，可立即開始。
- **階段 2 基礎建設**: 依賴階段 1，且封鎖所有 user story。
- **階段 3 US1**: 依賴階段 2，是 MVP。
- **階段 4 US2**: 依賴階段 2；可在 US1 服務契約穩定後與 US3 部分平行，但最終需要與 US1 draw history 寫入整合。
- **階段 5 US3**: 依賴階段 2；刪除後統計呈現需要 US2 統計模型，候選池排除 deleted 需要 US1 candidate pool。
- **階段 6 潤飾**: 依賴欲交付的 user stories 完成。

### 使用者故事相依性

- **US1 (P1)**: 可在 foundation 後開始，提供 MVP；不依賴 US2/US3。
- **US2 (P2)**: 依賴 foundation 的 schema v3/history models；與 US1 的成功 history append 整合後完整驗證。
- **US3 (P3)**: 依賴 foundation 的 card status/history models；deleted statistics row 需與 US2 統計投影整合，deleted candidate exclusion 需與 US1 candidate pool 整合。

### 相依圖

```text
階段 1 設定
  -> 階段 2 基礎建設
      -> US1 抽卡模式與 idempotency
      -> US2 統計投影與首頁表格
      -> US3 刪除保留與呈現不變性
          -> 階段 6 潤飾
```

### 每個使用者故事內部順序

- 測試任務必須先完成並確認失敗。
- 模型先於服務，服務先於 PageModel，PageModel 先於 Razor UI。
- Resource、CSS、JS 可在服務/PageModel 行為明確後平行更新。
- 每個檢查點後執行該 story 的 filter tests，再進入下一優先級。

---

## 平行處理機會

- T003 與 T004 可平行建立測試輔助工具。
- T005 與 T006 可平行撰寫 foundation 失敗測試。
- T007 到 T012 可平行建立互不依賴的模型檔案。
- US1 的 T019 到 T023 可平行撰寫測試；T031 到 T034 可在 T029/T030 介面確定後平行處理 resources、JS、CSS。
- US2 的 T036 到 T038 可平行撰寫測試；T045 到 T047 可在 T044 的統計 UI 結構確定後平行處理。
- US3 的 T049 到 T053 可平行撰寫測試；T063 到 T066 可在 lifecycle 與 UI 狀態明確後平行處理。
- T068、T069、T071、T073、T074 可平行補齊跨領域測試。

---

## 平行範例: 使用者故事 1

```bash
# 平行撰寫 US1 失敗測試:
任務: "T019 [US1] tests/CardPicker2.UnitTests/Models/DrawModeTests.cs"
任務: "T020 [US1] tests/CardPicker2.UnitTests/Services/DrawCandidatePoolBuilderTests.cs"
任務: "T021 [US1] tests/CardPicker2.UnitTests/Services/CardLibraryDrawModeTests.cs"
任務: "T022 [US1] tests/CardPicker2.UnitTests/Services/DrawIdempotencyTests.cs"
任務: "T023 [US1] tests/CardPicker2.IntegrationTests/Pages/DrawModePageTests.cs"

# 契約穩定後平行處理 UI 文案與資產:
任務: "T031 [US1] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T032 [US1] CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T033 [US1] CardPicker2/wwwroot/js/site.js"
任務: "T034 [US1] CardPicker2/wwwroot/css/site.css"
```

## 平行範例: 使用者故事 2

```bash
# 平行撰寫 US2 失敗測試:
任務: "T036 [US2] tests/CardPicker2.UnitTests/Services/DrawStatisticsServiceTests.cs"
任務: "T037 [US2] tests/CardPicker2.IntegrationTests/Pages/DrawStatisticsPageTests.cs"
任務: "T038 [US2] tests/CardPicker2.IntegrationTests/Pages/DrawHistoryPersistenceTests.cs"

# 統計 UI 文案與 responsive 工作:
任務: "T045 [US2] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T046 [US2] CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T047 [US2] CardPicker2/wwwroot/css/site.css"
```

## 平行範例: 使用者故事 3

```bash
# 平行撰寫 US3 失敗測試:
任務: "T049 [US3] tests/CardPicker2.UnitTests/Services/CardDeletionRetentionTests.cs"
任務: "T050 [US3] tests/CardPicker2.IntegrationTests/Pages/DeletedCardStatisticsPageTests.cs"
任務: "T051 [US3] tests/CardPicker2.IntegrationTests/Pages/DrawModeLocalizationInvariantTests.cs"
任務: "T052 [US3] tests/CardPicker2.IntegrationTests/Browser/DrawModeResponsiveAccessibilityTests.cs"
任務: "T053 [US3] tests/CardPicker2.UnitTests/Services/DrawCopyBoundaryTests.cs"

# 呈現層可平行處理:
任務: "T063 [US3] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T064 [US3] CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T065 [US3] CardPicker2/wwwroot/css/site.css"
任務: "T066 [US3] CardPicker2/wwwroot/js/site.js"
```

---

## 實作策略

### MVP 優先 (僅使用者故事 1)

1. 完成階段 1 設定。
2. 完成階段 2 基礎建設。
3. 完成階段 3 US1。
4. 停下並驗證 `dotnet test CardPicker2.sln --filter "DrawMode|DrawIdempotency|DrawPage"`。
5. 可展示 normal/random 抽卡、公平候選池、idempotent replay 與 persisted draw history。

### 增量交付

1. 設定與基礎建設: schema v3、history、status、file coordinator。
2. US1: mode-based draw 與 idempotency，形成 MVP。
3. US2: statistics summary/table 與 restart persistence。
4. US3: deleted retention、translation/lifecycle invariants、reduced motion/RWD。
5. 潤飾: security/logging/XML docs/full test/manual verification。

### 品質閘門

- 每個 behavior task 的測試先於 implementation。
- `dotnet test CardPicker2.sln --filter ...` 在每個 story checkpoint 通過。
- 最終 `dotnet build CardPicker2.sln` 與 `dotnet test CardPicker2.sln` 通過。
- 所有使用者可見 runtime UI 在 `zh-TW` 與 `en-US` 不顯示未翻譯 key。
- `CardPicker2/data/cards.json` corrupted/unsupported 時原檔保留，且 create/edit/delete/draw/statistics 都進入 blocked recovery。
- production HSTS/CSP、Anti-Forgery、效能預算、公開介面邊界、結構化日誌與敏感資訊禁止輸出皆有測試證據。

---

## 獨立測試條件摘要

- **US1**: Normal mode 只抽所選餐別 active cards；Random mode 抽全部 active cards 且忽略 meal type；空卡池、blocked、未投幣、invalid input 不成功；同 operation replay 不新增 history。
- **US2**: 首頁 total、draw count、historical probability 完全由成功 history 計算；無歷史顯示空狀態；失敗與 replay 不改變統計；重啟後統計保留。
- **US3**: 曾抽中卡牌刪除後 retained as deleted 且排除未來候選池；改名/翻譯更新不切分統計；語系、動畫、reduced motion、文案不改變 card ID、候選池或機率。

## 建議 MVP 範圍

MVP 範圍為階段 1、階段 2、階段 3，也就是 US1「依抽卡模式取得公平結果」。完成後即可交付 normal/random 抽卡、schema v3 history append 與 idempotent replay；US2/US3 可作為後續增量。
