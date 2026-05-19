# 任務: 餐點輪替防重複抽卡

**輸入**: `specs/006-card-rotation-cooldown/` 的 `spec.md`、`plan.md`、`research.md`、`data-model.md`、`quickstart.md`、`contracts/ui-contract.md`，並以 `specs/005-card-metadata-filtered-draw/` 的 metadata filtered candidate pool 與 `specs/004-draw-mode-statistics/` 的 draw mode、history、statistics、deleted-card retention、idempotency 作為既有實作背景。
**先決條件**: ASP.NET Core Razor Pages app、schema v4 `CardPicker2/data/cards.json`、004/005 功能已實作、xUnit/Moq/WebApplicationFactory/browser 測試專案。
**測試**: 憲章與本功能 quickstart 明確要求測試優先，因此每個行為故事都先建立失敗測試，再實作。
**組織方式**: 任務依使用者故事分組，讓 P1 可作為 MVP 獨立交付；P2 與 P3 在 foundation 完成後可按風險與容量平行推進。

## 格式: `[ID] [P?] [Story] 任務描述`

- **[P]**: 可平行處理，因為任務修改不同檔案且不依賴同階段其他未完成任務。
- **[Story]**: 僅使用於使用者故事階段，對應 `spec.md` 的 US1、US2、US3。
- 每個任務描述都包含具體檔案路徑或驗證命令目標。

---

## 階段 1: 設定 (共用基礎設施)

**目的**: 確認目前 005 baseline 可建置，並補齊輪替防重複測試所需 fixture、factory 與 HTML assertion helper。

- [ ] T001 執行 `dotnet restore CardPicker2.sln`，確認 `CardPicker2.sln` 目前可還原。
- [ ] T002 執行 `dotnet build CardPicker2.sln`，記錄 `CardPicker2.sln` 實作前 build baseline。
- [ ] T003 [P] 擴充輪替測試資料建構器於 `tests/CardPicker2.UnitTests/Services/DrawFeatureTestData.cs`，加入含 `rotationSnapshot` 與缺少 `rotationSnapshot` 的 schema v4 draw history、同 timestamp 不同持久化順序、deleted card history 與 deterministic operation IDs。
- [ ] T004 [P] 擴充整合測試 factory 於 `tests/CardPicker2.IntegrationTests/Infrastructure/DrawFeatureWebApplicationFactory.cs`，支援 rotation form fields、same-operation replay payload、culture cookie、Anti-Forgery token 與 production environment 切換。
- [ ] T005 [P] 建立輪替首頁 HTML assertion helper 於 `tests/CardPicker2.IntegrationTests/Pages/RotationCooldownHtmlAssertions.cs`，支援 cooldown toggle、N input、success summary、empty-after-rotation alert、old-history snapshot fallback 與 localized validation 驗證。

---

## 階段 2: 基礎建設 (阻斷性先決條件)

**目的**: 建立輪替設定、快照、候選池投影、近期歷史排序、排除集合、文件驗證與雙語資源基礎；所有 user story 都依賴此階段。

**關鍵限制**: 未完成本階段前，不要開始 US1/US2/US3 的實作任務。

- [ ] T006 [P] 新增輪替設定模型失敗測試於 `tests/CardPicker2.UnitTests/Models/RotationCooldownSettingsTests.cs`，覆蓋預設啟用 N=3、有效範圍 0..10、N=0 等同停用、負數/超過上限/無法 bind 拒絕。
- [ ] T007 [P] 新增輪替快照模型失敗測試於 `tests/CardPicker2.UnitTests/Models/RotationSnapshotTests.cs`，覆蓋非負 count、`PostRotationCandidateCount = PreRotationCandidateCount - ExcludedCandidateCount`、缺少快照合法但存在時必須有效。
- [ ] T008 [P] 新增輪替服務基礎失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawRotationCooldownServiceTests.cs`，覆蓋最近 N 筆成功歷史排序、同 timestamp 以持久化順序較後者較新、card ID 去重、deleted/missing IDs 只在存在於候選池時排除。
- [ ] T009 [P] 新增 schema v4 optional 快照相容失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibrarySchemaVersionTests.cs`，覆蓋 existing history missing `rotationSnapshot` 不 blocked、不回填，invalid snapshot count equation 會 block 並保留原檔。
- [ ] T010 [P] 新增輪替雙語 resource 失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/LocalizationResourceTests.cs`，覆蓋 `zh-TW` 與 `en-US` cooldown labels、N range hint、success summary、empty-after-rotation、invalid N、old-history-missing-snapshot keys 不缺漏。

**Foundation 紅燈檢查**: 在開始 T011 到 T022 任一實作任務前，先執行 `dotnet test CardPicker2.sln --filter "RotationCooldownSettings|RotationSnapshot|DrawRotationCooldownService|SchemaV4|LocalizationResource"`，確認 T006 到 T010 新增測試因尚未實作而失敗；若測試未失敗或失敗原因不符合預期，先修正測試設計再進入實作。

- [ ] T011 [P] 建立輪替設定模型於 `CardPicker2/Models/RotationCooldownSettings.cs`，包含 `AvoidRecentRepeats`、`RecentDrawCount`、`IsActive`、default factory 與 0..10 validation，並補 XML 文件註解含 `<example>`/`<code>`。
- [ ] T012 [P] 建立輪替快照模型於 `CardPicker2/Models/RotationSnapshot.cs`，包含是否啟用、N 值、輪替前候選池數、排除數、輪替後候選池數與 validation helper，並補 XML 文件註解含 `<example>`/`<code>`。
- [ ] T013 [P] 建立輪替候選池模型於 `CardPicker2/Models/RotationCandidatePool.cs`，包含 pre-rotation cards、post-rotation cards、excluded card IDs、settings、snapshot 與 nominal probability helpers。
- [ ] T014 [P] 建立候選池空狀態原因模型於 `CardPicker2/Models/CandidatePoolEmptyReason.cs`，區分 `BaseCandidatePoolEmpty`、`RotationCandidatePoolEmpty` 與 `InvalidRotationSettings`，並補 XML 文件註解含 `<example>`/`<code>`。
- [ ] T015 更新成功抽卡歷史模型於 `CardPicker2/Models/DrawHistoryRecord.cs`，新增 optional `RotationSnapshot`，確保 legacy missing snapshot 仍合法且新 history 可保存 non-null snapshot。
- [ ] T016 更新抽卡操作模型於 `CardPicker2/Models/DrawOperation.cs`，新增 `RotationCooldown` 設定並讓缺漏 form 值使用 default `RotationCooldownSettings`。
- [ ] T017 更新抽卡結果模型於 `CardPicker2/Models/DrawResult.cs`，新增 `RotationSettings`、`RotationSnapshot`、`CandidatePoolEmptyReason`、`RotationSummaryKey` 與 replay snapshot projection。
- [ ] T018 更新根文件驗證於 `CardPicker2/Models/CardLibraryDocument.cs`，保持 `CurrentSchemaVersion = 4` 並允許 missing `rotationSnapshot`，但在存在快照時套用 count validation。
- [ ] T019 建立輪替防重複服務於 `CardPicker2/Services/DrawRotationCooldownService.cs`，實作 recent N history projection、card ID exclusion set、candidate filtering、snapshot 建立與 empty-after-rotation 判斷。
- [ ] T020 更新 DI 註冊於 `CardPicker2/Program.cs`，註冊 `DrawRotationCooldownService` 或其介面並確認 singleton/scoped lifetime 不與 `CardLibraryFileCoordinator` 衝突。
- [ ] T021 [P] 新增或更新繁中輪替共用文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 cooldown labels、N hints、invalid N、success summary、empty-after-rotation、old-history-missing-snapshot 與 logging-safe status keys。
- [ ] T022 [P] 新增或更新英文輪替共用文案於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保 `en-US` cooldown UI、summary、empty states 與 validation 不顯示未翻譯 key。
- [ ] T023 執行 `dotnet test CardPicker2.sln --filter "RotationCooldownSettings|RotationSnapshot|DrawRotationCooldownService|SchemaV4|LocalizationResource"`，確認 `CardPicker2.sln` 的 foundation 測試從失敗轉為通過。

**檢查點**: 輪替模型、service、schema 相容性與 resource foundation 完成，user story implementation 可開始。

---

## 階段 3: 使用者故事 1 - 避免近期重複並維持公平抽卡 (優先級: P1) MVP

**目標**: 首頁預設啟用避免最近重複且 N=3；Normal/Random + metadata filters 先建立 005 候選池，再依最近 N 筆成功 history 排除近期 card IDs；輪替後非空時仍等機率抽卡；同一 operation replay 原 result 與原 snapshot。

**獨立測試**: 使用具已知 history 與 metadata 的 active cards，POST Normal/Random rotation draw，驗證排除 membership、`1/M` 標稱機率、N=0/關閉回到 005 規則、idempotent replay 與 Anti-Forgery。

### 使用者故事 1 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T024 [P] [US1] 新增輪替候選池 membership 失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawRotationCooldownServiceTests.cs`，覆蓋 Normal 先餐別再 metadata 再 rotation、Random 忽略餐別再 metadata 再 rotation、最近 N 筆同 card ID 只排除一次。
- [ ] T025 [P] [US1] 新增輪替抽卡服務失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryRotationDrawTests.cs`，覆蓋啟用 N=3 排除近期候選、N=0 與關閉防重複完全回到 005 candidate pool、輪替後 M 張時每張標稱機率 `1/M`。
- [ ] T026 [P] [US1] 新增輪替 replay 失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawIdempotencyRotationTests.cs`，覆蓋相同 `DrawOperationId` replay 原 card ID 與原 `RotationSnapshot`，不得因目前 N、最新 history 或最新 filters 重新抽卡。
- [ ] T027 [P] [US1] 新增首頁輪替抽卡整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/RotationCooldownDrawPageTests.cs`，覆蓋 GET 預設 toggle 啟用、N=3、新首頁 GET/應用程式重新啟動後不保留先前防重複偏好、POST Normal/Random cooldown fields、僅顯示數量的 success summary、Anti-Forgery 與 blocked state disable draw。
- [ ] T028 [US1] 執行 `dotnet test CardPicker2.sln --filter "DrawRotationCooldownService|CardLibraryRotationDraw|DrawIdempotencyRotation|RotationCooldownDrawPage"`，確認 `CardPicker2.sln` 的 US1 新測試在實作前失敗。

### 使用者故事 1 的實作

- [ ] T029 [US1] 更新候選池建構器於 `CardPicker2/Services/DrawCandidatePoolBuilder.cs`，讓服務可回傳 005 base + metadata filtered pool 給後續輪替服務使用且不混入 randomizer 權重。
- [ ] T030 [US1] 更新抽卡服務契約於 `CardPicker2/Services/ICardLibraryService.cs`，明確支援含 `RotationCooldownSettings` 的 `DrawOperation` 與 replay `RotationSnapshot` 回傳。
- [ ] T031 [US1] 更新抽卡核心流程於 `CardPicker2/Services/CardLibraryService.cs`，在 replay 檢查之後建立 005 pool、套用 `DrawRotationCooldownService`、從 post-rotation pool uniform random、append `DrawHistoryRecord` + `RotationSnapshot` 同一 atomic write。
- [ ] T032 [US1] 更新首頁 PageModel 於 `CardPicker2/Pages/Index.cshtml.cs`，bind `avoidRecentRepeats` 與 `recentDrawCount`，server-side 驗證 0..10，建立 `RotationCooldownSettings` 並保存 draw failure/success form state。
- [ ] T033 [US1] 更新首頁 Razor UI 於 `CardPicker2/Pages/Index.cshtml`，加入「避免最近重複」toggle、N input、range hint、僅顯示數量的 success rotation summary 與 hidden/replay state，且 Random mode 不使用 meal type 限制。
- [ ] T034 [US1] 更新 localized card/result projection 於 `CardPicker2/Services/MealCardLocalizationService.cs`，加入 success rotation summary display model 與 `RotationSnapshot` count formatting。
- [ ] T035 [P] [US1] 新增或更新繁中首頁輪替文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 default enabled label、N=0 說明、套用摘要、排除數與輪替後候選池大小。
- [ ] T036 [P] [US1] 新增或更新英文首頁輪替文案於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保 success summary 在英文語系下無未翻譯 key。
- [ ] T037 [P] [US1] 更新首頁輪替 responsive 樣式於 `CardPicker2/wwwroot/css/site.css`，確保 cooldown control、N input、filter panel、slot/result summary 在 390x844、768x1024、1366x768 不重疊或水平溢出。
- [ ] T038 [P] [US1] 更新首頁輪替 progressive enhancement 於 `CardPicker2/wwwroot/js/site.js`，支援語系/主題切換前暫存 cooldown toggle 與 N 值、N input UX guard、快速連點 guard，且不決定候選池或結果，也不得把 cooldown 設定保存為跨頁面或跨重新啟動偏好。
- [ ] T039 [US1] 執行 `dotnet test CardPicker2.sln --filter "DrawRotationCooldownService|CardLibraryRotationDraw|DrawIdempotencyRotation|RotationCooldownDrawPage"`，確認 `CardPicker2.sln` 的 US1 測試通過。

**檢查點**: US1 可作為 MVP 獨立展示，包含預設啟用 N=3、metadata 後輪替排除、公平 post-rotation pool 與 replay snapshot。

---

## 階段 4: 使用者故事 2 - 防重複後無可抽餐點時引導放寬條件 (優先級: P2)

**目標**: 清楚區分 005 base/metadata pool 原本為空與 rotation 後為空；rotation empty 不自動放寬、不抽近期卡、不新增 history/statistics，並提示降低 N、關閉防重複或調整條件；invalid N 拒絕抽卡。

**獨立測試**: 使用原始候選池非空但全被最近 N 筆 history 排除的資料，驗證不產生成功結果、不新增 history、不改變 statistics，並顯示目前語系的可操作放寬提示。

### 使用者故事 2 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T040 [P] [US2] 新增 rotation empty 服務失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryRotationDrawTests.cs`，覆蓋 pre-rotation pool 非空但 post-rotation pool 空時不新增 `DrawHistoryRecord`、不呼叫 randomizer、不改變 statistics。
- [ ] T041 [P] [US2] 新增 invalid N 服務失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryRotationValidationTests.cs`，覆蓋負數、超過 10、非數字 bind failure 不新增 history、不持久化設定、不套用自動修正。
- [ ] T042 [P] [US2] 新增首頁 rotation empty 整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/RotationCooldownDrawPageTests.cs`，覆蓋 base empty 使用既有 empty message、rotation empty 使用放寬提示、invalid N 顯示 validation message。
- [ ] T043 [P] [US2] 新增 rotation empty 統計不變整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/RotationCooldownStatisticsTests.cs`，覆蓋總成功抽卡次數與單卡抽中次數在 empty-after-rotation 與 invalid N 後不變。
- [ ] T044 [US2] 執行 `dotnet test CardPicker2.sln --filter "CardLibraryRotationDraw|CardLibraryRotationValidation|RotationCooldownDrawPage|RotationCooldownStatistics"`，確認 `CardPicker2.sln` 的 US2 新測試在實作前失敗。

### 使用者故事 2 的實作

- [ ] T045 [US2] 更新抽卡核心失敗分支於 `CardPicker2/Services/CardLibraryService.cs`，在 pre-rotation pool empty 時回傳既有 empty reason，在 post-rotation empty 時回傳 `RotationCandidatePoolEmpty` 並跳過 randomizer/history write。
- [ ] T046 [US2] 更新輪替設定 validation 於 `CardPicker2/Models/RotationCooldownSettings.cs` 與 `CardPicker2/Pages/Index.cshtml.cs`，讓 invalid N 進入 ModelState 並阻止 service draw success。
- [ ] T047 [US2] 更新抽卡結果模型於 `CardPicker2/Models/DrawResult.cs`，讓 `CandidatePoolEmptyReason` 與 localized status key 可區分 base empty、rotation empty、invalid rotation settings 與 blocked library。
- [ ] T048 [US2] 更新首頁 Razor UI 於 `CardPicker2/Pages/Index.cshtml`，在 rotation empty 顯示降低 N、關閉避免最近重複或調整其他條件的可操作提示，且不顯示成功 result card。
- [ ] T049 [P] [US2] 新增或更新繁中放寬提示與 validation 文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 rotation empty、invalid N、N range 與 base empty 區別文案。
- [ ] T050 [P] [US2] 新增或更新英文放寬提示與 validation 文案於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保 empty/validation 狀態在 `en-US` 無未翻譯 key。
- [ ] T051 [P] [US2] 更新 empty/validation alert 樣式於 `CardPicker2/wwwroot/css/site.css`，確保放寬提示、N input error、filter chips 與 button group 在指定 viewport 不重疊且不只依賴顏色。
- [ ] T052 [US2] 執行 `dotnet test CardPicker2.sln --filter "CardLibraryRotationDraw|CardLibraryRotationValidation|RotationCooldownDrawPage|RotationCooldownStatistics"`，確認 `CardPicker2.sln` 的 US2 測試通過。

**檢查點**: US1 與 US2 可同時運作，rotation empty 與 base empty 可被區分且都不污染 history/statistics。

---

## 階段 5: 使用者故事 3 - 看懂本次輪替規則與歷史一致性 (優先級: P3)

**目標**: 成功結果與 replay 顯示持久化 `RotationSnapshot` 的摘要；語系/主題/reduced motion/card edit/delete 只改變呈現，不改變 result、history、statistics 或 snapshot；舊 history 缺 snapshot 不 blocked、不回填、不重算。

**獨立測試**: 完成成功抽卡後重送、重新整理、切換語系、刪除或改名近期卡牌，驗證 result card ID、snapshot counts、history/statistics 與最近 N 次排除都維持規格定義。

### 使用者故事 3 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T053 [P] [US3] 新增輪替快照持久化失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryRotationSnapshotTests.cs`，覆蓋成功 draw 保存 non-null snapshot、關閉防重複與 N=0 也保存 count 摘要、write failure 不宣告成功。
- [ ] T054 [P] [US3] 新增舊 history 相容失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawStatisticsRotationCompatibilityTests.cs`，覆蓋 missing snapshot history 不 blocked、不回填、仍納入統計與最近 N 次排除。
- [ ] T055 [P] [US3] 新增 card edit/delete 後 ID 排除失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawRotationCooldownServiceTests.cs`，覆蓋近期 active card 改名/metadata 更新後仍依同 card ID 排除，deleted recent card 不進候選池也不排除其他 active card。
- [ ] T056 [P] [US3] 新增 replay 與語系切換整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/RotationCooldownReplayTests.cs`，覆蓋 operation replay 使用原 snapshot、result restore 不重算、missing snapshot 顯示舊紀錄提示。
- [ ] T057 [P] [US3] 新增輪替語系不變性整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/RotationCooldownLocalizationTests.cs`，覆蓋 `zh-TW`/`en-US` 切換只改變文案，不改變 card ID、operation ID、snapshot、history 或 statistics。
- [ ] T058 [P] [US3] 新增輪替 RWD/reduced-motion browser 失敗測試於 `tests/CardPicker2.IntegrationTests/Browser/RotationCooldownResponsiveAccessibilityTests.cs`，覆蓋 390x844、768x1024、1366x768、兩語系、`prefers-reduced-motion: reduce`、鍵盤操作與無水平溢出。
- [ ] T059 [US3] 執行 `dotnet test CardPicker2.sln --filter "CardLibraryRotationSnapshot|DrawStatisticsRotationCompatibility|RotationCooldownReplay|RotationCooldownLocalization|RotationCooldownResponsiveAccessibility"`，確認 `CardPicker2.sln` 的 US3 新測試在實作前失敗。

### 使用者故事 3 的實作

- [ ] T060 [US3] 更新抽卡 history append 於 `CardPicker2/Services/CardLibraryService.cs`，確保每次新成功 draw 都保存 non-null `RotationSnapshot` 並與結果同一 atomic write 成立。
- [ ] T061 [US3] 更新 replay/result restore 流程於 `CardPicker2/Pages/Index.cshtml.cs`，依 persisted history snapshot 重顯摘要，missing snapshot 顯示舊紀錄提示且不得依目前資料重算。
- [ ] T062 [US3] 更新統計服務相容性於 `CardPicker2/Services/DrawStatisticsService.cs`，確認 `RotationSnapshot` 不參與統計公式且 missing snapshot history 仍計入總數與單卡抽中次數。
- [ ] T063 [US3] 更新本地化投影於 `CardPicker2/Models/LocalizedMealCardView.cs` 與 `CardPicker2/Services/MealCardLocalizationService.cs`，支援 deleted result card、old-history-missing-snapshot 與 snapshot summary 的雙語呈現。
- [ ] T064 [US3] 更新首頁結果摘要 Razor UI 於 `CardPicker2/Pages/Index.cshtml`，用 definition list 或 stable summary chips 顯示 snapshot counts、排除數與輪替後候選池大小，維持僅顯示數量的呈現且不列出被排除卡牌名稱，並標示舊紀錄缺少輪替摘要。
- [ ] T065 [P] [US3] 新增或更新繁中歷史一致性文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 replay snapshot、missing snapshot、deleted result card 與不暗示加權的摘要文字。
- [ ] T066 [P] [US3] 新增或更新英文歷史一致性文案於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保 replay 與 missing snapshot 呈現在 `en-US` 無未翻譯 key。
- [ ] T067 [P] [US3] 更新輪替摘要與 reduced-motion 樣式於 `CardPicker2/wwwroot/css/site.css`，確保 summary chips、deleted badge、validation state 與 result card 在 light/dark theme 可讀且不重疊。
- [ ] T068 [P] [US3] 更新 reduced-motion 與狀態保留腳本於 `CardPicker2/wwwroot/js/site.js`，確保 reduced motion 跳過連續旋轉但不修改 submit payload、operation ID、candidate pool 或 snapshot。
- [ ] T069 [US3] 執行 `dotnet test CardPicker2.sln --filter "CardLibraryRotationSnapshot|DrawStatisticsRotationCompatibility|RotationCooldownReplay|RotationCooldownLocalization|RotationCooldownResponsiveAccessibility"`，確認 `CardPicker2.sln` 的 US3 測試通過。

**檢查點**: 所有使用者故事均可獨立驗證，且 snapshot、history、statistics、語系、主題與呈現層不互相污染。

---

## 階段 6: 潤飾與跨領域關注點

**目的**: 補齊安全、可觀察性、效能、公開介面邊界、公開 C# model/service API 文件註解、coverage evidence、完整驗證與 quickstart 手動檢查。

- [ ] T070 [P] 擴充安全標頭與 Anti-Forgery 測試於 `tests/CardPicker2.IntegrationTests/SecurityHeadersTests.cs`，覆蓋 rotation draw form、language/theme forms、production HSTS/CSP 與不新增外部來源。
- [ ] T071 [P] 擴充輪替結構化日誌測試於 `tests/CardPicker2.UnitTests/Services/DrawLoggingTests.cs`，覆蓋 invalid N、rotation applied、empty-after-rotation、draw success with rotation counts、replay with/without snapshot、write failure，且不記錄完整 JSON、stack trace、系統提示或未清理 payload。
- [ ] T072 [P] 擴充公開 C# model/service API 文件測試於 `tests/CardPicker2.UnitTests/Documentation/PublicApiDocumentationTests.cs`，要求本功能新增或變更的 public models/services XML docs 包含 `<summary>`、`<example>` 與 `<code>`。
- [ ] T073 [P] 擴充公開介面邊界測試於 `tests/CardPicker2.IntegrationTests/RouteSurfaceTests.cs`，確認本功能未新增外部 JSON/API endpoint，公開介面維持 Razor Pages、表單、query strings、page handlers 與狀態碼。
- [ ] T074 [P] 新增輪替效能與 web-vitals 驗證於 `tests/CardPicker2.IntegrationTests/Performance/RotationCooldownPerformanceTests.cs`，覆蓋首頁 GET、metadata + rotation draw POST、statistics projection、`/Cards` filtered search 在至少 150 張 active cards + 1,000 筆 history fixture 下 p95 < 200ms、主要內容 1 秒內更新、FCP < 1.5 秒與 LCP < 2.5 秒。
- [ ] T075 [P] 新增輪替文案邊界測試於 `tests/CardPicker2.UnitTests/Services/DrawCopyBoundaryTests.cs`，掃描 `CardPicker2/Resources/SharedResource.zh-TW.resx` 與 `CardPicker2/Resources/SharedResource.en-US.resx` 不含保底、下一次機率提高、連抽加成、付費加成、偏好加權或價值分級暗示。
- [ ] T076 更新安全與 DI wiring 於 `CardPicker2/Program.cs`，確認 production CSP/HSTS 保留、輪替服務 lifetime 正確、Serilog 設定不輸出敏感內容。
- [ ] T077 更新新增 public C# model XML 文件註解於 `CardPicker2/Models/RotationCooldownSettings.cs`、`CardPicker2/Models/RotationSnapshot.cs`、`CardPicker2/Models/RotationCandidatePool.cs`、`CardPicker2/Models/CandidatePoolEmptyReason.cs`、`CardPicker2/Models/DrawHistoryRecord.cs`、`CardPicker2/Models/DrawOperation.cs`、`CardPicker2/Models/DrawResult.cs`。
- [ ] T078 更新新增或變更 public C# service XML 文件註解於 `CardPicker2/Services/DrawRotationCooldownService.cs`、`CardPicker2/Services/ICardLibraryService.cs`、`CardPicker2/Services/CardLibraryService.cs`、`CardPicker2/Services/DrawCandidatePoolBuilder.cs`、`CardPicker2/Services/DrawStatisticsService.cs`、`CardPicker2/Services/MealCardLocalizationService.cs`。
- [ ] T079 執行 `dotnet test CardPicker2.sln --filter "RotationCooldown|RotationSnapshot|DrawIdempotency|FilteredDraw|DrawStatistics|SecurityHeaders|AntiForgery|Logging|RouteSurface|RotationCooldownPerformance|WebVitals|RotationCooldownResponsiveAccessibility"`，確認 `CardPicker2.sln` 的 cross-cutting 測試通過。
- [ ] T080 執行 `dotnet build CardPicker2.sln`，確認 `CardPicker2.sln` 無新增 build warning、formatting 或 naming 違規。
- [ ] T081 執行 `dotnet test CardPicker2.sln`，確認 `CardPicker2.sln` 全部單元、整合、browser/security/performance/route-surface tests 通過。
- [ ] T082 執行 `dotnet test CardPicker2.sln --collect:"XPlat Code Coverage"`，檢查 coverage report 中本功能涉及的 critical business logic（`CardPicker2/Models/` 與 `CardPicker2/Services/` 的 rotation/filter/draw/history/persistence 路徑）覆蓋率達 80% 以上；若未達標，必須在 `specs/006-card-rotation-cooldown/plan.md` 記錄例外、風險與補救計畫後才可交付。
- [ ] T083 依 `specs/006-card-rotation-cooldown/quickstart.md` 完成手動或 browser automation 驗證，覆蓋預設防重複、關閉防重複與 N=0、防重複設定不跨頁面或跨重新啟動持久保存、隨機模式與 metadata filters、防重複後空候選池、原始候選池為空、重複提交與 snapshot replay、舊 history 缺 snapshot、deleted card、card edit、僅顯示數量的輪替摘要、語系/主題狀態保留、reduced motion、RWD、安全與觀察性。

---

## 相依性與執行順序

### 階段相依性

- **階段 1 設定**: 無依賴，可立即開始。
- **階段 2 基礎建設**: 依賴階段 1，且封鎖所有 user story。
- **階段 3 US1**: 依賴階段 2，是 MVP。
- **階段 4 US2**: 依賴階段 2；可在 US1 輪替服務契約穩定後與 US3 部分平行，但需共用 empty reason 與 validation。
- **階段 5 US3**: 依賴階段 2；snapshot persistence/replay 需與 US1 成功 draw 整合，missing snapshot 相容可獨立測試。
- **階段 6 潤飾**: 依賴欲交付的 user stories 完成。

### 使用者故事相依性

- **US1 (P1)**: 可在 foundation 後開始，提供 MVP；不依賴 US2/US3。
- **US2 (P2)**: 依賴 foundation 的 `RotationCooldownSettings`、`RotationCandidatePool` 與 empty reason；與 US1 共用 draw service，但 empty-after-rotation 行為可獨立驗證。
- **US3 (P3)**: 依賴 foundation 的 `RotationSnapshot` 與 optional history model；snapshot replay 需與 US1 成功 draw 整合，統計相容與 missing snapshot 可獨立驗證。

### 相依圖

```text
階段 1 設定
  -> 階段 2 基礎建設
      -> US1 近期排除與公平抽卡
      -> US2 防重複後空候選池與 invalid N
      -> US3 輪替摘要、snapshot replay 與歷史一致性
          -> 階段 6 潤飾
```

### 每個使用者故事內部順序

- 測試任務必須先完成並確認失敗。
- 模型先於服務，服務先於 PageModel，PageModel 先於 Razor UI。
- Resource、CSS、JS 可在服務/PageModel 行為明確後平行更新。
- 每個檢查點後執行該 story 的 filter tests，再進入下一優先級。

---

## 平行處理機會

- T003、T004、T005 可平行建立測試輔助工具。
- T006 到 T010 可平行撰寫 foundation 失敗測試。
- T011 到 T014 可平行建立互不依賴的輪替模型檔案。
- T021 與 T022 可平行補齊雙語 resource keys。
- US1 的 T024 到 T027 可平行撰寫測試；T035 到 T038 可在 T032/T033 的 UI contract 明確後平行處理 resources、CSS、JS。
- US2 的 T040 到 T043 可平行撰寫測試；T049 到 T051 可在 T048 的 empty UI contract 明確後平行處理 resources、CSS。
- US3 的 T053 到 T058 可平行撰寫測試；T065 到 T068 可在 T061/T064 的 replay/result UI contract 明確後平行處理 resources、CSS、JS。
- T070 到 T075 可平行補齊 cross-cutting 測試。

---

## 平行範例: 使用者故事 1

```bash
# 平行撰寫 US1 失敗測試:
任務: "T024 [US1] tests/CardPicker2.UnitTests/Services/DrawRotationCooldownServiceTests.cs"
任務: "T025 [US1] tests/CardPicker2.UnitTests/Services/CardLibraryRotationDrawTests.cs"
任務: "T026 [US1] tests/CardPicker2.UnitTests/Services/DrawIdempotencyRotationTests.cs"
任務: "T027 [US1] tests/CardPicker2.IntegrationTests/Pages/RotationCooldownDrawPageTests.cs"

# UI contract 穩定後平行處理文案與前端:
任務: "T035 [US1] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T036 [US1] CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T037 [US1] CardPicker2/wwwroot/css/site.css"
任務: "T038 [US1] CardPicker2/wwwroot/js/site.js"
```

## 平行範例: 使用者故事 2

```bash
# 平行撰寫 US2 失敗測試:
任務: "T040 [US2] tests/CardPicker2.UnitTests/Services/CardLibraryRotationDrawTests.cs"
任務: "T041 [US2] tests/CardPicker2.UnitTests/Services/CardLibraryRotationValidationTests.cs"
任務: "T042 [US2] tests/CardPicker2.IntegrationTests/Pages/RotationCooldownDrawPageTests.cs"
任務: "T043 [US2] tests/CardPicker2.IntegrationTests/Pages/RotationCooldownStatisticsTests.cs"

# 空候選池 UI contract 穩定後平行處理文案與樣式:
任務: "T049 CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T050 CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T051 CardPicker2/wwwroot/css/site.css"
```

## 平行範例: 使用者故事 3

```bash
# 平行撰寫 US3 失敗測試:
任務: "T053 [US3] tests/CardPicker2.UnitTests/Services/CardLibraryRotationSnapshotTests.cs"
任務: "T054 [US3] tests/CardPicker2.UnitTests/Services/DrawStatisticsRotationCompatibilityTests.cs"
任務: "T056 [US3] tests/CardPicker2.IntegrationTests/Pages/RotationCooldownReplayTests.cs"
任務: "T057 [US3] tests/CardPicker2.IntegrationTests/Pages/RotationCooldownLocalizationTests.cs"
任務: "T058 [US3] tests/CardPicker2.IntegrationTests/Browser/RotationCooldownResponsiveAccessibilityTests.cs"

# replay/result UI contract 穩定後平行處理文案與前端:
任務: "T065 CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T066 CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T067 CardPicker2/wwwroot/css/site.css"
任務: "T068 CardPicker2/wwwroot/js/site.js"
```

---

## 實作策略

### MVP 優先 (只交付 US1)

1. 完成階段 1 設定。
2. 完成階段 2 基礎建設。
3. 完成階段 3 US1。
4. 停下並獨立驗證 US1：`dotnet test CardPicker2.sln --filter "DrawRotationCooldownService|CardLibraryRotationDraw|DrawIdempotencyRotation|RotationCooldownDrawPage"`。
5. 若只需要 MVP，先完成階段 6 中與 US1 相關的安全、日誌、文件註解與 build/test 驗證。

### 增量交付

1. Setup + Foundation -> 輪替模型、service 與 resource 基礎就緒。
2. US1 -> 預設 N=3 的公平近期排除抽卡可展示。
3. US2 -> 空候選池與 invalid N 行為完整可信。
4. US3 -> snapshot replay、語系/主題/reduced motion 與 history 相容完整。
5. Polish -> 安全、效能、RWD、coverage 與 quickstart 全驗證。

### 團隊平行策略

1. 團隊共同完成階段 1 與階段 2。
2. foundation 完成後，US1 工作者負責 `CardLibraryService` draw success path，US2 工作者負責 empty/validation failure path，US3 工作者負責 snapshot replay/statistics compatibility tests。
3. 所有碰到 `CardPicker2/Services/CardLibraryService.cs`、`CardPicker2/Pages/Index.cshtml.cs`、`CardPicker2/Pages/Index.cshtml` 的工作需以 US1 優先合併，US2/US3 在最新 contract 上補 failure/replay 分支，避免同檔互相覆蓋。

---

## 備註

- `[P]` 任務代表不同檔案或明確可分段處理；若實作時發現同檔衝突，先完成較早任務再重排後續任務。
- 所有行為變更先寫失敗測試，且在對應實作後以任務中的 filter command 驗證。
- 新增或變更 public C# model/service API 必須補 XML 文件註解，且包含 `<example>` 與 `<code>`。
- 不得新增資料庫、外部 JSON API、SPA framework、加權推薦、保底、長期黑名單、手動排除或任何付費/價值分級規則。
