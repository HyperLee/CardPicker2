# 任務清單: 網站主題模式切換

**輸入**: `/specs/002-theme-mode-toggle/` 的設計文件
**前置文件**: plan.md、spec.md、research.md、data-model.md、contracts/ui-contract.md、quickstart.md

**測試**: 依 CardPicker2 憲章與本功能計畫，主題行為、瀏覽器偏好、跨頁套用、安全標頭、可及性與資料不變性變更 MUST 先建立失敗測試，再實作。

**組織方式**: 任務依 P1、P2、P3 使用者故事分組；每個故事完成後都必須可獨立展示與驗證。

## 格式: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行，必須是不同檔案且無相依衝突
- **[Story]**: 使用者故事任務才使用，例如 [US1]、[US2]、[US3]
- Description 均包含精確檔案路徑

## Phase 1: Setup，共用基礎

**目的**: 建立主題功能所需的 browser automation 與測試輔助基礎。

- [ ] T001 更新 `tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj` 加入 Microsoft.Playwright、可及性 smoke 測試所需套件與 browser automation 測試設定，至少設定 Chromium、Firefox、WebKit；Safari/Edge 無法自動化時須記錄手動驗證步驟
- [ ] T002 [P] 建立 Playwright 測試 fixture 於 `tests/CardPicker2.IntegrationTests/Browser/ThemeBrowserFixture.cs`，負責啟動 WebApplicationFactory、建立 Chromium/Firefox/WebKit browser context、mobile touch/pointer context 與共用 base URL
- [ ] T003 [P] 建立主題 HTML assertion helper 於 `tests/CardPicker2.IntegrationTests/Pages/ThemeModeHtmlAssertions.cs`，封裝首頁主題控制項與非首頁無控制項檢查

---

## Phase 2: Foundational，阻塞性前置工作

**目的**: 完成所有主題故事共用且會阻塞測試與驗證的基礎。

**CRITICAL**: 此階段完成前不得開始任何使用者故事實作。

- [ ] T004 [P] 建立共用暫存卡牌庫 fixture 於 `tests/CardPicker2.IntegrationTests/Infrastructure/TempCardLibrary.cs`，供主題測試驗證不污染 `CardPicker2/data/cards.json`
- [ ] T005 [P] 建立主要頁面測試資料於 `tests/CardPicker2.IntegrationTests/Pages/ThemeControlledSurfaceData.cs`，列出 `/`、`/Privacy`、`/Error`、`/Cards`、`/Cards/11111111-1111-1111-1111-111111111111`、`/Cards/Create`、`/Cards/Edit/11111111-1111-1111-1111-111111111111`
- [ ] T006 更新 `tests/CardPicker2.IntegrationTests/SecurityHeadersTests.cs` 的 CSP assertion helper，準備驗證 production HSTS、`default-src 'self'` 與主題 bootstrap script 明確允許策略

**Checkpoint**: Browser 測試 fixture、頁面測試資料與安全標頭 assertion 可被後續故事重用。

---

## Phase 3: User Story 1 - 在首頁切換整站主題 (Priority: P1) MVP

**Goal**: 使用者在首頁選擇亮色、暗黑或跟隨系統模式後，首頁立即更新，後續瀏覽的站內頁面套用一致有效主題，且首頁以外頁面不提供主題選擇控制項。

**Independent Test**: 進入首頁逐一檢查三個主題選項和值，切換後確認 `html` 主題 attribute 更新；瀏覽主要站內頁面確認套用 shared layout 主題且不顯示主題控制項。

### Tests for User Story 1，必須先失敗

- [ ] T007 [US1] 新增 Razor HTML contract 測試於 `tests/CardPicker2.IntegrationTests/Pages/ThemeModePageTests.cs`，驗證首頁輸出「亮色模式」、「暗黑模式」、「跟隨系統」與 value `light`、`dark`、`system`
- [ ] T008 [P] [US1] 新增非首頁無主題控制項測試於 `tests/CardPicker2.IntegrationTests/Pages/ThemeModeNonHomePageTests.cs`，覆蓋 `/Privacy`、`/Error`、`/Cards`、詳情、建立與編輯頁
- [ ] T009 [P] [US1] 更新 production CSP 測試於 `tests/CardPicker2.IntegrationTests/SecurityHeadersTests.cs`，驗證主題 head script 以 hash、nonce 或等效明確策略允許且不移除 HSTS
- [ ] T010 [US1] 新增首頁主題 browser behavior 測試於 `tests/CardPicker2.IntegrationTests/Browser/ThemeModeBrowserTests.cs` 並執行 `dotnet test CardPicker2.sln --filter "ThemeModePageTests|ThemeModeNonHomePageTests|SecurityHeadersTests|ThemeModeBrowserTests"`，驗證滑鼠、鍵盤與 mobile pointer/touch 選擇 `light`、`dark`、`system` 後 1 秒內更新 `data-bs-theme`/`data-theme-mode`、後續瀏覽 `/Cards` 或 `/Privacy` 沿用同一選取模式、`localStorage` 為 `dark` 或 `system` 時首次可見呈現前已由 head bootstrap script 套用有效主題，且依 `specs/002-theme-mode-toggle/quickstart.md` 確認 US1 新增測試在實作前失敗

### Implementation for User Story 1

- [ ] T011 [US1] 更新 `CardPicker2/Pages/Shared/_Layout.cshtml`，在 Bootstrap 與 `site.css` 前加入最小 head bootstrap script 並設定 `data-bs-theme`、`data-theme-mode`
- [ ] T012 [US1] 更新 `CardPicker2/Pages/Index.cshtml`，新增首頁唯一的主題模式 fieldset/radio segmented control 並使用繁體中文標示
- [ ] T013 [US1] 更新 `CardPicker2/wwwroot/js/site.js`，加入主題模式白名單驗證、目前頁面立即套用、localStorage 寫入與首頁 radio checked state 同步
- [ ] T014 [US1] 更新 `CardPicker2/wwwroot/css/site.css`，加入 light/dark CSS custom properties、首頁主題控制項樣式、可見焦點與既有 slot/card/form surface 的基礎主題色
- [ ] T015 [US1] 更新 `CardPicker2/Program.cs`，讓 production CSP 明確允許 `_Layout.cshtml` 的主題 bootstrap script 並保留 `default-src 'self'`、HSTS 與現有本機靜態資源限制
- [ ] T016 [US1] 執行 `dotnet test CardPicker2.sln --filter "ThemeModePageTests|ThemeModeNonHomePageTests|SecurityHeadersTests|ThemeModeBrowserTests"` 並依 `specs/002-theme-mode-toggle/quickstart.md` 確認 US1 測試通過

**Checkpoint**: User Story 1 可獨立展示為 MVP。

---

## Phase 4: User Story 2 - 跟隨系統外觀偏好 (Priority: P2)

**Goal**: 使用者選擇「跟隨系統」後，網站依 browser/system color scheme 推導有效主題；系統外觀變更時 2 秒內更新，明確選擇 light/dark 時不受系統變更影響。

**Independent Test**: 以 browser automation 模擬 `prefers-color-scheme` 為 light/dark，設定 `system` 模式後確認有效主題正確；切換系統偏好時確認 2 秒內更新，改選 light/dark 後確認不再跟隨系統變更。

### Tests for User Story 2，必須先失敗

- [ ] T017 [US2] 新增 system 模式 browser behavior 測試於 `tests/CardPicker2.IntegrationTests/Browser/ThemeModeBrowserTests.cs`，驗證 `system` 在 light/dark browser color scheme 下推導 `data-bs-theme`
- [ ] T018 [US2] 擴充 `tests/CardPicker2.IntegrationTests/Browser/ThemeModeBrowserTests.cs`，驗證 system preference change event 於 2 秒內更新有效主題，且 `light`、`dark` 明確模式忽略系統變更
- [ ] T019 [US2] 執行 `dotnet test CardPicker2.sln --filter ThemeModeBrowserTests` 並依 `specs/002-theme-mode-toggle/quickstart.md` 確認 US2 新增測試在實作前失敗

### Implementation for User Story 2

- [ ] T020 [US2] 更新 `CardPicker2/wwwroot/js/site.js`，加入 `matchMedia('(prefers-color-scheme: dark)')` change listener、`addEventListener`/`addListener` 相容處理與 system 模式即時重新推導
- [ ] T021 [US2] 更新 `CardPicker2/Pages/Shared/_Layout.cshtml`，讓 head bootstrap script 在 `matchMedia` 不存在或丟出例外時回到 `data-theme-mode="system"` 與 `data-bs-theme="light"`
- [ ] T022 [US2] 更新 `CardPicker2/wwwroot/css/site.css`，補齊 `color-scheme`、dark effective theme 下的導覽、按鈕、表單、警示與 focus token
- [ ] T023 [US2] 執行 `dotnet test CardPicker2.sln --filter ThemeModeBrowserTests` 並依 `specs/002-theme-mode-toggle/quickstart.md` 確認 US2 通過且 US1 不回歸

**Checkpoint**: User Story 1 與 User Story 2 均可獨立驗證。

---

## Phase 5: User Story 3 - 保留偏好並維持可讀性 (Priority: P3)

**Goal**: 同一瀏覽器與裝置回訪時沿用最近選擇的主題模式；跨分頁 2 秒內同步；三種模式在主要頁面與常見 viewport 下維持可讀、可操作，且不改變餐點資料、搜尋條件、表單輸入或抽卡結果。

**Independent Test**: 設定主題後重新載入與開新分頁，確認 localStorage 偏好仍生效且跨分頁同步；在手機、平板、桌面 viewport 檢查主要頁面無水平溢出與焦點可見；切換主題後確認卡牌資料檔、搜尋條件、表單輸入與抽卡結果未變。

### Tests for User Story 3，必須先失敗

- [ ] T024 [US3] 擴充 `tests/CardPicker2.IntegrationTests/Browser/ThemeModeBrowserTests.cs`，驗證 localStorage 保存 `cardpicker.theme.mode`、無效值回到 `system`、回訪首次可見呈現前套用有效主題，以及 localStorage 讀取/寫入例外會安全 fallback 並只輸出非敏感 console warning 名稱 `CardPickerThemePreferenceReadFailed` 或 `CardPickerThemePreferenceWriteFailed`
- [ ] T025 [P] [US3] 新增跨分頁同步測試於 `tests/CardPicker2.IntegrationTests/Browser/ThemeModeStorageSyncTests.cs`，驗證另一分頁收到 `storage` event 後 2 秒內更新有效主題且不顯示首頁控制項，並驗證 storage event 處理失敗只輸出非敏感 console warning 名稱 `CardPickerThemeSyncFailed`
- [ ] T026 [P] [US3] 新增狀態不變性測試於 `tests/CardPicker2.IntegrationTests/Browser/ThemeModeStateIntegrityTests.cs`，驗證切換主題不清除抽卡結果、搜尋 query、建立表單輸入、validation message 或 `CardPicker2/data/cards.json`
- [ ] T027 [P] [US3] 新增 responsive 與可及性 smoke 測試於 `tests/CardPicker2.IntegrationTests/Browser/ThemeModeResponsiveTests.cs`，覆蓋 390x844、768x1024、1366x768 與 light/dark/system 模式下 `scrollWidth == clientWidth`、鍵盤焦點可見、主題控制鍵盤切換、mobile pointer/touch 切換、automated axe 或等效檢查無重大可及性違規；若工具無法完整量測 WCAG 2.2 AA 對比，測試輸出或 quickstart 紀錄必須要求人工 contrast/focus 證據
- [ ] T028 [US3] 執行 `dotnet test CardPicker2.sln --filter "ThemeModeBrowserTests|ThemeModeStorageSyncTests|ThemeModeStateIntegrityTests|ThemeModeResponsiveTests"` 並依 `specs/002-theme-mode-toggle/quickstart.md` 確認 US3 新增測試在實作前失敗

### Implementation for User Story 3

- [ ] T029 [US3] 更新 `CardPicker2/wwwroot/js/site.js`，加入 localStorage read/write try-catch、無效值清理或忽略、storage event 同步、不重載頁面/不提交表單的主題切換流程，以及只輸出 `CardPickerThemePreferenceReadFailed`、`CardPickerThemePreferenceWriteFailed`、`CardPickerThemeSyncFailed` 等非敏感 console warning 名稱的診斷行為
- [ ] T030 [US3] 更新 `CardPicker2/Pages/Shared/_Layout.cshtml`，調整 navbar/footer class 為 theme-aware Bootstrap class，避免 `text-dark`、`bg-white` 在 dark effective theme 下破壞可讀性
- [ ] T031 [US3] 更新 `CardPicker2/wwwroot/css/site.css`，補齊 card library、detail、create/edit form、delete panel、validation summary、empty state、slot result 在 light/dark/system 下的對比與 responsive 尺寸
- [ ] T032 [US3] 更新 `CardPicker2/Pages/Index.cshtml`，確保主題控制項不包在狀態變更表單內且切換時不影響 `MealType`、`CoinInserted` 或目前揭示結果
- [ ] T033 [US3] 執行 `dotnet test CardPicker2.sln --filter "ThemeModeBrowserTests|ThemeModeStorageSyncTests|ThemeModeStateIntegrityTests|ThemeModeResponsiveTests"` 並依 `specs/002-theme-mode-toggle/quickstart.md` 確認 US3 通過且 US1、US2 不回歸

**Checkpoint**: 所有主題故事均可獨立驗證，且不影響餐點抽卡、搜尋與卡牌管理流程。

---

## Phase 6: Polish 與跨切面工作

**目的**: 完成文件、品質、安全、效能與憲章合規檢查。

- [ ] T034 [P] 更新 `specs/002-theme-mode-toggle/quickstart.md`，補上 Playwright 安裝、browser automation 執行方式、主題驗收紀錄格式與手動 fallback 驗證步驟
- [ ] T035 [P] 更新 `specs/002-theme-mode-toggle/contracts/ui-contract.md`，記錄最終採用的 CSP hash/nonce 或等效明確允許策略與 DOM attribute contract
- [ ] T036 執行 `dotnet format CardPicker2.sln --verify-no-changes` 並針對 `CardPicker2.sln` 修正任何格式化差異
- [ ] T037 執行 `dotnet build CardPicker2.sln` 並針對 `CardPicker2.sln` 修正所有新增 warning 或 error
- [ ] T038 執行 `dotnet test CardPicker2.sln` 並針對 `CardPicker2.sln` 確認既有餐點抽卡、搜尋、卡牌管理與所有主題測試均通過
- [ ] T039 以 browser automation 或手動驗證 `CardPicker2/wwwroot/css/site.css` 與 `CardPicker2/wwwroot/js/site.js` 在主要頁面符合 Chromium/Firefox/WebKit browser matrix、FCP/LCP、1 秒主題切換、2 秒跨分頁同步、鍵盤/觸控操作、WCAG 2.2 AA 對比、可見焦點與無水平溢出要求；Safari/Edge 若無法自動化，須在驗證紀錄中補手動結果
- [ ] T040 檢查 `CardPicker2/Program.cs`、`CardPicker2/wwwroot/js/site.js` 與 `CardPicker2/Pages/Shared/_Layout.cshtml`，確認 UI、console 診斷與 headers 不包含秘密值、完整資料檔內容、stack trace、系統提示或未清理的 localStorage 值，且 localStorage/sync 失敗只輸出允許的非敏感診斷名稱

---

## 相依性與執行順序

### Phase Dependencies

- **Setup (Phase 1)**: 無相依性，可立即開始
- **Foundational (Phase 2)**: 依賴 Setup，會阻塞所有使用者故事
- **US1 (Phase 3)**: 依賴 Foundational，是 MVP
- **US2 (Phase 4)**: 依賴 Foundational 與 US1 的主題套用基礎
- **US3 (Phase 5)**: 依賴 Foundational 與 US1；可在 US2 測試完成後與 US2 實作小心協調，但 `site.js`、`site.css` 同檔案修改需序列化
- **Polish (Phase 6)**: 依賴所有選定使用者故事完成

### User Story Dependencies

- **User Story 1 (P1)**: Foundational 完成後可開始，不依賴其他故事；建議作為 MVP
- **User Story 2 (P2)**: 依賴 US1 的 DOM attribute、radio control 與基本 apply logic
- **User Story 3 (P3)**: 依賴 US1 的 persistence key 與基本 apply logic；跨分頁與可讀性檢查可在 US2 後完成

### Within Each User Story

- 測試 MUST 先寫並先失敗
- Browser behavior 測試先於 `site.js` 行為實作
- HTML contract 測試先於 Razor markup 實作
- CSP 測試先於 `Program.cs` CSP 變更
- 每個故事完成後 MUST 執行該故事測試並確認較高優先級故事不回歸

### Parallel Opportunities

- **Setup**: T002 與 T003 可平行
- **Foundational**: T004 與 T005 可平行
- **US1**: T007、T008、T009、T010 可由不同人平行撰寫；實作時 T011/T012/T013/T014 需按整合順序合併，且 T010 的 browser behavior 測試必須先失敗後才能修改 `_Layout.cshtml`、`Index.cshtml`、`site.js` 或 `site.css`
- **US2**: T017 與 T018 修改同檔案不可平行；T020、T021、T022 分別修改不同檔案但需以測試結果協調
- **US3**: T025、T026、T027 可平行；T029、T030、T031、T032 涉及不同檔案但 `site.js`/`site.css` 需避免與 US2 同時衝突
- **Polish**: T034 與 T035 可平行，T036-T040 應在所有實作合併後序列執行

---

## 實作策略

### MVP First，只完成 User Story 1

1. 完成 Phase 1 與 Phase 2。
2. 撰寫 T007-T010 並確認 US1 測試失敗。
3. 完成 T011-T015。
4. 執行 T016，確認首頁三模式控制、browser behavior、整站套用、非首頁無控制項與 production CSP/HSTS 通過。

### Incremental Delivery

1. 交付 US1 作為可展示 MVP。
2. 交付 US2，加入 system preference 動態跟隨且確認 US1 不回歸。
3. 交付 US3，加入 localStorage 回訪、跨分頁同步、可讀性與資料不變性驗證。
4. 完成 Polish，執行格式化、建置、完整測試與 quickstart 驗證。

### Parallel Team Strategy

1. 一人完成 Setup/Foundational，另一人可準備 US1 HTML/CSP 測試。
2. US1 完成後，US2 browser behavior 測試與 US3 responsive/state integrity 測試可平行撰寫。
3. 所有修改 `CardPicker2/wwwroot/js/site.js` 與 `CardPicker2/wwwroot/css/site.css` 的實作任務需序列合併，避免覆蓋主題行為或視覺 token。

---

## Notes

- 所有使用者可見文字與文件維持繁體中文 zh-TW。
- 主題偏好只保存 `light`、`dark`、`system`，不得保存有效主題或任何餐點資料。
- 主題切換不得修改 `CardPicker2/data/cards.json`、抽卡結果、搜尋條件、validation message 或未送出的表單資料。
- 不得使用 bulk deletion 指令；若需要缺檔情境，只能針對單一檔案路徑操作。
- 每個 checkpoint 都必須提供測試輸出或手動驗證證據。
