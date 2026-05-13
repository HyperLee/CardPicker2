# Tasks: 雙語語系切換

**Input**: Design documents from `/specs/003-bilingual-language-toggle/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/ui-contract.md`, `quickstart.md`  
**Tests**: 本功能規格與憲章明確要求測試優先；每個會改變行為、資料規則、驗證邏輯或使用者流程的故事都先列測試任務。  
**Organization**: 任務依使用者故事分組，確保每個故事可獨立實作、驗證與展示。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行，因為修改不同檔案或不依賴尚未完成的任務
- **[Story]**: 使用者故事標籤，只出現在使用者故事階段
- 每項任務都包含明確檔案路徑

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: 建立 localization resource 與測試輔助檔案，供所有故事共用。

- [ ] T001 [P] 建立 shared localization marker class `CardPicker2/Resources/SharedResource.cs`
- [ ] T002 [P] 建立繁體中文 shared resource keys `CardPicker2/Resources/SharedResource.zh-TW.resx`
- [ ] T003 [P] 建立英文 shared resource keys 並與繁中 key 完全對齊 `CardPicker2/Resources/SharedResource.en-US.resx`
- [ ] T004 [P] 建立語系頁面 HTML 斷言輔助工具 `tests/CardPicker2.IntegrationTests/Pages/LanguageHtmlAssertions.cs`
- [ ] T005 [P] 建立 ASP.NET Core culture cookie 測試輔助工具 `tests/CardPicker2.IntegrationTests/Infrastructure/LanguageCookieTestExtensions.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 完成所有故事共用的語系白名單、cookie preference、雙語卡牌資料模型、投影、schema v1/v2 載入與 localization middleware。

**Critical**: 此階段完成前，不應開始任何使用者故事的頁面實作。

### Tests First

- [ ] T006 [P] 新增 `SupportedLanguage` 與 culture cookie fallback 單元測試 `tests/CardPicker2.UnitTests/Services/LanguagePreferenceServiceTests.cs`
- [ ] T007 [P] 新增卡牌目前語系投影與英文缺漏 fallback 單元測試 `tests/CardPicker2.UnitTests/Services/MealCardLocalizationServiceTests.cs`
- [ ] T008 [P] 新增 schema v1 migration、schema v2 seed、corrupted/unsupported preserve 單元測試 `tests/CardPicker2.UnitTests/Services/CardLibraryLocalizationTests.cs`
- [ ] T009 [P] 擴充雙語與 fallback duplicate detection 單元測試 `tests/CardPicker2.UnitTests/Services/DuplicateCardDetectorTests.cs`

### Implementation

- [ ] T010 建立支援語系白名單模型與 `HtmlLang` 對應 `CardPicker2/Models/SupportedLanguage.cs`
- [ ] T011 建立語系偏好模型與 cookie 狀態 `CardPicker2/Models/LanguagePreference.cs`
- [ ] T012 建立單一語系餐點內容模型 `CardPicker2/Models/MealCardLocalizedContent.cs`
- [ ] T013 建立目前語系卡牌顯示投影模型 `CardPicker2/Models/LocalizedMealCardView.cs`
- [ ] T014 更新卡牌模型以使用 `zh-TW`/`en-US` localizations 與翻譯缺漏狀態 `CardPicker2/Models/MealCard.cs`
- [ ] T015 更新 JSON root schema 為 v2 並保留 v1 載入支援所需 metadata `CardPicker2/Models/CardLibraryDocument.cs`
- [ ] T016 更新卡牌輸入模型為 `NameZhTw`、`DescriptionZhTw`、`NameEnUs`、`DescriptionEnUs` 與 localized validation `CardPicker2/Models/MealCardInputModel.cs`
- [ ] T017 更新搜尋條件以包含目前語系與 visible-name 搜尋規則 `CardPicker2/Models/SearchCriteria.cs`
- [ ] T018 更新抽卡結果以保存 `CardId`、`MealType`、localized card projection 與 message key `CardPicker2/Models/DrawResult.cs`
- [ ] T019 實作語系偏好 service，處理 cookie 建立、白名單驗證與 safe returnUrl `CardPicker2/Services/LanguagePreferenceService.cs`
- [ ] T020 實作卡牌本地化投影 service，集中處理 fallback、missing translation 與餐別顯示 `CardPicker2/Services/MealCardLocalizationService.cs`
- [ ] T021 更新 duplicate detector 為兩語系 visible name+description 比對 `CardPicker2/Services/DuplicateCardDetector.cs`
- [ ] T022 更新預設種子資料為每餐別至少三張完整雙語卡牌 `CardPicker2/Services/SeedMealCards.cs`
- [ ] T023 更新卡牌載入流程以讀取 v1/v2、缺檔建立 v2、corrupted/unsupported 保留原檔並 block operations `CardPicker2/Services/CardLibraryService.cs`
- [ ] T024 更新卡牌服務介面以支援 localized projection、draw result card ID 與雙語 mutation contract `CardPicker2/Services/ICardLibraryService.cs`
- [ ] T025 更新載入與 mutation result 以攜帶安全 message key/arguments 而非硬編碼單語訊息 `CardPicker2/Services/CardLibraryLoadResult.cs`
- [ ] T026 更新 mutation result 以支援 localized success/failure message key 與雙語 card payload `CardPicker2/Services/CardLibraryMutationResult.cs`
- [ ] T027 註冊 ASP.NET Core localization、view/DataAnnotations localization、cookie-only request culture provider 與語系服務 `CardPicker2/Program.cs`

**Checkpoint**: localization middleware、雙語資料模型、schema migration 與投影服務已可由後續故事使用。

---

## Phase 3: User Story 1 - 在首頁切換整站語系 (Priority: P1) MVP

**Goal**: 使用者能在首頁或 shared layout 切換繁體中文與英文，首頁導覽、按鈕、功能名稱、餐別、狀態文字與目前可見餐點資訊立即使用選定語系，且不清除目前狀態。

**Independent Test**: 清除 `.AspNetCore.Culture` 後進入 `/` 應為繁體中文；POST 切換到 `en-US` 後回到首頁，`<html lang>`、導覽、首頁文案、餐別與狀態文字皆為英文；切回 `zh-TW` 後狀態與可見結果仍保留。

### Tests for User Story 1

- [ ] T028 [P] [US1] 新增首頁預設繁中、英文 cookie、切回繁中與 layout 目前語系整合測試 `tests/CardPicker2.IntegrationTests/Pages/LanguageSwitchPageTests.cs`
- [ ] T029 [P] [US1] 新增 `POST /Language?handler=Set` Anti-Forgery、culture 白名單、unsafe returnUrl 與 cookie 寫入整合測試 `tests/CardPicker2.IntegrationTests/Pages/LanguageSetHandlerTests.cs`
- [ ] T030 [P] [US1] 新增首頁語系切換保留 meal selection、coin state 與 revealed result 的 browser 測試 `tests/CardPicker2.IntegrationTests/Browser/LanguageHomeStatePreservationTests.cs`

### Implementation for User Story 1

- [ ] T031 [US1] 建立語系設定 Razor Page shell `CardPicker2/Pages/Language.cshtml`
- [ ] T032 [US1] 實作語系設定 PageModel `POST /Language?handler=Set` `CardPicker2/Pages/Language.cshtml.cs`
- [ ] T033 [US1] 建立 shared language switch partial，包含 Anti-Forgery form、目前語系與可及名稱 `CardPicker2/Pages/Shared/_LanguageSwitcher.cshtml`
- [ ] T034 [US1] 更新 shared layout 的 `<html lang>`、導覽、標題、頁尾與 language switch placement `CardPicker2/Pages/Shared/_Layout.cshtml`
- [ ] T035 [US1] 更新首頁 view 以使用 shared resources、localized meal labels、hidden result state 與 fallback prompt `CardPicker2/Pages/Index.cshtml`
- [ ] T036 [US1] 更新首頁 PageModel 以使用 localized message keys、current culture projection 與同一 `CardId` result re-render `CardPicker2/Pages/Index.cshtml.cs`
- [ ] T037 [US1] 實作首頁語系切換前後 transient state 保存與還原 `CardPicker2/wwwroot/js/site.js`
- [ ] T038 [US1] 新增 language switcher、首頁長英文文案與焦點狀態 responsive 樣式 `CardPicker2/wwwroot/css/site.css`
- [ ] T039 [US1] 補齊 layout、language switch、首頁、抽卡狀態與餐別 resource keys `CardPicker2/Resources/SharedResource.zh-TW.resx`
- [ ] T040 [US1] 補齊 layout、language switch、首頁、抽卡狀態與餐別英文 resource keys `CardPicker2/Resources/SharedResource.en-US.resx`

**Checkpoint**: User Story 1 可獨立展示：首頁與 shared layout 可在兩語系間切換，狀態不被清除。

---

## Phase 4: User Story 2 - 以選定語系使用抽卡與卡牌庫 (Priority: P2)

**Goal**: 使用者選定語系後，抽卡揭示結果、卡牌庫、詳情、隱私權與錯誤頁都以一致語系顯示，搜尋依目前語系 visible name 比對。

**Independent Test**: 在英文模式依序瀏覽 `/Cards`、card details、`/Privacy`、`/Error` 並完成搜尋與抽卡；所有導覽、功能名稱、欄位、餐別、餐點名稱/描述、無結果與 recovery 訊息皆為英文，切回繁中後同一流程為繁中。

### Tests for User Story 2

- [ ] T041 [P] [US2] 新增卡牌列表搜尋、英文 visible-name 搜尋與 fallback name 搜尋整合測試 `tests/CardPicker2.IntegrationTests/Pages/LocalizedSearchPageTests.cs`
- [ ] T042 [P] [US2] 新增抽卡結果 card ID 不因語系切換重新抽卡的整合測試 `tests/CardPicker2.IntegrationTests/Pages/LocalizedDrawPageTests.cs`
- [ ] T043 [P] [US2] 新增卡牌詳情、隱私權、錯誤頁與 shared layout 語系一致性整合測試 `tests/CardPicker2.IntegrationTests/Pages/LocalizedNonHomePageTests.cs`
- [ ] T044 [P] [US2] 新增兩語系 resource completeness 與未翻譯 key 偵測測試 `tests/CardPicker2.IntegrationTests/Pages/LocalizationResourceTests.cs`

### Implementation for User Story 2

- [ ] T045 [US2] 更新卡牌列表 PageModel 以 current culture projection 搜尋並回傳 localized cards `CardPicker2/Pages/Cards/Index.cshtml.cs`
- [ ] T046 [US2] 更新卡牌列表 view 的搜尋表單、結果數、無結果、餐別與 fallback prompt `CardPicker2/Pages/Cards/Index.cshtml`
- [ ] T047 [US2] 更新卡牌詳情 PageModel 以回傳 localized card view、not-found 與 blocked recovery 訊息 `CardPicker2/Pages/Cards/Details.cshtml.cs`
- [ ] T048 [US2] 更新卡牌詳情 view 的 labels、actions、餐點內容、fallback badge 與 delete confirmation 文案 `CardPicker2/Pages/Cards/Details.cshtml`
- [ ] T049 [US2] 更新隱私權頁面 view 與 PageModel 的 localized title/copy `CardPicker2/Pages/Privacy.cshtml` `CardPicker2/Pages/Privacy.cshtml.cs`
- [ ] T050 [US2] 更新錯誤頁面 view 與 PageModel 的安全 localized recovery copy `CardPicker2/Pages/Error.cshtml` `CardPicker2/Pages/Error.cshtml.cs`
- [ ] T051 [US2] 更新餐別顯示 helper，改由支援語系或 localizer 產生餐別名稱 `CardPicker2/Models/MealType.cs`
- [ ] T052 [US2] 更新搜尋與 draw service flow 以使用 visible localized projection 且不改變抽卡 pool `CardPicker2/Services/CardLibraryService.cs`
- [ ] T053 [US2] 擴充語系切換 state 保存以保留 `/Cards` query string、validation context 與 delete confirmation context `CardPicker2/wwwroot/js/site.js`
- [ ] T054 [US2] 補齊卡牌列表、詳情、隱私權、錯誤、搜尋、fallback 與 recovery resource keys `CardPicker2/Resources/SharedResource.zh-TW.resx`
- [ ] T055 [US2] 補齊卡牌列表、詳情、隱私權、錯誤、搜尋、fallback 與 recovery 英文 resource keys `CardPicker2/Resources/SharedResource.en-US.resx`

**Checkpoint**: User Story 1 與 User Story 2 都可獨立驗證，且語系不影響抽卡身分、搜尋條件或卡牌資料。

---

## Phase 5: User Story 3 - 維護雙語餐點內容與保留偏好 (Priority: P3)

**Goal**: 使用者可新增與編輯繁中/英文餐點名稱與描述；語系偏好可於同一瀏覽器與裝置保留；既有 v1 卡牌缺英文時以繁中 fallback 顯示並引導補齊。

**Independent Test**: 新增一張完整雙語卡牌後，在兩種語系下搜尋、詳情與抽卡都顯示同一 `CardId` 的對應語系內容；缺任一語系欄位或任一語系 duplicate 時拒絕儲存且原 JSON 不變；回訪沿用最近一次有效語系。

### Tests for User Story 3

- [ ] T056 [P] [US3] 更新雙語 required fields、invalid meal type 與 localized validation 單元測試 `tests/CardPicker2.UnitTests/Models/MealCardInputModelTests.cs`
- [ ] T057 [P] [US3] 新增 create/edit/delete 雙語欄位、fallback prompt 與 localized messages 整合測試 `tests/CardPicker2.IntegrationTests/Pages/LocalizedCardManagementPageTests.cs`
- [ ] T058 [P] [US3] 新增語系 cookie 回訪保留、無效 cookie fallback 與 cookie 不可用 graceful behavior 整合測試 `tests/CardPicker2.IntegrationTests/Pages/LanguagePreferencePersistenceTests.cs`
- [ ] T059 [P] [US3] 新增 schema v1 fallback、補齊英文後寫入 schema v2 與 atomic write 保護整合測試 `tests/CardPicker2.IntegrationTests/Pages/CardLibraryLocalizationPersistenceTests.cs`

### Implementation for User Story 3

- [ ] T060 [US3] 更新卡牌表單 partial 為繁中/英文 name/description 欄位、localized labels 與 validation spans `CardPicker2/Pages/Cards/_CardForm.cshtml`
- [ ] T061 [US3] 更新建立卡牌 view 的 localized title、blocked recovery 與雙語 submit/cancel controls `CardPicker2/Pages/Cards/Create.cshtml`
- [ ] T062 [US3] 更新建立卡牌 PageModel 以處理雙語 input、localized ModelState、duplicate failure 與 success redirect `CardPicker2/Pages/Cards/Create.cshtml.cs`
- [ ] T063 [US3] 更新編輯卡牌 view 的 localized title、fallback prompt、雙語欄位與 submit/cancel controls `CardPicker2/Pages/Cards/Edit.cshtml`
- [ ] T064 [US3] 更新編輯卡牌 PageModel 以載入 v1 fallback、要求補齊英文、拒絕 duplicate 並保留原卡 `CardPicker2/Pages/Cards/Edit.cshtml.cs`
- [ ] T065 [US3] 更新詳情頁 delete handler 的 localized confirmation、failure、success 與 state preservation hooks `CardPicker2/Pages/Cards/Details.cshtml.cs`
- [ ] T066 [US3] 更新 create/update/delete mutation 以驗證雙語欄位、跨語系 duplicate、schema v2 atomic write 與 localized result keys `CardPicker2/Services/CardLibraryService.cs`
- [ ] T067 [US3] 更新 runtime seed JSON 為 schema v2 完整雙語資料 `CardPicker2/data/cards.json`
- [ ] T068 [US3] 擴充表單語系切換 transient state 保存，涵蓋 create/edit validation 與未送出欄位 `CardPicker2/wwwroot/js/site.js`
- [ ] T069 [US3] 補齊 create/edit/delete、雙語欄位、duplicate、required-field 與 preference persistence resource keys `CardPicker2/Resources/SharedResource.zh-TW.resx`
- [ ] T070 [US3] 補齊 create/edit/delete、雙語欄位、duplicate、required-field 與 preference persistence 英文 resource keys `CardPicker2/Resources/SharedResource.en-US.resx`

**Checkpoint**: 所有使用者故事都可獨立運作，新增與編輯後的卡牌具備完整雙語內容，偏好可於回訪保留。

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: 完成 responsive、可及性、安全標頭、效能、結構化日誌、文件註解、覆蓋率與整體品質驗證。

- [ ] T071 [P] 新增兩語系 responsive、水平溢出與 axe smoke browser 測試 `tests/CardPicker2.IntegrationTests/Browser/LanguageResponsiveAccessibilityTests.cs`
- [ ] T072 [P] 新增語系切換、無效 cookie fallback、翻譯缺漏、schema migration/write failure、validation failure 與 draw success 的安全 structured logging 測試 `tests/CardPicker2.UnitTests/Services/LocalizationLoggingTests.cs`
- [ ] T073 [P] 新增 production HSTS/CSP、Anti-Forgery 與安全錯誤訊息自動化整合測試 `tests/CardPicker2.IntegrationTests/SecurityHeadersTests.cs`
- [ ] T074 [P] 新增語系切換與主要頁面 render/handler 效能 smoke 測試，驗證 1 秒互動回應與 plan 效能預算 `tests/CardPicker2.IntegrationTests/Performance/LanguagePerformanceTests.cs`
- [ ] T075 [P] 新增公開服務與模型 XML 文件註解檢查測試，涵蓋需要示例的 `<example>` 或 `<code>` `tests/CardPicker2.UnitTests/Documentation/PublicApiDocumentationTests.cs`
- [ ] T076 [P] 更新測試專案 coverage collector 設定以支援憲章 80% 關鍵業務邏輯覆蓋率閘門 `tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj` `tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj`
- [ ] T077 [P] 更新雙語長字串、fallback badge、語系切換與表單在 mobile/tablet/desktop 的最終樣式 `CardPicker2/wwwroot/css/site.css`
- [ ] T078 [P] 更新 client-side localization state 程式碼，確保不保存完整餐點資料、秘密值、stack trace 或完整 JSON `CardPicker2/wwwroot/js/site.js`
- [ ] T079 更新語系切換、偏好套用、翻譯缺漏、schema migration/write failure、validation failure 與 draw success 的 structured logging，且不得記錄未清理輸入、完整 JSON、秘密值或 stack trace `CardPicker2/Services/LanguagePreferenceService.cs` `CardPicker2/Services/MealCardLocalizationService.cs` `CardPicker2/Services/CardLibraryService.cs` `CardPicker2/Pages/Index.cshtml.cs`
- [ ] T080 執行 `dotnet test CardPicker2.sln --filter Language` 並依 `specs/003-bilingual-language-toggle/quickstart.md` 確認語系測試通過
- [ ] T081 執行 `dotnet test CardPicker2.sln --filter Localization` 並依 `specs/003-bilingual-language-toggle/quickstart.md` 確認 localization 測試通過
- [ ] T082 執行 `dotnet test CardPicker2.sln --filter Bilingual` 並依 `specs/003-bilingual-language-toggle/quickstart.md` 確認雙語資料測試通過
- [ ] T083 執行 `dotnet test CardPicker2.sln --filter "Logging|SecurityHeaders|Performance|Documentation"` 並依 `.specify/memory/constitution.md` 確認結構化日誌、安全標頭、效能與 XML 文件註解閘門通過
- [ ] T084 執行完整 `dotnet test CardPicker2.sln` 並依 `specs/003-bilingual-language-toggle/quickstart.md` 確認所有單元與整合測試通過
- [ ] T085 執行 `dotnet test CardPicker2.sln --collect:"XPlat Code Coverage"` 並依 `.specify/memory/constitution.md` 確認關鍵業務邏輯測試覆蓋率達 80% 以上，或在 PR handoff 中記錄合理例外
- [ ] T086 執行 `dotnet build CardPicker2.sln` 並依 `.specify/memory/constitution.md` 確認無新增警告、格式違規與 XML 文件註解違規
- [ ] T087 依 `specs/003-bilingual-language-toggle/quickstart.md` 手動或 browser automation 驗證 390x844、768x1024、1366x768 兩語系無重疊與水平溢出
- [ ] T088 依 `specs/003-bilingual-language-toggle/contracts/ui-contract.md` 驗證 production HSTS/CSP、Anti-Forgery、安全錯誤訊息與自動化 `SecurityHeadersTests` 仍符合契約

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 無前置依賴，可立即開始
- **Foundational (Phase 2)**: 依賴 Setup；完成前阻擋所有使用者故事
- **User Story 1 (Phase 3)**: 依賴 Foundational；MVP 範圍
- **User Story 2 (Phase 4)**: 依賴 Foundational；可在 US1 之後驗證完整站內頁面，也可由不同開發者平行處理頁面檔案
- **User Story 3 (Phase 5)**: 依賴 Foundational；可在 US1/US2 頁面基礎上完成雙語維護與偏好保留
- **Polish (Final Phase)**: 依賴預定交付的使用者故事完成

### User Story Dependencies

- **US1 (P1)**: 只依賴 Foundational，可作為 MVP 獨立完成與展示
- **US2 (P2)**: 依賴 Foundational；與 US1 共用 shared layout 與 language handler，但卡牌庫、詳情、搜尋、抽卡投影可獨立驗證
- **US3 (P3)**: 依賴 Foundational；與 US2 共用 projection/fallback 規則，但 create/edit/delete 與 preference persistence 可獨立驗證

### Within Each User Story

- 測試任務必須先撰寫並確認在實作前失敗
- 模型與 service contract 先於 PageModel 與 Razor view
- PageModel 協調 binding、service calls、ModelState、redirect 與 feedback；核心規則留在 services
- 每個 checkpoint 都應執行對應故事的 filter 測試，再進入下一個優先級

---

## Parallel Opportunities

- Setup 階段的 T001-T005 可平行處理，因為分別建立 resource marker、resource files 與 test helpers
- Foundational 測試 T006-T009 可平行撰寫；實作 T010-T018 可依模型檔案平行處理後再整合到 services
- US1 測試 T028-T030 可平行撰寫；T031-T033 可平行處理 language page 與 shared partial，T034-T040 需整合 layout/home/resources
- US2 測試 T041-T044 可平行撰寫；T045-T051 可依頁面分工，T052-T055 在投影與 resource 完成後整合
- US3 測試 T056-T059 可平行撰寫；T060-T065 可依 create/edit/details/form 分工，T066-T070 在雙語 mutation contract 確定後整合
- Polish T071-T078 可平行處理，T079 在 logging 測試定義後整合實作，T080-T088 依驗證命令、coverage 與手動檢查順序執行

---

## Parallel Example: User Story 1

```bash
# 可同時交給不同代理或開發者的測試任務：
Task: "T028 [US1] tests/CardPicker2.IntegrationTests/Pages/LanguageSwitchPageTests.cs"
Task: "T029 [US1] tests/CardPicker2.IntegrationTests/Pages/LanguageSetHandlerTests.cs"
Task: "T030 [US1] tests/CardPicker2.IntegrationTests/Browser/LanguageHomeStatePreservationTests.cs"

# language page 與 partial 可先平行建立，再整合 layout：
Task: "T031 [US1] CardPicker2/Pages/Language.cshtml"
Task: "T032 [US1] CardPicker2/Pages/Language.cshtml.cs"
Task: "T033 [US1] CardPicker2/Pages/Shared/_LanguageSwitcher.cshtml"
```

## Parallel Example: User Story 2

```bash
# 搜尋、抽卡與非首頁 localization 測試可平行：
Task: "T041 [US2] tests/CardPicker2.IntegrationTests/Pages/LocalizedSearchPageTests.cs"
Task: "T042 [US2] tests/CardPicker2.IntegrationTests/Pages/LocalizedDrawPageTests.cs"
Task: "T043 [US2] tests/CardPicker2.IntegrationTests/Pages/LocalizedNonHomePageTests.cs"

# 卡牌列表與詳情頁可分工：
Task: "T045 [US2] CardPicker2/Pages/Cards/Index.cshtml.cs"
Task: "T047 [US2] CardPicker2/Pages/Cards/Details.cshtml.cs"
Task: "T050 [US2] CardPicker2/Pages/Error.cshtml"
```

## Parallel Example: User Story 3

```bash
# 雙語表單、偏好保留與 schema persistence 測試可平行：
Task: "T056 [US3] tests/CardPicker2.UnitTests/Models/MealCardInputModelTests.cs"
Task: "T058 [US3] tests/CardPicker2.IntegrationTests/Pages/LanguagePreferencePersistenceTests.cs"
Task: "T059 [US3] tests/CardPicker2.IntegrationTests/Pages/CardLibraryLocalizationPersistenceTests.cs"

# create/edit/details 頁面可分工：
Task: "T061 [US3] CardPicker2/Pages/Cards/Create.cshtml"
Task: "T063 [US3] CardPicker2/Pages/Cards/Edit.cshtml"
Task: "T065 [US3] CardPicker2/Pages/Cards/Details.cshtml.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational
3. 完成 Phase 3: User Story 1
4. 停下並驗證：執行 `dotnet test CardPicker2.sln --filter Language`
5. Demo 首頁語系切換、目前語系顯示與狀態保留

### Incremental Delivery

1. Setup + Foundational：建立 localization middleware、resource、雙語模型與 schema v2 基礎
2. US1：交付首頁與 shared layout 語系切換 MVP
3. US2：交付卡牌庫、詳情、搜尋、抽卡結果與非首頁一致語系
4. US3：交付雙語卡牌維護、preference persistence、schema v1 fallback 與 v2 寫入
5. Polish：完成 responsive、可及性、安全標頭與完整測試驗證

### Quality Gates

- 每個故事的測試先於實作，且先確認失敗
- 所有 state-changing forms 保留 Anti-Forgery
- corrupted/unsupported `cards.json` 不得被 seed 或 migration 覆蓋
- 語系切換不得改變 `CardId`、`MealType`、抽卡機率、搜尋條件、未送出輸入或刪除確認狀態
- UI 在 `zh-TW` 與 `en-US` 下不得顯示未翻譯 key、空白餐點名稱、秘密值、stack trace 或完整內部資料
- 關鍵語系、翻譯缺漏、schema、驗證與抽卡事件必須有安全 structured logging，且不得記錄秘密值、未清理輸入或完整 JSON
- 主要頁面與語系切換必須符合 plan 效能預算，並以 smoke 測試或 browser automation 留下證據
- 公開服務與模型的非平凡行為必須具備 XML 文件註解；需要示例的 API 必須含 `<example>` 或 `<code>`
- 關鍵業務邏輯測試覆蓋率必須達 80% 以上，或在 PR handoff 中記錄合理例外與替代驗證

## Task Summary

- **Total tasks**: 88
- **Setup tasks**: 5
- **Foundational tasks**: 22
- **US1 tasks**: 13
- **US2 tasks**: 15
- **US3 tasks**: 15
- **Polish tasks**: 18
- **Suggested MVP scope**: Phase 1 + Phase 2 + User Story 1
- **Format validation**: 所有 checklist task 均使用 `- [ ] T###` 格式；使用者故事任務包含 `[US1]`、`[US2]` 或 `[US3]`；可平行任務以 `[P]` 標示；每項任務都包含至少一個檔案路徑。
