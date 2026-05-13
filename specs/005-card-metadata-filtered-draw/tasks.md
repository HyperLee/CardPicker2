# 任務: 餐點條件篩選抽卡

**輸入**: `specs/005-card-metadata-filtered-draw/` 的 `spec.md`、`plan.md`、`research.md`、`data-model.md`、`quickstart.md`、`contracts/ui-contract.md`，並以 `specs/004-draw-mode-statistics/` 的抽卡模式、抽卡歷史、統計、刪除保留與雙語 baseline 作為既有實作背景。
**先決條件**: ASP.NET Core Razor Pages app、既有 schema v3 `CardPicker2/data/cards.json`、004 draw mode/statistics 已實作、xUnit/Moq/WebApplicationFactory/Playwright 測試專案。
**測試**: 憲章與本功能 quickstart 明確要求測試優先，因此每個行為故事都先建立失敗測試，再實作。
**組織方式**: 任務依使用者故事分組，讓 P1 可以作為 MVP 獨立完成與驗證，P2 與 P3 可在 foundation 完成後平行推進。

## 格式: `[ID] [P?] [Story] 任務描述`

- **[P]**: 可平行處理，因為任務修改不同檔案且不依賴同階段其他未完成任務。
- **[Story]**: 僅使用於使用者故事階段，對應 `spec.md` 的 US1、US2、US3。
- 每個任務描述都包含具體檔案路徑或驗證命令目標。

---

## 階段 1: 設定 (共用基礎設施)

**目的**: 確認目前專案可還原與建置，並補齊 metadata/filter 測試會共用的 fixture 與 assertion helper。

- [X] T001 執行 `dotnet restore CardPicker2.sln`，確認 `CardPicker2.sln` 目前可還原。
- [X] T002 執行 `dotnet build CardPicker2.sln`，記錄 `CardPicker2.sln` 實作前 build baseline。
- [X] T003 [P] 擴充 metadata 測試資料建構器於 `tests/CardPicker2.UnitTests/Services/DrawFeatureTestData.cs`，加入 schema v4 cards、完整/缺漏 `decisionMetadata`、tags、price/time/diet/spice 與 deterministic card IDs。
- [X] T004 [P] 擴充整合測試 factory 於 `tests/CardPicker2.IntegrationTests/Infrastructure/DrawFeatureWebApplicationFactory.cs`，支援 filtered draw/filter query payload、culture cookie、Anti-Forgery token 與 production environment 切換。
- [X] T005 [P] 建立 metadata filter HTML assertion helper 於 `tests/CardPicker2.IntegrationTests/Pages/MetadataFilterHtmlAssertions.cs`，支援首頁、卡牌庫、details 與 create/edit 頁面的 filter controls、chips、metadata badges 與 localized empty state 驗證。

---

## 階段 2: 基礎建設 (阻斷性先決條件)

**目的**: 建立 schema v4、metadata enum/model、criteria、validation、filter service、migration 與 resource 基礎；所有 user story 都依賴此階段。

**關鍵限制**: 未完成本階段前，不要開始 US1/US2/US3 的實作任務。

- [X] T006 [P] 新增 schema v4 與 metadata migration 失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibrarySchemaVersionTests.cs`，覆蓋 v1/v2/v3 in-memory migration、v4 validation、invalid metadata enum block、corrupted JSON preserve、missing file seed v4。
- [X] T007 [P] 新增 metadata 正規化失敗測試於 `tests/CardPicker2.UnitTests/Models/MealCardDecisionMetadataTests.cs`，覆蓋 tag trim、空白移除、`OrdinalIgnoreCase` 去重、保留第一次顯示文字、dietary preference 去重與 stable order。
- [X] T008 [P] 新增 filter criteria 失敗測試於 `tests/CardPicker2.UnitTests/Models/CardFilterCriteriaTests.cs`，覆蓋 meal type optional、price/time/diet/spice/tags normalization、random mode 忽略 meal type 的 criteria 投影。
- [X] T009 [P] 新增 metadata validator 失敗測試於 `tests/CardPicker2.UnitTests/Services/MealCardMetadataValidatorTests.cs`，覆蓋 unsupported enum、空白 tag、重複 tag、metadata 可缺漏但有值必須合法。
- [X] T010 [P] 新增 filter service 失敗測試於 `tests/CardPicker2.UnitTests/Services/MealCardFilterServiceTests.cs`，覆蓋 active-only、缺 metadata 未篩選可通過、套用欄位時缺漏不符合、diet/tags all-match、max spice `<=`。
- [X] T011 [P] 新增 metadata localization resource 失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/LocalizationResourceTests.cs`，覆蓋 `zh-TW` 與 `en-US` metadata labels、options、validation、empty state 與 summary keys 不缺漏。
- [X] T012 [P] 建立價格區間 enum 於 `CardPicker2/Models/PriceRange.cs`，定義 `Low`、`Medium`、`High` 並補 XML 文件註解含 `<example>`/`<code>`。
- [X] T013 [P] 建立準備時間區間 enum 於 `CardPicker2/Models/PreparationTimeRange.cs`，定義 `Quick`、`Standard`、`Long` 並補 XML 文件註解含 `<example>`/`<code>`。
- [X] T014 [P] 建立飲食偏好 enum 於 `CardPicker2/Models/DietaryPreference.cs`，定義 `Vegetarian`、`Light`、`HeavyFlavor`、`TakeoutFriendly` 並補 XML 文件註解含 `<example>`/`<code>`。
- [X] T015 [P] 建立辣度 enum 於 `CardPicker2/Models/SpiceLevel.cs`，定義 `None`、`Mild`、`Medium`、`Hot` 與排序語意，並補 XML 文件註解含 `<example>`/`<code>`。
- [X] T016 [P] 建立餐點決策資訊模型於 `CardPicker2/Models/MealCardDecisionMetadata.cs`，包含 `Tags`、`PriceRange`、`PreparationTimeRange`、`DietaryPreferences`、`SpiceLevel` 與 normalize helper。
- [X] T017 [P] 建立共用篩選條件模型於 `CardPicker2/Models/CardFilterCriteria.cs`，包含 meal type、price、time、dietary preferences、max spice、tags、current language 與 active filter 判斷。
- [X] T018 [P] 建立條件摘要投影模型於 `CardPicker2/Models/FilterSummary.cs`，支援首頁、卡牌庫與結果摘要顯示目前套用條件。
- [X] T019 更新卡牌模型於 `CardPicker2/Models/MealCard.cs`，新增 optional `DecisionMetadata`，並讓 constructors、`Normalize()`、active/deleted lifecycle 都保留 metadata。
- [X] T020 更新根文件模型於 `CardPicker2/Models/CardLibraryDocument.cs`，將 `CurrentSchemaVersion` 升級為 4，保留 v1/v2/v3 migration 常數並明確保留 `DrawHistory`。
- [X] T021 建立 metadata 驗證服務於 `CardPicker2/Services/MealCardMetadataValidator.cs`，集中處理 enum validation、tag normalization、input-to-metadata 轉換與 localized status key。
- [X] T022 建立 metadata 篩選服務於 `CardPicker2/Services/MealCardFilterService.cs`，實作 active cards、交集規則、missing metadata semantics、all-tags/all-diet match 與 max spice `<=`。
- [X] T023 更新 library load/write/validation 於 `CardPicker2/Services/CardLibraryService.cs`，支援 schema v1/v2/v3/v4 migration、v3 card `DecisionMetadata = null`、v4 metadata validation、unsupported/corrupted block 與原檔保留。
- [X] T024 更新種子資料於 `CardPicker2/Services/SeedMealCards.cs`，讓 missing file 建立 schema v4，且每餐別至少 3 張 active bilingual cards 含可手動驗證的完整/部分 metadata。
- [X] T025 更新 DI 註冊於 `CardPicker2/Program.cs`，註冊 `MealCardMetadataValidator`、`MealCardFilterService` 與本功能需要的 singleton/scoped lifetime。
- [X] T026 [P] 新增或更新繁中 metadata 共用文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 labels、option display names、filter summary、validation 與 not-set empty text。
- [X] T027 [P] 新增或更新英文 metadata 共用文案於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保 `en-US` 不顯示未翻譯 key。
- [X] T028 執行 `dotnet test CardPicker2.sln --filter "SchemaV4|MealCardDecisionMetadata|CardFilterCriteria|MealCardMetadataValidator|MealCardFilterService|LocalizationResource"`，確認 `CardPicker2.sln` 的 foundation 測試從失敗轉為通過。

**檢查點**: schema v4、metadata model/validation/filter foundation 完成，user story implementation 可開始。

---

## 階段 3: 使用者故事 1 - 依當下條件篩選後公平抽卡 (優先級: P1) MVP

**目標**: 首頁支援 metadata filters；Normal mode 先依餐別再套用 filters，Random mode 從全部 active cards 套用 filters；空候選池不新增 history/statistics；篩選後候選池內仍等機率。

**獨立測試**: 使用具有不同 metadata 的 active cards，POST normal/random filtered draw，驗證候選池 membership、`1/N` 標稱機率、empty filtered pool、idempotent replay、語系切換與 Anti-Forgery。

### 使用者故事 1 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [X] T029 [P] [US1] 新增候選池篩選失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawCandidatePoolFilterTests.cs`，覆蓋 Normal meal type 後套用 filters、Random 忽略 meal type、missing metadata 被 selected filters 排除、每張候選卡標稱機率 `1/N`。
- [X] T030 [P] [US1] 新增 filtered draw 服務失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryFilteredDrawTests.cs`，覆蓋 price/time/diet/spice/tags filters、empty filtered pool 不新增 `DrawHistory`、statistics 不變、write failure 不宣告成功。
- [X] T031 [P] [US1] 擴充 idempotency 失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawIdempotencyTests.cs`，覆蓋同一 `DrawOperationId` replay 原 card ID，且不得因目前提交的新 filters 重新抽卡。
- [X] T032 [P] [US1] 新增首頁 filtered draw 整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/FilteredDrawPageTests.cs`，覆蓋 GET 顯示 filter controls、POST Normal/Random filters、empty pool message、Anti-Forgery 與 blocked state disable draw。
- [X] T033 [P] [US1] 新增首頁 filter 狀態語系與主題切換失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/FilterLocalizationStateTests.cs`，覆蓋 `zh-TW`/`en-US` 切換與 theme toggle 後保留 filters、result card ID、operation ID、候選池語意與 statistics。
- [X] T034 [US1] 執行 `dotnet test CardPicker2.sln --filter "DrawCandidatePoolFilter|CardLibraryFilteredDraw|DrawIdempotency|FilteredDrawPage|FilterLocalizationState"`，確認 `CardPicker2.sln` 的 US1 新測試在實作前失敗。

### 使用者故事 1 的實作

- [X] T035 [US1] 更新抽卡操作模型於 `CardPicker2/Models/DrawOperation.cs`，新增 `Filters` 並確保 Random mode criteria 不使用 meal type。
- [X] T036 [US1] 更新候選池建構器於 `CardPicker2/Services/DrawCandidatePoolBuilder.cs`，先建立 Normal/Random base pool，再透過 `MealCardFilterService` 套用 metadata filters。
- [X] T037 [US1] 更新抽卡結果模型於 `CardPicker2/Models/DrawResult.cs`，新增 `AppliedFilters`、`FilterSummary`、`FilteredPoolSize` 與 localized metadata result summary。
- [X] T038 [US1] 更新服務契約於 `CardPicker2/Services/ICardLibraryService.cs`，讓 `DrawAsync(DrawOperation operation, CancellationToken)` 明確支援 filtered draw 與 filter summary。
- [X] T039 [US1] 更新抽卡核心流程於 `CardPicker2/Services/CardLibraryService.cs`，驗證 filters、建立 filtered pool、empty pool 不寫 history、成功 append history、replay 不重新套用新 filters、記錄 filtered draw success/empty pool 日誌。
- [X] T040 [US1] 更新首頁 PageModel 於 `CardPicker2/Pages/Index.cshtml.cs`，bind price/time/diet/spice/tags filters、建立 `CardFilterCriteria`、保留 clear/result restore/query state 與 localized summary。
- [X] T041 [US1] 更新首頁 Razor UI 於 `CardPicker2/Pages/Index.cshtml`，新增 filter controls、active filter summary、clear filters 入口、empty filtered pool message 與 result metadata summary。
- [X] T042 [US1] 更新 localized card projection 於 `CardPicker2/Models/LocalizedMealCardView.cs` 與 `CardPicker2/Services/MealCardLocalizationService.cs`，加入 metadata display badges 與 not-set/fallback summary。
- [X] T043 [P] [US1] 新增或更新繁中首頁 filtered draw 文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 filter legends、clear filters、empty filtered pool、metadata result summary 與 validation messages。
- [X] T044 [P] [US1] 新增或更新英文首頁 filtered draw 文案於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保首頁 filter UI 與 result summary 無未翻譯 key。
- [X] T045 [P] [US1] 更新首頁 filter responsive 樣式於 `CardPicker2/wwwroot/css/site.css`，確保 390x844、768x1024、1366x768 下 filter panel、chips、slot/result 不重疊或水平溢出。
- [X] T046 [P] [US1] 更新首頁 filter progressive enhancement 於 `CardPicker2/wwwroot/js/site.js`，支援 filter state preservation、clear filters、tag chips 與 reduced-motion-safe submit guard，且不決定抽卡結果。
- [X] T047 [US1] 執行 `dotnet test CardPicker2.sln --filter "DrawCandidatePoolFilter|CardLibraryFilteredDraw|DrawIdempotency|FilteredDrawPage|FilterLocalizationState"`，確認 `CardPicker2.sln` 的 US1 測試通過。

**檢查點**: US1 可作為 MVP 獨立展示，包含 Normal/Random filtered draw、公平候選池、empty pool 防寫入與 idempotent replay。

---

## 階段 4: 使用者故事 2 - 以決策條件瀏覽與搜尋卡牌 (優先級: P2)

**目標**: `/Cards` 支援 keyword、meal type 與 metadata filters 交集搜尋；顯示目前條件、清除入口、結果數/無結果訊息與每張卡 metadata 摘要。

**獨立測試**: 建立多張不同 metadata 的 active cards，GET `/Cards` query filters，驗證結果只顯示同時符合全部條件的 active cards，且 query/filter state、clear filters 與 localization 正確。

### 使用者故事 2 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T048 [P] [US2] 擴充卡牌搜尋失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibrarySearchTests.cs`，覆蓋 keyword、meal type、price/time/diet/spice/tags 交集、deleted cards 排除與 missing metadata semantics。
- [ ] T049 [P] [US2] 新增卡牌庫 filtered search 整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/FilteredCardLibraryPageTests.cs`，覆蓋 query binding、active filter summary、clear filters、result count、無結果狀態與 blocked state。
- [ ] T050 [P] [US2] 擴充 localized search 頁面失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/LocalizedSearchPageTests.cs`，覆蓋英文語系 metadata labels/options/badges、fallback card name、無未翻譯 key。
- [ ] T051 [US2] 執行 `dotnet test CardPicker2.sln --filter "CardLibrarySearch|FilteredCardLibraryPage|LocalizedSearchPage"`，確認 `CardPicker2.sln` 的 US2 新測試在實作前失敗。

### 使用者故事 2 的實作

- [ ] T052 [US2] 更新搜尋條件模型於 `CardPicker2/Models/SearchCriteria.cs`，加入 `Filters`、normalized tags、metadata filter state 與 current-language keyword projection 規則。
- [ ] T053 [US2] 更新搜尋服務流程於 `CardPicker2/Services/CardLibraryService.cs`，在 active-only keyword/meal type 搜尋後套用 `MealCardFilterService`，並記錄 filtered search result count。
- [ ] T054 [US2] 更新卡牌庫 PageModel 於 `CardPicker2/Pages/Cards/Index.cshtml.cs`，bind price/time/diet/spice/tags query fields，建立 `SearchCriteria`、`FilterSummary` 與 clear filters 狀態。
- [ ] T055 [US2] 更新卡牌庫 Razor UI 於 `CardPicker2/Pages/Cards/Index.cshtml`，新增 metadata filter controls、active filter chips、clear filters 入口、result count、no-result message 與 card metadata badges。
- [ ] T056 [US2] 更新 card projection metadata 摘要於 `CardPicker2/Models/LocalizedMealCardView.cs` 與 `CardPicker2/Services/MealCardLocalizationService.cs`，讓卡牌庫每列可顯示 tags、price、time、diet 與 spice。
- [ ] T057 [P] [US2] 新增或更新繁中卡牌庫篩選文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 Cards filter labels、active condition chips、result count、no-result 與 clear filters。
- [ ] T058 [P] [US2] 新增或更新英文卡牌庫篩選文案於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保 `/Cards` filtered search 無未翻譯 key。
- [ ] T059 [P] [US2] 更新卡牌庫 filter responsive 樣式於 `CardPicker2/wwwroot/css/site.css`，確保 filter panel、tag chips、metadata badges 與 card list 在指定 viewport 不溢出。
- [ ] T060 [P] [US2] 更新卡牌庫 filter progressive enhancement 於 `CardPicker2/wwwroot/js/site.js`，支援 clear filters、tag input/chips 與語系/主題切換前的 transient state preservation。
- [ ] T061 [US2] 執行 `dotnet test CardPicker2.sln --filter "CardLibrarySearch|FilteredCardLibraryPage|LocalizedSearchPage"`，確認 `CardPicker2.sln` 的 US2 測試通過。

**檢查點**: US1 與 US2 可同時運作，首頁與卡牌庫使用同一 metadata filter semantics。

---

## 階段 5: 使用者故事 3 - 維護餐點決策資訊 (優先級: P3)

**目標**: Create/Edit/Details 支援 metadata 維護與顯示；舊卡缺 metadata 不 blocked；metadata 更新不改變 card ID、history、statistics、status 或 duplicate detection 規則。

**獨立測試**: 使用舊 schema v3 與新 schema v4 資料，確認缺 metadata 的卡仍可未篩選抽卡/搜尋，create/edit 可保存 metadata，details 顯示 not-set，metadata update 不切分統計。

### 使用者故事 3 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T062 [P] [US3] 擴充輸入模型失敗測試於 `tests/CardPicker2.UnitTests/Models/MealCardInputModelTests.cs`，覆蓋 metadata optional、invalid enum rejection、tag normalization、validation message localization。
- [ ] T063 [P] [US3] 新增 metadata persistence 失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryMetadataPersistenceTests.cs`，覆蓋 create/edit metadata atomic write、schema v4 persist、restart reload、metadata update 保留 card ID/history/statistics/status。
- [ ] T064 [P] [US3] 新增 card metadata management 頁面失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/CardMetadataManagementPageTests.cs`，覆蓋 create/edit/details metadata fields、not-set display、validation failure 保留輸入與 Anti-Forgery。
- [ ] T065 [P] [US3] 擴充 duplicate detector 失敗測試於 `tests/CardPicker2.UnitTests/Services/DuplicateCardDetectorTests.cs`，確認 duplicate detection 只依 bilingual name、description、meal type，不因 metadata 不同而放寬或收緊。
- [ ] T066 [P] [US3] 新增舊卡 metadata 缺漏相容失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryMetadataPersistenceTests.cs`，覆蓋 schema v3 舊卡未套用 filters 可搜尋/抽卡，套用缺漏欄位 filter 時不符合。
- [ ] T067 [US3] 執行 `dotnet test CardPicker2.sln --filter "MealCardInputModel|CardLibraryMetadataPersistence|CardMetadataManagementPage|DuplicateCardDetector"`，確認 `CardPicker2.sln` 的 US3 新測試在實作前失敗。

### 使用者故事 3 的實作

- [ ] T068 [US3] 更新輸入模型於 `CardPicker2/Models/MealCardInputModel.cs`，新增 `TagsInput`、`PriceRange`、`PreparationTimeRange`、`DietaryPreferences`、`SpiceLevel`、metadata normalization 與 `ToDecisionMetadata()`。
- [ ] T069 [US3] 更新共用卡牌表單於 `CardPicker2/Pages/Cards/_CardForm.cshtml`，加入 tags、price、time、dietary preferences、spice inputs，並保留 bilingual required fields 的 validation。
- [ ] T070 [US3] 更新新增卡牌 PageModel 於 `CardPicker2/Pages/Cards/Create.cshtml.cs`，讓 metadata validation failure 回到 page、保留輸入、blocked state 禁用 submit。
- [ ] T071 [US3] 更新編輯卡牌 PageModel 於 `CardPicker2/Pages/Cards/Edit.cshtml.cs`，從既有 card 載入 metadata 到 `MealCardInputModel`，且 metadata-only update 保留同一 card ID。
- [ ] T072 [US3] 更新 create/update mutation 流程於 `CardPicker2/Services/CardLibraryService.cs`，在同一原子寫入中保存 metadata、拒絕 invalid metadata、保留 existing lifecycle/history、寫入失敗不局部修改。
- [ ] T073 [US3] 更新重複偵測於 `CardPicker2/Services/DuplicateCardDetector.cs`，明確忽略 `DecisionMetadata`，只比對 active card 的 normalized bilingual name、description 與 meal type。
- [ ] T074 [US3] 更新 details PageModel 於 `CardPicker2/Pages/Cards/Details.cshtml.cs`，載入 localized metadata summary 與 not-set display state。
- [ ] T075 [US3] 更新 details Razor UI 於 `CardPicker2/Pages/Cards/Details.cshtml`，顯示 tags、price、time、diet、spice metadata summary，缺漏欄位顯示目前語系 not-set 文案。
- [ ] T076 [P] [US3] 新增或更新繁中 card metadata 管理文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`，包含 form labels、help text、validation、details not-set 與 success/failure messages。
- [ ] T077 [P] [US3] 新增或更新英文 card metadata 管理文案於 `CardPicker2/Resources/SharedResource.en-US.resx`，確保 create/edit/details metadata UI 無未翻譯 key。
- [ ] T078 [P] [US3] 更新 metadata form/detail responsive 樣式於 `CardPicker2/wwwroot/css/site.css`，確保 field groups、checkboxes、chips、badges 與 detail summary 在指定 viewport 不溢出。
- [ ] T079 [P] [US3] 更新表單狀態保留腳本於 `CardPicker2/wwwroot/js/site.js`，讓 create/edit metadata fields 在語系或主題切換前可暫存並在成功還原後清除。
- [ ] T080 [US3] 執行 `dotnet test CardPicker2.sln --filter "MealCardInputModel|CardLibraryMetadataPersistence|CardMetadataManagementPage|DuplicateCardDetector"`，確認 `CardPicker2.sln` 的 US3 測試通過。

**檢查點**: 所有使用者故事均可獨立驗證，且 metadata 維護不破壞舊卡、history、statistics、duplicate detection 或 deleted retention。

---

## 階段 6: 潤飾與跨領域關注點

**目的**: 補齊安全、可觀察性、效能、RWD/可及性、公開介面邊界、公開 C# model/service API 文件註解、coverage evidence、完整驗證與 quickstart 手動檢查。

- [ ] T081 [P] 擴充安全標頭與 Anti-Forgery 測試於 `tests/CardPicker2.IntegrationTests/SecurityHeadersTests.cs`，覆蓋 filtered draw、create/edit metadata forms、production HSTS/CSP 與不新增外部來源。
- [ ] T082 [P] 擴充 metadata/filter 結構化日誌測試於 `tests/CardPicker2.UnitTests/Services/DrawLoggingTests.cs`，覆蓋 schema v4 migration、metadata validation failure、empty filtered pool、filtered draw success、filtered search count、write failure 且不記錄完整 JSON 或未清理 tag 原文。
- [ ] T083 [P] 擴充公開 C# model/service API 文件測試於 `tests/CardPicker2.UnitTests/Documentation/PublicApiDocumentationTests.cs`，要求本功能新增或變更的 public models/services XML docs 包含 `<summary>`、`<example>` 與 `<code>`。
- [ ] T084 [P] 擴充公開介面邊界測試於 `tests/CardPicker2.IntegrationTests/RouteSurfaceTests.cs`，確認本功能未新增外部 JSON/API endpoint，公開介面維持 Razor Pages、表單、query strings、page handlers 與狀態碼。
- [ ] T085 [P] 新增 metadata filter 效能與 web-vitals 驗證於 `tests/CardPicker2.IntegrationTests/Performance/MetadataFilterPerformanceTests.cs`，覆蓋首頁 GET、filtered draw POST、`/Cards` filtered search、metadata projection 在至少 150 張 active cards + 1,000 筆 draw history 本機 JSON fixture 下 p95 < 200ms、主要內容 1 秒內更新、FCP < 1.5 秒與 LCP < 2.5 秒。
- [ ] T086 [P] 新增 RWD/reduced-motion/可及性 browser 測試於 `tests/CardPicker2.IntegrationTests/Browser/MetadataFilterResponsiveAccessibilityTests.cs`，覆蓋 390x844、768x1024、1366x768、`zh-TW`/`en-US`、theme toggle 後 filter state 保留、`prefers-reduced-motion: reduce`、鍵盤操作與 axe smoke check。
- [ ] T087 更新安全與 DI wiring 於 `CardPicker2/Program.cs`，確認 production CSP/HSTS 保留、filter services lifetime 正確、Serilog 設定不輸出敏感內容。
- [ ] T088 更新新增 public C# model XML 文件註解於 `CardPicker2/Models/PriceRange.cs`、`CardPicker2/Models/PreparationTimeRange.cs`、`CardPicker2/Models/DietaryPreference.cs`、`CardPicker2/Models/SpiceLevel.cs`、`CardPicker2/Models/MealCardDecisionMetadata.cs`、`CardPicker2/Models/CardFilterCriteria.cs`、`CardPicker2/Models/FilterSummary.cs`。
- [ ] T089 更新新增或變更 public C# service XML 文件註解於 `CardPicker2/Services/ICardLibraryService.cs`、`CardPicker2/Services/CardLibraryService.cs`、`CardPicker2/Services/MealCardMetadataValidator.cs`、`CardPicker2/Services/MealCardFilterService.cs`、`CardPicker2/Services/DrawCandidatePoolBuilder.cs`、`CardPicker2/Services/MealCardLocalizationService.cs`。
- [ ] T090 執行 `dotnet test CardPicker2.sln --filter "Metadata|CardFilter|FilteredDraw|FilteredSearch|SchemaV4|SecurityHeaders|AntiForgery|Logging|RouteSurface|MetadataFilterPerformance|WebVitals|MetadataFilterResponsiveAccessibility"`，確認 `CardPicker2.sln` 的 cross-cutting 測試通過。
- [ ] T091 執行 `dotnet build CardPicker2.sln`，確認 `CardPicker2.sln` 無新增 build warning、formatting 或 naming 違規。
- [ ] T092 執行 `dotnet test CardPicker2.sln`，確認 `CardPicker2.sln` 全部單元、整合、browser/security/performance/route-surface tests 通過。
- [ ] T093 執行 `dotnet test CardPicker2.sln --collect:"XPlat Code Coverage"`，檢查 coverage report 中本功能涉及的 critical business logic（`CardPicker2/Models/` 與 `CardPicker2/Services/` 的 metadata/filter/draw/search/persistence 路徑）覆蓋率達 80% 以上；若未達標，必須在 `plan.md` 記錄例外、風險與補救計畫後才可交付。
- [ ] T094 依 `specs/005-card-metadata-filtered-draw/quickstart.md` 完成手動或 browser automation 驗證，覆蓋首頁 filtered draw、Random mode filters、empty pool、卡牌庫篩選、metadata create/edit/details、schema v4、語系與主題狀態保留、reduced motion、RWD、安全、觀察性，以及首頁/卡牌庫各至少 10 次 smoke checklist 中 9 次以上於 30 秒內完成或看到可理解提示。

---

## 相依性與執行順序

### 階段相依性

- **階段 1 設定**: 無依賴，可立即開始。
- **階段 2 基礎建設**: 依賴階段 1，且封鎖所有 user story。
- **階段 3 US1**: 依賴階段 2，是 MVP。
- **階段 4 US2**: 依賴階段 2；可在 US1 filter semantics 穩定後與 US3 部分平行，但最終需使用同一 `MealCardFilterService`。
- **階段 5 US3**: 依賴階段 2；create/edit/details metadata 可與 US2 UI 平行，但需與 schema v4 persistence 與 duplicate detection 整合。
- **階段 6 潤飾**: 依賴欲交付的 user stories 完成。

### 使用者故事相依性

- **US1 (P1)**: 可在 foundation 後開始，提供 MVP；不依賴 US2/US3。
- **US2 (P2)**: 依賴 foundation 的 metadata model/filter service；與 US1 共用 filter semantics，但卡牌庫搜尋可獨立驗證。
- **US3 (P3)**: 依賴 foundation 的 metadata model/validation/schema v4；metadata 維護完成後供 US1/US2 使用，但舊卡相容與 not-set display 可獨立驗證。

### 相依圖

```text
階段 1 設定
  -> 階段 2 基礎建設
      -> US1 首頁條件篩選抽卡
      -> US2 卡牌庫條件篩選搜尋
      -> US3 卡牌決策資訊維護
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
- T006 到 T011 可平行撰寫 foundation 失敗測試。
- T012 到 T018 可平行建立互不依賴的 enum/model 檔案。
- T026 與 T027 可平行補齊雙語 resource keys。
- US1 的 T029 到 T033 可平行撰寫測試；T043 到 T046 可在 T040/T041 的 UI contract 明確後平行處理 resources、CSS、JS。
- US2 的 T048 到 T050 可平行撰寫測試；T057 到 T060 可在 T054/T055 的 query/UI contract 明確後平行處理 resources、CSS、JS。
- US3 的 T062 到 T066 可平行撰寫測試；T076 到 T079 可在 T068/T069 的 form contract 明確後平行處理 resources、CSS、JS。
- T081 到 T086 可平行補齊 cross-cutting 測試。

---

## 平行範例: 使用者故事 1

```bash
# 平行撰寫 US1 失敗測試:
任務: "T029 [US1] tests/CardPicker2.UnitTests/Services/DrawCandidatePoolFilterTests.cs"
任務: "T030 [US1] tests/CardPicker2.UnitTests/Services/CardLibraryFilteredDrawTests.cs"
任務: "T031 [US1] tests/CardPicker2.UnitTests/Services/DrawIdempotencyTests.cs"
任務: "T032 [US1] tests/CardPicker2.IntegrationTests/Pages/FilteredDrawPageTests.cs"
任務: "T033 [US1] tests/CardPicker2.IntegrationTests/Pages/FilterLocalizationStateTests.cs"

# UI contract 穩定後平行處理文案與前端:
任務: "T043 [US1] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T044 [US1] CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T045 [US1] CardPicker2/wwwroot/css/site.css"
任務: "T046 [US1] CardPicker2/wwwroot/js/site.js"
```

## 平行範例: 使用者故事 2

```bash
# 平行撰寫 US2 失敗測試:
任務: "T048 [US2] tests/CardPicker2.UnitTests/Services/CardLibrarySearchTests.cs"
任務: "T049 [US2] tests/CardPicker2.IntegrationTests/Pages/FilteredCardLibraryPageTests.cs"
任務: "T050 [US2] tests/CardPicker2.IntegrationTests/Pages/LocalizedSearchPageTests.cs"

# 卡牌庫 UI 文案與 responsive 工作:
任務: "T057 [US2] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T058 [US2] CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T059 [US2] CardPicker2/wwwroot/css/site.css"
任務: "T060 [US2] CardPicker2/wwwroot/js/site.js"
```

## 平行範例: 使用者故事 3

```bash
# 平行撰寫 US3 失敗測試:
任務: "T062 [US3] tests/CardPicker2.UnitTests/Models/MealCardInputModelTests.cs"
任務: "T063 [US3] tests/CardPicker2.UnitTests/Services/CardLibraryMetadataPersistenceTests.cs"
任務: "T064 [US3] tests/CardPicker2.IntegrationTests/Pages/CardMetadataManagementPageTests.cs"
任務: "T065 [US3] tests/CardPicker2.UnitTests/Services/DuplicateCardDetectorTests.cs"

# metadata 管理呈現層可平行處理:
任務: "T076 [US3] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T077 [US3] CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T078 [US3] CardPicker2/wwwroot/css/site.css"
任務: "T079 [US3] CardPicker2/wwwroot/js/site.js"
```

---

## 實作策略

### MVP 優先 (僅使用者故事 1)

1. 完成階段 1 設定。
2. 完成階段 2 基礎建設。
3. 完成階段 3 US1。
4. 停下並驗證 `dotnet test CardPicker2.sln --filter "DrawCandidatePoolFilter|CardLibraryFilteredDraw|DrawIdempotency|FilteredDrawPage|FilterLocalizationState"`。
5. 可展示 Normal/Random filtered draw、metadata candidate pool、empty pool 防寫入、idempotent replay 與 result metadata summary。

### 增量交付

1. 設定與基礎建設: schema v4、metadata model、criteria、validator、filter service。
2. US1: 首頁 filtered draw 與公平候選池，形成 MVP。
3. US2: 卡牌庫 metadata filtered search、條件摘要與 clear filters。
4. US3: create/edit/details metadata 維護、舊卡相容與 duplicate/history/statistics invariant。
5. 潤飾: security/logging/performance/RWD/XML docs/full test/manual verification。

### 品質閘門

- 每個 behavior task 的測試先於 implementation。
- `dotnet test CardPicker2.sln --filter ...` 在每個 story checkpoint 通過。
- 最終 `dotnet build CardPicker2.sln` 與 `dotnet test CardPicker2.sln` 通過。
- `dotnet test CardPicker2.sln --collect:"XPlat Code Coverage"` 產出覆蓋率證據，critical business logic 覆蓋率達 80% 以上，或 `plan.md` 明確記錄例外、風險與補救計畫。
- 所有使用者可見 runtime UI 在 `zh-TW` 與 `en-US` 不顯示未翻譯 key。
- `CardPicker2/data/cards.json` corrupted/unsupported 時原檔保留，且 create/edit/delete/draw/search/statistics 都進入 blocked recovery。
- production HSTS/CSP、Anti-Forgery、效能與 web-vitals 預算、公開介面邊界、結構化日誌與敏感資訊禁止輸出皆有測試證據。

---

## 獨立測試條件摘要

- **US1**: Normal mode 先依餐別再套用 metadata filters；Random mode 從全部 active cards 套用 filters；缺 metadata 的 card 在未篩選時可抽、套用該欄位時不符合；empty filtered pool、blocked、未投幣、invalid input 不成功；同 operation replay 不新增 history。
- **US2**: `/Cards` keyword、meal type、price/time/diet/spice/tags 採交集；結果只包含 active cards；目前條件與清除入口可見；無結果與 metadata badges 依目前語系顯示。
- **US3**: create/edit 可保存 optional metadata；details 顯示 not-set；舊卡缺 metadata 不 blocked；metadata update 不改變 card ID、draw history、statistics、status 或 duplicate detection 規則。

## 建議 MVP 範圍

MVP 範圍為階段 1、階段 2、階段 3，也就是 US1「依當下條件篩選後公平抽卡」。完成後即可交付首頁 metadata filtered draw、schema v4 foundation、候選池公平性、空候選池防寫入與 idempotent replay；US2/US3 可作為後續增量。

## 任務統計與格式驗證

- **總任務數**: 94
- **US1 任務數**: 19
- **US2 任務數**: 14
- **US3 任務數**: 19
- **Setup 任務數**: 5
- **Foundational 任務數**: 23
- **Polish 任務數**: 14
- **平行機會**: 測試 fixture、foundation tests、enum/model files、resource files、CSS/JS、cross-cutting tests 可平行處理。
- **格式驗證**: 全部任務均使用 `- [ ] T### [P?] [US?] 任務描述含檔案路徑` 格式；Setup、Foundational、Polish 任務不含 story label；US1/US2/US3 任務均含對應 story label。
