# 任務: 餐點收藏與手動排除抽卡

**輸入**: 來自 `/specs/007-card-preference-controls/` 的設計文件

**前置文件**: `plan.md`、`spec.md`、`research.md`、`data-model.md`、`quickstart.md`、`contracts/ui-contract.md`

**測試要求**: 憲章與本功能規格明確要求測試優先。所有會改變行為、資料規則、驗證邏輯或使用者流程的任務，必須先完成對應失敗測試，再進行實作。

**組織方式**: 任務依使用者故事分組，確保每個故事可獨立實作、驗證與展示。

## 階段 1: 設定與測試輔助

**目的**: 準備 schema v5、偏好狀態與頁面測試會共用的 fixture/helper，避免每個故事重複建立測試資料。

- [ ] T001 [P] 擴充 schema v5、收藏與排除抽卡測試資料 helper 於 `tests/CardPicker2.UnitTests/Services/DrawFeatureTestData.cs`
- [ ] T002 [P] 擴充 preference-aware integration fixture 建立工具於 `tests/CardPicker2.IntegrationTests/Infrastructure/MetadataFilterTestData.cs`
- [ ] T003 [P] 擴充隔離 JSON 資料檔 helper 以支援 schema v5 與重啟驗證於 `tests/CardPicker2.IntegrationTests/Infrastructure/TempCardLibrary.cs`
- [ ] T004 [P] 擴充測試網站 factory 的偏好測試 wiring 與 deterministic randomizer 支援於 `tests/CardPicker2.IntegrationTests/Infrastructure/DrawFeatureWebApplicationFactory.cs`
- [ ] T005 [P] 新增偏好 UI HTML assertion helper 於 `tests/CardPicker2.IntegrationTests/Pages/PreferenceHtmlAssertions.cs`

---

## 階段 2: 基礎建設

**目的**: 建立所有使用者故事共用的偏好模型、schema v5 migration、資料驗證、投影與服務契約。此階段完成前不得開始 user story 實作。

**關鍵**: 先寫失敗測試，再實作模型與 persistence。schema v4 缺少 `preferences` 必須安全載入為預設值；corrupted/unsupported/invalid preference JSON 仍維持封鎖且保留原檔。

### 基礎測試

- [ ] T006 [P] 新增偏好狀態預設值、正規化與不可影響抽卡的失敗測試於 `tests/CardPicker2.UnitTests/Models/CardPreferenceStateTests.cs`
- [ ] T007 [P] 新增 favorite/draw eligibility criteria 預設值與 unsupported value 行為失敗測試於 `tests/CardPicker2.UnitTests/Models/CardPreferenceCriteriaTests.cs`
- [ ] T008 [P] 新增 schema v4->v5 migration、schema v5 round-trip、invalid preference JSON 封鎖與 write v5 失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryPreferencePersistenceTests.cs`
- [ ] T009 [P] 擴充 public model/service XML 文件註解失敗測試以涵蓋偏好 API 於 `tests/CardPicker2.UnitTests/Documentation/PublicApiDocumentationTests.cs`
- [ ] T010 執行 `dotnet test CardPicker2.sln --filter "CardPreferenceState|CardPreferenceCriteria|CardLibraryPreferencePersistence|PublicApiDocumentation"`，確認 `CardPicker2.sln` 的基礎新測試在實作前失敗

### 基礎實作

- [ ] T011 [P] 建立卡牌偏好狀態模型與 XML 文件註解於 `CardPicker2/Models/CardPreferenceState.cs`
- [ ] T012 [P] 建立收藏篩選列舉與 XML 文件註解於 `CardPicker2/Models/FavoriteFilter.cs`
- [ ] T013 [P] 建立可抽狀態篩選列舉與 XML 文件註解於 `CardPicker2/Models/DrawEligibilityFilter.cs`
- [ ] T014 [P] 建立偏好篩選條件模型與 XML 文件註解於 `CardPicker2/Models/CardPreferenceCriteria.cs`
- [ ] T015 [P] 建立 target-state 偏好更新輸入模型與 server validation 註解於 `CardPicker2/Models/CardPreferenceUpdateInputModel.cs`
- [ ] T016 [P] 建立偏好 mutation 結果模型與 status enum 於 `CardPicker2/Models/PreferenceMutationResult.cs`
- [ ] T017 更新 JSON 文件 schema 常數、v5 版本語意與 XML 文件註解於 `CardPicker2/Models/CardLibraryDocument.cs`
- [ ] T018 更新卡牌模型以保存 `Preferences`、`IsDrawable`、`IsPreferenceEditable` 與 normalize 保留偏好於 `CardPicker2/Models/MealCard.cs`
- [ ] T019 更新搜尋條件以納入 `CardPreferenceCriteria` 預設值於 `CardPicker2/Models/SearchCriteria.cs`
- [ ] T020 更新 localized card projection 以包含收藏、排除與可抽狀態欄位於 `CardPicker2/Models/LocalizedMealCardView.cs`
- [ ] T021 更新抽卡結果模型以攜帶選中卡牌目前偏好狀態與 preference message key 於 `CardPicker2/Models/DrawResult.cs`
- [ ] T022 更新候選池空集合原因以區分偏好排除造成的空候選池於 `CardPicker2/Models/CandidatePoolEmptyReason.cs`
- [ ] T023 更新種子資料建立流程，確保新 seed card 預設未收藏且未排除於 `CardPicker2/Services/SeedMealCards.cs`
- [ ] T024 更新服務契約，加入 target-state 偏好 mutation 與偏好投影需求於 `CardPicker2/Services/ICardLibraryService.cs`
- [ ] T025 更新卡牌本地化投影，輸出偏好 badges、可抽狀態與安全 display projection 於 `CardPicker2/Services/MealCardLocalizationService.cs`
- [ ] T026 更新 card library persistence，支援 schema v1-v4 載入為 v5、schema v5 validation、invalid preference blocking 與 atomic v5 write 於 `CardPicker2/Services/CardLibraryService.cs`
- [ ] T027 執行 `dotnet test CardPicker2.sln --filter "CardPreferenceState|CardPreferenceCriteria|CardLibraryPreferencePersistence|PublicApiDocumentation"`，確認 `CardPicker2.sln` 的基礎測試通過

**檢查點**: schema v5 偏好狀態可安全載入、驗證、投影與保存；所有 user story 可開始實作。

---

## 階段 3: 使用者故事 1 - 手動排除不想再抽到的餐點 (優先級: P1)

**目標**: 使用者可在卡牌庫與詳情頁將 active card 設為排除抽卡或取消排除；已排除卡仍預設可見且可管理，但不得進入任何未來 normal/random/metadata/rotation 候選池。

**獨立測試**: 準備多張有效卡牌，將其中一張設為排除後，執行 normal mode、random mode、metadata filtered draw 與 rotation draw；被排除卡不得出現在候選池或結果中，且取消排除後可在符合條件時回到候選池。

### 使用者故事 1 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T028 [P] [US1] 新增排除 target-state mutation、重複提交、missing/deleted/blocked/write failure 失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryPreferenceMutationTests.cs`
- [ ] T029 [P] [US1] 新增偏好排除候選池失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawCandidatePoolPreferenceTests.cs`
- [ ] T030 [P] [US1] 新增偏好排除與 metadata/rotation 順序失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryPreferenceDrawTests.cs`
- [ ] T031 [P] [US1] 新增首頁偏好排除抽卡整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/PreferenceFilteredDrawPageTests.cs`
- [ ] T032 [P] [US1] 新增卡牌庫與詳情頁排除操作整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/CardPreferencePageTests.cs`
- [ ] T033 [P] [US1] 新增偏好排除後統計不變整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/PreferenceFilteredDrawStatisticsTests.cs`
- [ ] T034 [US1] 執行 `dotnet test CardPicker2.sln --filter "PreferenceMutation|DrawCandidatePoolPreference|CardLibraryPreferenceDraw|PreferenceFilteredDraw|CardPreferencePage|PreferenceFilteredDrawStatistics"`，確認 `CardPicker2.sln` 的 US1 新測試在實作前失敗

### 使用者故事 1 的實作

- [ ] T035 [US1] 實作 `SetPreferenceAsync` target-state mutation、idempotency、missing/deleted/blocked/write failure 與安全 message key 於 `CardPicker2/Services/CardLibraryService.cs`
- [ ] T036 [US1] 更新候選池建構，先移除 `Preferences.IsExcludedFromDraw == true` 再套用 mode、meal 與 metadata filters 於 `CardPicker2/Services/DrawCandidatePoolBuilder.cs`
- [ ] T037 [US1] 更新抽卡流程，處理 preference exclusion count、preference-empty reason、不呼叫 randomizer、不新增 history/statistics 與結構化日誌於 `CardPicker2/Services/CardLibraryService.cs`
- [ ] T038 [US1] 更新卡牌庫 PageModel，加入排除/取消排除 POST handler、Anti-Forgery 表單回跳與 localized status message 於 `CardPicker2/Pages/Cards/Index.cshtml.cs`
- [ ] T039 [US1] 更新詳情頁 PageModel，加入排除/取消排除 POST handler、not-found/deleted/blocked/write failure 處理於 `CardPicker2/Pages/Cards/Details.cshtml.cs`
- [ ] T040 [US1] 建立可重用排除控制 partial，提交 target final state 而非 toggle action 於 `CardPicker2/Pages/Cards/_CardPreferenceControls.cshtml`
- [ ] T041 [US1] 更新卡牌庫列表，預設顯示已排除 active cards、狀態 badge 與排除 target-state form 於 `CardPicker2/Pages/Cards/Index.cshtml`
- [ ] T042 [US1] 更新卡牌詳情頁，顯示排除狀態、可抽狀態與排除 target-state form 於 `CardPicker2/Pages/Cards/Details.cshtml`
- [ ] T043 [P] [US1] 新增繁中排除抽卡、可抽狀態、偏好空候選池與 mutation 訊息資源於 `CardPicker2/Resources/SharedResource.zh-TW.resx`
- [ ] T044 [P] [US1] 新增英文排除抽卡、可抽狀態、偏好空候選池與 mutation 訊息資源於 `CardPicker2/Resources/SharedResource.en-US.resx`
- [ ] T045 [P] [US1] 更新 preference badge、列表控制、詳情控制與 empty prompt responsive 樣式於 `CardPicker2/wwwroot/css/site.css`
- [ ] T046 [P] [US1] 更新 target-state button progressive enhancement 與快速連點 UI guard 於 `CardPicker2/wwwroot/js/site.js`
- [ ] T047 [US1] 執行 `dotnet test CardPicker2.sln --filter "PreferenceMutation|DrawCandidatePoolPreference|CardLibraryPreferenceDraw|PreferenceFilteredDraw|CardPreferencePage|PreferenceFilteredDrawStatistics"`，確認 `CardPicker2.sln` 的 US1 測試通過

**檢查點**: US1 是 MVP；使用者可手動排除餐點，排除卡仍可管理且不再進入未來抽卡候選池。

---

## 階段 4: 使用者故事 2 - 收藏喜歡的餐點並快速辨識 (優先級: P2)

**目標**: 使用者可收藏/取消收藏卡牌；收藏狀態在卡牌庫與詳情頁清楚呈現，並可和 keyword、meal type、metadata、draw eligibility filters 交集篩選；收藏不得影響候選池、排序、統計、rotation 或 duplicate detection。

**獨立測試**: 收藏多張卡牌後在 `/Cards` 套用收藏篩選並查看詳情；再驗證收藏卡與未收藏卡在同一候選池內仍有相同標稱機率，history/statistics/rotation 不因收藏改變。

### 使用者故事 2 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T048 [P] [US2] 新增收藏 target-state mutation 與重啟保存失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryPreferenceMutationTests.cs`
- [ ] T049 [P] [US2] 新增偏好篩選服務交集規則失敗測試於 `tests/CardPicker2.UnitTests/Services/CardPreferenceFilterServiceTests.cs`
- [ ] T050 [P] [US2] 新增收藏不得影響候選池與公平性失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawCandidatePoolPreferenceTests.cs`
- [ ] T051 [P] [US2] 新增收藏/可抽狀態與 keyword/meal/metadata 組合搜尋失敗測試於 `tests/CardPicker2.UnitTests/Services/CardLibraryPreferenceSearchTests.cs`
- [ ] T052 [P] [US2] 新增卡牌庫偏好篩選整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/PreferenceFilteredCardLibraryPageTests.cs`
- [ ] T053 [P] [US2] 新增收藏與統計/rotation 相容性失敗測試於 `tests/CardPicker2.UnitTests/Services/DrawStatisticsPreferenceCompatibilityTests.cs`
- [ ] T054 [US2] 執行 `dotnet test CardPicker2.sln --filter "PreferenceMutation|CardPreferenceFilterService|DrawCandidatePoolPreference|CardLibraryPreferenceSearch|PreferenceFilteredCardLibrary|DrawStatisticsPreferenceCompatibility"`，確認 `CardPicker2.sln` 的 US2 新測試在實作前失敗

### 使用者故事 2 的實作

- [ ] T055 [US2] 建立偏好篩選服務，實作 `FavoriteFilter` 與 `DrawEligibilityFilter` 交集規則於 `CardPicker2/Services/CardPreferenceFilterService.cs`
- [ ] T056 [US2] 更新服務 wiring，註冊或注入偏好篩選服務且不改變既有 lifetime 於 `CardPicker2/Program.cs`
- [ ] T057 [US2] 更新卡牌搜尋流程，套用 keyword、meal type、metadata filters 與 preference filters 交集並記錄安全 count 於 `CardPicker2/Services/CardLibraryService.cs`
- [ ] T058 [US2] 更新卡牌庫 PageModel，加入 `favoriteFilter`、`drawEligibilityFilter` query binding、條件摘要與清除條件狀態於 `CardPicker2/Pages/Cards/Index.cshtml.cs`
- [ ] T059 [US2] 更新偏好控制 partial，加入收藏/取消收藏 target-state form 與雙狀態 badge 於 `CardPicker2/Pages/Cards/_CardPreferenceControls.cshtml`
- [ ] T060 [US2] 更新卡牌庫 Razor UI，加入收藏篩選、可抽/已排除篩選、結果數、badge 與 filter state preservation 於 `CardPicker2/Pages/Cards/Index.cshtml`
- [ ] T061 [US2] 更新詳情頁 Razor UI，顯示收藏狀態並提供收藏/取消收藏操作於 `CardPicker2/Pages/Cards/Details.cshtml`
- [ ] T062 [P] [US2] 新增繁中收藏 labels、filters、badges、success/error 與 no-result 文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`
- [ ] T063 [P] [US2] 新增英文收藏 labels、filters、badges、success/error 與 no-result 文案於 `CardPicker2/Resources/SharedResource.en-US.resx`
- [ ] T064 [US2] 執行 `dotnet test CardPicker2.sln --filter "PreferenceMutation|CardPreferenceFilterService|DrawCandidatePoolPreference|CardLibraryPreferenceSearch|PreferenceFilteredCardLibrary|DrawStatisticsPreferenceCompatibility"`，確認 `CardPicker2.sln` 的 US2 測試通過

**檢查點**: US1 與 US2 可同時運作；收藏只影響辨識與卡牌庫篩選，不影響任何抽卡或統計規則。

---

## 階段 5: 使用者故事 3 - 從抽卡結果立即整理偏好 (優先級: P3)

**目標**: 使用者成功抽出餐點後，可直接在結果區收藏/取消收藏或排除/取消排除剛抽中的卡牌；操作後仍顯示同一 result card ID，不重新抽卡、不新增 history、不改變 statistics 或 rotation snapshot。

**獨立測試**: 完成一次成功抽卡，記錄 operation ID、result card ID、history count、statistics 與 rotation snapshot；在結果區提交收藏或排除，確認畫面重顯同一結果且只有偏好狀態改變。

### 使用者故事 3 的測試

> 先撰寫這些測試，確認在實作前失敗。

- [ ] T065 [P] [US3] 新增首頁結果區收藏/排除 action 整合失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/PreferenceResultActionTests.cs`
- [ ] T066 [P] [US3] 新增結果區偏好 action 不新增 history/statistics/snapshot 失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/PreferenceResultActionStatisticsTests.cs`
- [ ] T067 [P] [US3] 新增結果區 target-state 重複提交與語系切換不變性失敗測試於 `tests/CardPicker2.IntegrationTests/Pages/PreferenceResultActionLocalizationTests.cs`
- [ ] T068 [P] [US3] 新增 preference result action responsive/accessibility 失敗測試於 `tests/CardPicker2.IntegrationTests/Browser/CardPreferenceResponsiveAccessibilityTests.cs`
- [ ] T069 [P] [US3] 新增 result preference action HTML assertion helper 覆蓋測試於 `tests/CardPicker2.IntegrationTests/Pages/PreferenceHtmlAssertions.cs`
- [ ] T070 [US3] 執行 `dotnet test CardPicker2.sln --filter "PreferenceResultAction|PreferenceResultActionStatistics|PreferenceResultActionLocalization|CardPreferenceResponsiveAccessibility"`，確認 `CardPicker2.sln` 的 US3 新測試在實作前失敗

### 使用者故事 3 的實作

- [ ] T071 [US3] 更新首頁 PageModel，加入 result preference binding、`OnPostPreferenceAsync`、result restore 與 operation/result card validation 於 `CardPicker2/Pages/Index.cshtml.cs`
- [ ] T072 [US3] 更新首頁結果區 Razor UI，加入收藏/排除 target-state forms、目前偏好 badge 與 disabled blocked state 於 `CardPicker2/Pages/Index.cshtml`
- [ ] T073 [US3] 更新抽卡 replay/result projection，重顯成功結果時投影目前 preference state 但不重新 randomize 於 `CardPicker2/Services/CardLibraryService.cs`
- [ ] T074 [US3] 更新 localized result projection，確保 result card 的偏好 badge、deleted state、metadata 與 rotation summary 可並存於 `CardPicker2/Services/MealCardLocalizationService.cs`
- [ ] T075 [P] [US3] 新增繁中結果區偏好 action success/error、replay 與 blocked 文案於 `CardPicker2/Resources/SharedResource.zh-TW.resx`
- [ ] T076 [P] [US3] 新增英文結果區偏好 action success/error、replay 與 blocked 文案於 `CardPicker2/Resources/SharedResource.en-US.resx`
- [ ] T077 [US3] 執行 `dotnet test CardPicker2.sln --filter "PreferenceResultAction|PreferenceResultActionStatistics|PreferenceResultActionLocalization|CardPreferenceResponsiveAccessibility"`，確認 `CardPicker2.sln` 的 US3 測試通過

**檢查點**: 所有使用者故事均可獨立驗證；結果區偏好整理不污染已成立的抽卡事實。

---

## 階段 6: 潤飾與跨領域關注點

**目的**: 補齊安全、可觀察性、效能、公開介面邊界、雙語完整性、RWD/accessibility、XML 文件註解、coverage 與 quickstart 驗證。

- [ ] T078 [P] 擴充 Anti-Forgery 與 production HSTS/CSP 測試，覆蓋首頁 result preference、卡牌庫 preference 與詳情頁 preference forms 於 `tests/CardPicker2.IntegrationTests/SecurityHeadersTests.cs`
- [ ] T079 [P] 擴充偏好結構化日誌測試，覆蓋 schema v5 migration、preference update、invalid target、preference empty pool、draw success count 與 write failure 於 `tests/CardPicker2.UnitTests/Services/DrawLoggingTests.cs`
- [ ] T080 [P] 擴充公開介面邊界測試，確認未新增外部 JSON/API endpoint 且偏好互動維持 Razor Pages/form/query/page handler 於 `tests/CardPicker2.IntegrationTests/RouteSurfaceTests.cs`
- [ ] T081 [P] 新增偏好效能與 web-vitals smoke 測試，覆蓋首頁 GET、preference-aware draw POST、preference update POST、statistics projection 與 `/Cards` preference filter p95 於 `tests/CardPicker2.IntegrationTests/Performance/CardPreferencePerformanceTests.cs`
- [ ] T082 [P] 擴充 resource key 完整性測試，確認 `zh-TW` 與 `en-US` 無偏好相關未翻譯 key 於 `tests/CardPicker2.IntegrationTests/Pages/LocalizationResourceTests.cs`
- [ ] T083 [P] 擴充賭博式或加權暗示文案邊界測試，確認偏好文案不含保底、加權、下一次機率提高或付費暗示於 `tests/CardPicker2.UnitTests/Services/DrawCopyBoundaryTests.cs`
- [ ] T084 更新 production 安全標頭、DI wiring、Serilog 安全欄位與 service lifetime 最終檢查於 `CardPicker2/Program.cs`
- [ ] T085 更新新增或變更 public C# model/service API 的 XML 文件註解於 `CardPicker2/Models/CardPreferenceState.cs`、`CardPicker2/Models/MealCard.cs`、`CardPicker2/Models/DrawResult.cs`、`CardPicker2/Services/ICardLibraryService.cs`、`CardPicker2/Services/CardLibraryService.cs`、`CardPicker2/Services/DrawCandidatePoolBuilder.cs`、`CardPicker2/Services/MealCardLocalizationService.cs`，並以 `tests/CardPicker2.UnitTests/Documentation/PublicApiDocumentationTests.cs` 驗證
- [ ] T086 執行 `dotnet test CardPicker2.sln --filter "CardPreference|Preference|SecurityHeaders|AntiForgery|Logging|RouteSurface|CardPreferencePerformance|LocalizationResource|DrawCopyBoundary"`，確認 `CardPicker2.sln` 的 cross-cutting 測試通過
- [ ] T087 執行 `dotnet build CardPicker2.sln`，確認 `CardPicker2.sln` 無新增 build warning、formatting 或 naming 違規
- [ ] T088 執行 `dotnet test CardPicker2.sln`，確認 `CardPicker2.sln` 全部單元、整合、browser、security、performance 與 route-surface 測試通過
- [ ] T089 依 `specs/007-card-preference-controls/quickstart.md` 完成手動或 browser automation 驗證，覆蓋 schema v5、卡牌庫收藏/排除、詳情頁、排除候選池、空候選池、結果區操作、公平性、語系/主題、reduced motion、RWD、效能、安全與觀察性

---

## 相依性與執行順序

### 階段相依性

- **階段 1 設定**: 無依賴，可立即開始。
- **階段 2 基礎建設**: 依賴階段 1，且封鎖所有 user story。
- **階段 3 US1**: 依賴階段 2，是 MVP。
- **階段 4 US2**: 依賴階段 2；可在 US1 的 preference mutation contract 穩定後與後續 UI 工作平行，但必須驗證收藏不影響 US1 候選池。
- **階段 5 US3**: 依賴階段 2，並實務上需要 US1/US2 的 preference mutation 與 display contract；可用 integration tests 獨立驗證結果區不重新抽卡。
- **階段 6 潤飾**: 依賴欲交付的 user stories 完成。

### 使用者故事相依性

- **US1 (P1)**: 可在 foundation 後開始，提供 MVP；不依賴收藏功能。
- **US2 (P2)**: 可在 foundation 後開始，但 final validation 必須確認不回歸 US1 排除抽卡規則。
- **US3 (P3)**: 依賴 preference mutation 能力；完整結果區整理需要 US1 排除與 US2 收藏狀態都可更新。

### 相依圖

```text
階段 1 設定
  -> 階段 2 基礎建設
      -> US1 手動排除抽卡 (MVP)
      -> US2 收藏與偏好篩選
          -> US3 結果區偏好操作
              -> 階段 6 潤飾
```

### 每個使用者故事內部順序

- 測試任務必須先完成並確認失敗。
- 模型先於服務，服務先於 PageModel，PageModel 先於 Razor UI。
- Resource、CSS、JS 可在服務/PageModel 行為明確後平行更新。
- 每個檢查點後執行該 story 的 filter tests，再進入下一優先級。

---

## 平行處理機會

- T001 到 T005 可平行建立測試 helper，因為修改不同測試輔助檔。
- T006 到 T009 可平行撰寫 foundation 失敗測試。
- T011 到 T016 可平行建立互不依賴的模型檔案。
- US1 的 T028 到 T033 可平行撰寫失敗測試；T043 到 T046 可在 handler contract 穩定後平行處理 resources、CSS 與 JS。
- US2 的 T048 到 T053 可平行撰寫失敗測試；T062 與 T063 可平行補齊雙語 resource。
- US3 的 T065 到 T069 可平行撰寫失敗測試；T075 與 T076 可平行補齊雙語 resource。
- T078 到 T083 可平行補齊 cross-cutting 測試。

---

## 平行範例: 使用者故事 1

```bash
# 平行撰寫 US1 失敗測試:
任務: "T028 [US1] tests/CardPicker2.UnitTests/Services/CardLibraryPreferenceMutationTests.cs"
任務: "T029 [US1] tests/CardPicker2.UnitTests/Services/DrawCandidatePoolPreferenceTests.cs"
任務: "T030 [US1] tests/CardPicker2.UnitTests/Services/CardLibraryPreferenceDrawTests.cs"
任務: "T031 [US1] tests/CardPicker2.IntegrationTests/Pages/PreferenceFilteredDrawPageTests.cs"
任務: "T032 [US1] tests/CardPicker2.IntegrationTests/Pages/CardPreferencePageTests.cs"

# UI contract 穩定後平行處理文案與前端:
任務: "T043 [US1] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T044 [US1] CardPicker2/Resources/SharedResource.en-US.resx"
任務: "T045 [US1] CardPicker2/wwwroot/css/site.css"
任務: "T046 [US1] CardPicker2/wwwroot/js/site.js"
```

## 平行範例: 使用者故事 2

```bash
# 平行撰寫 US2 失敗測試:
任務: "T049 [US2] tests/CardPicker2.UnitTests/Services/CardPreferenceFilterServiceTests.cs"
任務: "T050 [US2] tests/CardPicker2.UnitTests/Services/DrawCandidatePoolPreferenceTests.cs"
任務: "T051 [US2] tests/CardPicker2.UnitTests/Services/CardLibraryPreferenceSearchTests.cs"
任務: "T052 [US2] tests/CardPicker2.IntegrationTests/Pages/PreferenceFilteredCardLibraryPageTests.cs"
任務: "T053 [US2] tests/CardPicker2.UnitTests/Services/DrawStatisticsPreferenceCompatibilityTests.cs"

# 卡牌庫 UI contract 穩定後平行處理文案:
任務: "T062 [US2] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T063 [US2] CardPicker2/Resources/SharedResource.en-US.resx"
```

## 平行範例: 使用者故事 3

```bash
# 平行撰寫 US3 失敗測試:
任務: "T065 [US3] tests/CardPicker2.IntegrationTests/Pages/PreferenceResultActionTests.cs"
任務: "T066 [US3] tests/CardPicker2.IntegrationTests/Pages/PreferenceResultActionStatisticsTests.cs"
任務: "T067 [US3] tests/CardPicker2.IntegrationTests/Pages/PreferenceResultActionLocalizationTests.cs"
任務: "T068 [US3] tests/CardPicker2.IntegrationTests/Browser/CardPreferenceResponsiveAccessibilityTests.cs"

# 結果區 UI contract 穩定後平行處理文案:
任務: "T075 [US3] CardPicker2/Resources/SharedResource.zh-TW.resx"
任務: "T076 [US3] CardPicker2/Resources/SharedResource.en-US.resx"
```

---

## 實作策略

### MVP 優先 (僅使用者故事 1)

1. 完成階段 1: 設定與測試輔助。
2. 完成階段 2: 基礎建設。
3. 完成階段 3: US1 手動排除抽卡。
4. 停下並驗證 `dotnet test CardPicker2.sln --filter "PreferenceMutation|DrawCandidatePoolPreference|CardLibraryPreferenceDraw|PreferenceFilteredDraw|CardPreferencePage|PreferenceFilteredDrawStatistics"`。
5. 若只交付 MVP，仍需完成階段 6 中與 US1 相關的安全、日誌、文件註解、build/test 與 quickstart 驗證。

### 增量交付

1. Setup + Foundation -> schema v5、偏好模型、persistence 與投影就緒。
2. US1 -> 排除抽卡與取消排除可展示，且排除卡不進候選池。
3. US2 -> 收藏與卡牌庫偏好篩選可展示，且收藏不影響抽卡公平性。
4. US3 -> 結果區可立即整理偏好，且不污染既有抽卡歷史。
5. Polish -> 安全、觀察性、效能、RWD/accessibility、coverage 與 quickstart 全驗證。

### 團隊平行策略

1. 團隊共同完成階段 1 與階段 2。
2. foundation 完成後，US1 工作者負責 draw candidate exclusion 與卡牌管理偏好 mutation，US2 工作者負責卡牌庫偏好篩選與收藏 UI，US3 工作者負責首頁結果區 POST 與 replay invariants。
3. 所有碰到 `CardPicker2/Services/CardLibraryService.cs`、`CardPicker2/Pages/Index.cshtml.cs`、`CardPicker2/Pages/Index.cshtml` 的工作需以較早 story contract 優先合併，再由後續故事補 failure/replay/UI 分支，避免同檔互相覆蓋。

---

## 備註

- `[P]` 任務代表不同檔案或明確可分段處理；若實作時發現同檔衝突，先完成較早任務再重排後續任務。
- 所有行為變更先寫失敗測試，且在對應實作後以任務中的 filter command 驗證。
- 新增或變更 public C# model/service API 必須補 XML 文件註解，且包含 `<example>` 與 `<code>`。
- 不得新增資料庫、外部 JSON API、SPA framework、收藏加權、推薦分數、保底、付費、下注、點數、稀有度或自動學習規則。
- 偏好更新、抽卡 replay、語系切換、主題切換、reduced motion、動畫時間與顯示排序不得改變已成立的 card ID、history、statistics、rotation snapshot 或候選池資料事實。

## 獨立測試條件摘要

- **US1**: 已排除 active card 仍預設顯示於 `/Cards` 且可取消排除；normal/random/metadata/rotation draw 永不抽出已排除 card；排除造成空候選池時不新增 history/statistics 並顯示可修正提示。
- **US2**: 收藏狀態在列表與詳情頁可見且可篩選；favorite filter 與 drawable/excluded filter 可和 keyword、meal type、metadata filters 交集；收藏不影響候選池、fairness、history、statistics、rotation 或 duplicate detection。
- **US3**: 結果區偏好操作保留同一 result card ID、operation ID、history、statistics 與 rotation snapshot；target-state 重複提交不反轉；語系、主題與 reduced motion 只改變呈現。

## 建議 MVP 範圍

MVP 範圍為階段 1、階段 2、階段 3，也就是 US1「手動排除不想再抽到的餐點」。完成後即可交付可逆排除狀態、排除候選池規則、卡牌庫/詳情管理入口、空候選池提示與相關測試；US2/US3 可作為後續增量。
