# 任務清單: 餐點抽卡網站2

**輸入**: `/specs/001-casino-meal-picker/` 的設計文件
**前置文件**: plan.md（必要）、spec.md（使用者故事必要）、research.md、data-model.md、contracts/ui-contract.md、quickstart.md

**測試**: CardPicker2 憲章要求任何行為、資料規則、驗證邏輯或使用者流程變更 MUST 先建立失敗測試。本任務清單在 foundational 與各使用者故事的實作前安排對應單元測試與整合測試。

**組織方式**: 任務依使用者故事分組，確保 US1 抽卡、US2 瀏覽搜尋、US3 卡牌維護皆可獨立實作、展示與驗證。

## 格式: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行，必須是不同檔案且無相依衝突
- **[Story]**: 任務所屬使用者故事，例如 US1、US2、US3
- Description MUST 包含精確檔案路徑
- 使用者面向文件與任務描述 MUST 使用繁體中文 zh-TW

## 路徑慣例

- **Web app**: `CardPicker2/` 為 Razor Pages 應用程式
- **頁面**: `CardPicker2/Pages/`
- **模型**: `CardPicker2/Models/`
- **服務**: `CardPicker2/Services/`
- **靜態資源**: `CardPicker2/wwwroot/`
- **測試**: `tests/CardPicker2.UnitTests/` 與 `tests/CardPicker2.IntegrationTests/`

## Phase 1: Setup，共用基礎

**目的**: 建立功能所需的專案、測試專案、套件與目錄基礎。

- [X] T001 建立功能目錄結構於 CardPicker2/Models/、CardPicker2/Services/、CardPicker2/data/、CardPicker2/Pages/Cards/、tests/CardPicker2.UnitTests/、tests/CardPicker2.IntegrationTests/
- [X] T002 建立 xUnit 單元測試專案並加入方案於 tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj 與 CardPicker2.sln
- [X] T003 建立 xUnit 整合測試專案並加入方案於 tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj 與 CardPicker2.sln
- [X] T004 設定測試與日誌套件參考於 CardPicker2/CardPicker2.csproj、tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj、tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj

---

## Phase 2: Foundational，阻塞性前置工作

**目的**: 完成所有使用者故事共用且會阻塞後續工作的資料模型、持久化、驗證、安全與可觀察性基礎。

**CRITICAL**: 此階段完成前不得開始任何使用者故事實作。

### Tests for Foundational，必須先失敗

- [X] T005 [P] 新增 MealCardInputModel 必填欄位與非法餐別驗證測試於 tests/CardPicker2.UnitTests/Models/MealCardInputModelTests.cs
- [X] T006 [P] 新增 DuplicateCardDetector trim 與大小寫不敏感重複判斷測試於 tests/CardPicker2.UnitTests/Services/DuplicateCardDetectorTests.cs
- [X] T007 [P] 新增 CardLibraryService 缺檔建種子、腐敗 JSON 保留阻斷、不支援 schemaVersion、必要欄位缺失、非法餐別、持久化資料重複與原子寫入失敗不污染資料測試於 tests/CardPicker2.UnitTests/Services/CardLibraryServiceTests.cs
- [X] T008 執行 foundational 新增測試並確認實作前失敗於 tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj

### Implementation for Foundational

- [X] T009 [P] 建立 MealType 列舉與繁中顯示名稱輔助方法於 CardPicker2/Models/MealType.cs
- [X] T010 [P] 建立不可變 ID 的 MealCard 領域模型與 XML 文件註解於 CardPicker2/Models/MealCard.cs
- [X] T011 [P] 建立含 Data Annotations 與繁中驗證訊息的 MealCardInputModel 於 CardPicker2/Models/MealCardInputModel.cs
- [X] T012 [P] 建立 schemaVersion 與 cards 根文件模型於 CardPicker2/Models/CardLibraryDocument.cs
- [X] T013 [P] 建立搜尋條件模型與輸入正規化邏輯於 CardPicker2/Models/SearchCriteria.cs
- [X] T014 [P] 建立抽卡狀態與抽卡結果模型於 CardPicker2/Models/DrawOperationState.cs 與 CardPicker2/Models/DrawResult.cs
- [X] T015 [P] 建立卡牌庫載入結果與儲存選項模型於 CardPicker2/Services/CardLibraryLoadResult.cs 與 CardPicker2/Services/CardLibraryOptions.cs
- [X] T016 建立重複卡牌偵測服務，使用 Name.Trim()、MealType、Description.Trim() 與 OrdinalIgnoreCase 於 CardPicker2/Services/DuplicateCardDetector.cs
- [X] T017 建立早餐、午餐、晚餐各至少 3 張且不重複的種子資料於 CardPicker2/Services/SeedMealCards.cs
- [X] T018 建立卡牌庫服務介面，涵蓋載入、搜尋、詳情、抽卡、建立、編輯、刪除與 blocking 狀態查詢於 CardPicker2/Services/ICardLibraryService.cs
- [X] T019 實作 JSON 載入、缺檔重建、腐敗檔案保留、資料驗證與原子寫入於 CardPicker2/Services/CardLibraryService.cs
- [X] T020 建立與 SeedMealCards 一致的初始卡牌庫文件於 CardPicker2/data/cards.json
- [X] T021 註冊 CardLibraryOptions、ICardLibraryService、Serilog console/file logging 與正式環境 CSP middleware 於 CardPicker2/Program.cs
- [X] T022 設定 Serilog rolling file 與非秘密日誌層級設定於 CardPicker2/appsettings.json 與 CardPicker2/appsettings.Development.json
- [X] T023 執行 foundational 測試並確認通過於 tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj

**Checkpoint**: 基礎完成後，使用者故事可依優先級或團隊容量平行展開。

---

## Phase 3: User Story 1 - 以賭場老虎機流程抽出餐點 (Priority: P1) MVP

**Goal**: 使用者可選擇早餐、午餐或晚餐，完成投幣與拉桿/開始流程，看到老虎機式狀態並取得符合所選餐別的等機率餐點結果。

**Independent Test**: 使用預載卡牌進入首頁，驗證未選餐別與未投幣會被阻止；選定餐別並投幣後送出抽卡，頁面顯示轉動/揭示狀態與符合餐別的餐點名稱、餐別、完整描述。

### Tests for User Story 1，必須先失敗

- [X] T024 [P] [US1] 新增 MealCardRandomizer 等機率索引範圍與空集合拒絕測試於 tests/CardPicker2.UnitTests/Services/MealCardRandomizerTests.cs，並新增 DrawOperationState 狀態轉換與 DrawResult 顯示映射測試於 tests/CardPicker2.UnitTests/Models/DrawOperationStateTests.cs
- [X] T025 [US1] 新增抽卡服務只從所選餐別抽出、空卡池拒絕與 blocking recovery 拒絕測試於 tests/CardPicker2.UnitTests/Services/CardLibraryServiceTests.cs
- [X] T026 [P] [US1] 新增首頁抽卡整合測試，覆蓋未選餐別、未投幣、成功揭示、Anti-Forgery 與 blocking recovery 狀態於 tests/CardPicker2.IntegrationTests/Pages/DrawPageTests.cs
- [X] T027 [US1] 執行 US1 新增測試並確認實作前失敗於 CardPicker2.sln

### Implementation for User Story 1

- [X] T028 [P] [US1] 建立可替換抽卡亂數介面於 CardPicker2/Services/IMealCardRandomizer.cs
- [X] T029 [P] [US1] 實作 MealCardRandomizer 使用 BCL random API 產生 [0,count) 索引於 CardPicker2/Services/MealCardRandomizer.cs
- [X] T030 [US1] 擴充 ICardLibraryService 抽卡合約以回傳 DrawResult 與繁中失敗訊息於 CardPicker2/Services/ICardLibraryService.cs
- [X] T031 [US1] 實作抽卡篩選、空卡池拒絕、blocking recovery 拒絕與抽卡成功 Information log 於 CardPicker2/Services/CardLibraryService.cs
- [X] T032 [US1] 更新首頁 PageModel 的 OnGet 與 OnPostDrawAsync，處理 mealType query、CoinInserted、ModelState 與 DrawOperationState 於 CardPicker2/Pages/Index.cshtml.cs
- [X] T033 [US1] 更新首頁 Razor 表單，加入餐別選擇、投幣確認、拉桿/開始、Anti-Forgery token、狀態文字與揭示結果於 CardPicker2/Pages/Index.cshtml
- [X] T034 [US1] 加入老虎機視覺、響應式版面、焦點狀態與 reduced-motion CSS 於 CardPicker2/wwwroot/css/site.css
- [X] T035 [US1] 加入投幣/拉桿前端狀態、送出後禁用重複提交與 reduced-motion 揭示處理於 CardPicker2/wwwroot/js/site.js
- [X] T036 [US1] 註冊 IMealCardRandomizer 服務與首頁相關結構化日誌分類於 CardPicker2/Program.cs
- [X] T037 [US1] 執行 US1 單元與整合測試並確認通過於 CardPicker2.sln

**Checkpoint**: User Story 1 MUST 可獨立展示、測試與交付。

---

## Phase 4: User Story 2 - 瀏覽與搜尋餐點靈感 (Priority: P2)

**Goal**: 使用者可進入卡牌庫列表，依名稱關鍵字、餐別或兩者組合搜尋，並查看單一卡牌完整描述。

**Independent Test**: 使用多張預載卡牌進入 `/Cards`，驗證無條件列出全部卡牌、關鍵字大小寫不敏感部分比對、餐別篩選、組合條件、查無結果訊息與 `/Cards/{id}` 詳情一致性。

### Tests for User Story 2，必須先失敗

- [X] T038 [P] [US2] 新增搜尋服務測試，覆蓋無條件、關鍵字、餐別、組合條件與查無結果於 tests/CardPicker2.UnitTests/Services/CardLibrarySearchTests.cs
- [X] T039 [P] [US2] 新增卡牌列表與詳情整合測試，覆蓋 /Cards 查詢字串、查無結果、/Cards/{id} 與 404 於 tests/CardPicker2.IntegrationTests/Pages/SearchPageTests.cs
- [X] T040 [US2] 執行 US2 新增測試並確認實作前失敗於 CardPicker2.sln

### Implementation for User Story 2

- [X] T041 [US2] 擴充 ICardLibraryService 搜尋與詳情查詢合約，支援 SearchCriteria 與 Guid 查找於 CardPicker2/Services/ICardLibraryService.cs
- [X] T042 [US2] 實作名稱部分比對、餐別篩選、組合條件與已刪除資料排除於 CardPicker2/Services/CardLibraryService.cs
- [X] T043 [P] [US2] 建立卡牌列表 PageModel，處理 keyword、mealType query、blocking recovery 與查無結果狀態於 CardPicker2/Pages/Cards/Index.cshtml.cs
- [X] T044 [P] [US2] 建立卡牌詳情 PageModel，處理 Guid route、blocking recovery 與找不到卡牌 404 於 CardPicker2/Pages/Cards/Details.cshtml.cs
- [X] T045 [US2] 建立卡牌列表 Razor Page，顯示搜尋表單、餐點名稱、餐別、詳情入口與查無結果訊息於 CardPicker2/Pages/Cards/Index.cshtml
- [X] T046 [US2] 建立卡牌詳情 Razor Page，依 UI 契約顯示餐點名稱、餐別、完整描述與找不到訊息於 CardPicker2/Pages/Cards/Details.cshtml
- [X] T047 [US2] 補齊列表與詳情的 Bootstrap 5 響應式樣式、空狀態與可及性樣式於 CardPicker2/wwwroot/css/site.css
- [X] T048 [US2] 執行 US2 測試並確認 US1 不回歸於 CardPicker2.sln

**Checkpoint**: User Story 1 與 User Story 2 MUST 仍可各自獨立驗證。

---

## Phase 5: User Story 3 - 維護本機餐點卡牌庫 (Priority: P3)

**Goal**: 使用者可新增、編輯與刪除餐點卡牌，所有有效變更會持久化並立即反映在瀏覽、搜尋與抽卡流程中。

**Independent Test**: 從 `/Cards/Create` 新增有效卡牌後可在列表、搜尋與抽卡中使用；編輯卡牌後詳情與搜尋呈現最新資料；刪除確認後該卡牌不再出現；重複新增或編輯會被拒絕且原資料保持不變。

### Tests for User Story 3，必須先失敗

- [X] T049 [P] [US3] 新增卡牌建立、編輯、刪除、重複拒絕與編輯失敗保留原內容的服務測試於 tests/CardPicker2.UnitTests/Services/CardLibraryMutationTests.cs
- [X] T050 [P] [US3] 新增卡牌管理整合測試，覆蓋 Create/Edit/Delete 表單、POST /Cards/{id}?handler=Delete、Anti-Forgery、欄位錯誤、重複錯誤與 blocking recovery 停用操作於 tests/CardPicker2.IntegrationTests/Pages/CardManagementPageTests.cs
- [X] T051 [US3] 執行 US3 新增測試並確認實作前失敗於 CardPicker2.sln

### Implementation for User Story 3

- [X] T052 [US3] 擴充 ICardLibraryService 建立、編輯、刪除合約，定義成功、驗證失敗、重複、找不到與寫入失敗結果於 CardPicker2/Services/ICardLibraryService.cs
- [X] T053 [US3] 實作建立卡牌的新 Guid 產生、trim 驗證、非法餐別拒絕、重複拒絕與寫入失敗回復於 CardPicker2/Services/CardLibraryService.cs
- [X] T054 [US3] 實作編輯卡牌不可變 ID、編輯成重複時完整失敗、刪除永久移除與結構化警告/錯誤日誌於 CardPicker2/Services/CardLibraryService.cs
- [X] T055 [P] [US3] 建立共用卡牌表單 partial，包含欄位錯誤與繁中標籤於 CardPicker2/Pages/Cards/_CardForm.cshtml
- [X] T056 [P] [US3] 建立新增卡牌 PageModel，處理 GET、POST、ModelState、服務結果與 TempData 成功訊息於 CardPicker2/Pages/Cards/Create.cshtml.cs
- [X] T057 [P] [US3] 建立編輯卡牌 PageModel，處理 GET、POST、找不到、不可變 ID 與服務失敗訊息於 CardPicker2/Pages/Cards/Edit.cshtml.cs
- [X] T058 [US3] 建立新增卡牌 Razor Page，包含 Anti-Forgery、validation summary 與 blocking recovery 停用狀態於 CardPicker2/Pages/Cards/Create.cshtml
- [X] T059 [US3] 建立編輯卡牌 Razor Page，包含 Anti-Forgery、validation summary 與 blocking recovery 停用狀態於 CardPicker2/Pages/Cards/Edit.cshtml
- [X] T060 [US3] 在卡牌列表加入編輯/刪除管理入口，並在詳情頁加入刪除確認表單與 OnPostDeleteAsync handler，要求 ConfirmDelete=true 並保留 Anti-Forgery 於 CardPicker2/Pages/Cards/Index.cshtml、CardPicker2/Pages/Cards/Details.cshtml 與 CardPicker2/Pages/Cards/Details.cshtml.cs
- [X] T061 [US3] 補齊卡牌管理表單、確認區、停用狀態與行動版排版樣式於 CardPicker2/wwwroot/css/site.css
- [X] T062 [US3] 執行 US3 測試並確認 US1、US2 不回歸於 CardPicker2.sln

**Checkpoint**: 所有已選使用者故事 MUST 獨立可用且整體不回歸。

---

## Phase 6: Polish 與跨切面工作

**目的**: 完成跨故事品質要求、文件、效能、安全、可及性與交付檢查。

- [ ] T063 [P] 補齊生產安全標頭整合測試，驗證 HSTS 與 Content-Security-Policy 於 tests/CardPicker2.IntegrationTests/SecurityHeadersTests.cs
- [ ] T064 [P] 檢查並補齊所有公開模型與服務 XML 文件註解，必要時加入 `<example>` 或 `<code>`，於 CardPicker2/Models/MealType.cs、CardPicker2/Models/MealCard.cs、CardPicker2/Models/MealCardInputModel.cs、CardPicker2/Models/CardLibraryDocument.cs、CardPicker2/Models/SearchCriteria.cs、CardPicker2/Models/DrawOperationState.cs、CardPicker2/Models/DrawResult.cs、CardPicker2/Services/CardLibraryLoadResult.cs、CardPicker2/Services/CardLibraryOptions.cs、CardPicker2/Services/DuplicateCardDetector.cs、CardPicker2/Services/ICardLibraryService.cs、CardPicker2/Services/CardLibraryService.cs、CardPicker2/Services/IMealCardRandomizer.cs、CardPicker2/Services/MealCardRandomizer.cs、CardPicker2/Services/SeedMealCards.cs
- [ ] T065 更新 shared layout 導覽、繁中文案與卡牌頁入口於 CardPicker2/Pages/Shared/_Layout.cshtml
- [ ] T066 驗證並調整桌面與行動尺寸下首頁、列表、詳情與表單無重疊或溢出，並確認鍵盤操作、焦點狀態與 WCAG 2.1 AA 目標於 CardPicker2/wwwroot/css/site.css
- [ ] T067 驗證 reduced-motion、重複提交防護與表單前端行為於 CardPicker2/wwwroot/js/site.js
- [ ] T068 執行格式、建置、完整測試與覆蓋率品質閘門，確認關鍵業務邏輯覆蓋率達 80% 以上或在本任務記錄合理例外，於 CardPicker2.sln、tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj 與 tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj
- [ ] T069 依 quickstart 手動驗證首次啟動、抽卡、搜尋、CRUD、腐敗 JSON、reduced motion、老虎機視覺驗收與效能預算量測，並保留 FCP、LCP、搜尋回應與 Page handler p95 證據於 specs/001-casino-meal-picker/quickstart.md
- [ ] T070 更新交付紀錄與驗證證據於 specs/001-casino-meal-picker/tasks.md

---

## 相依性與執行順序

### Phase Dependencies

- **Setup (Phase 1)**: 無相依性，可立即開始
- **Foundational (Phase 2)**: 依賴 Setup，會阻塞所有使用者故事
- **US1 (Phase 3)**: 依賴 Foundational，是 MVP 優先交付範圍
- **US2 (Phase 4)**: 依賴 Foundational，可在 US1 完成後交付，也可由不同人員在基礎完成後平行開發並於整合時確認 US1 不回歸
- **US3 (Phase 5)**: 依賴 Foundational，可在 US1/US2 後交付，也可由不同人員在基礎完成後平行開發並於整合時確認既有故事不回歸
- **Polish (Phase 6)**: 依賴所有選定使用者故事完成

### User Story Dependencies

- **User Story 1 (P1)**: Foundational 完成後可開始，不依賴其他故事
- **User Story 2 (P2)**: Foundational 完成後可開始，功能價值不依賴 US1，但整合時 MUST 確認 US1 不回歸
- **User Story 3 (P3)**: Foundational 完成後可開始，功能價值不依賴 US1/US2，但整合時 MUST 確認新增、編輯、刪除結果能被 US1/US2 使用

### Within Each User Story

- 測試 MUST 先寫並先失敗
- 模型先於服務
- 服務先於 PageModel
- PageModel 先於 Razor Page 與靜態資源整合
- 每個故事完成後 MUST 獨立驗證

### Parallel Opportunities

- T005、T006、T007 可平行建立不同 foundational 測試檔案
- T009、T010、T011、T012、T013、T014、T015 可平行建立不同模型與結果檔案
- T024、T026 可平行建立 US1 的單元與整合測試檔案
- T028、T029 可平行建立 US1 randomizer 介面與實作檔案
- T038、T039 可平行建立 US2 的單元與整合測試檔案
- T043、T044 可平行建立 US2 列表與詳情 PageModel
- T049、T050 可平行建立 US3 的單元與整合測試檔案
- T055、T056、T057 可平行建立 US3 表單 partial、新增 PageModel 與編輯 PageModel
- T063、T064 可平行處理安全標頭測試與 XML 文件註解

---

## 平行執行範例

### User Story 1

```bash
# Terminal A
dotnet test tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj --filter MealCardRandomizer

# Terminal B
dotnet test tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj --filter DrawPage
```

### User Story 2

```bash
# Terminal A
dotnet test tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj --filter CardLibrarySearch

# Terminal B
dotnet test tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj --filter SearchPage
```

### User Story 3

```bash
# Terminal A
dotnet test tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj --filter CardLibraryMutation

# Terminal B
dotnet test tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj --filter CardManagementPage
```

---

## 實作策略

### MVP First，只完成 User Story 1

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational
3. 先撰寫並確認 US1 測試失敗
4. 完成 US1 抽卡服務、首頁 PageModel、Razor Page、CSS 與 JS
5. 執行 US1 單元與整合測試，確認首頁抽卡可獨立交付

### Incremental Delivery

1. 完成 Setup 與 Foundational
2. 完成 US1，測試並展示 MVP 抽卡流程
3. 完成 US2，確認 US1 不回歸並展示瀏覽搜尋
4. 完成 US3，確認新增、編輯、刪除會反映在 US1/US2
5. 完成 Polish，執行完整建置、測試、quickstart 與安全/可及性檢查

### Parallel Team Strategy

1. 團隊共同完成 Setup 與 Foundational
2. Foundational 完成後可分工處理 US1、US2、US3，但每個故事都必須先有失敗測試
3. 故事整合前各自通過單元與整合測試
4. 最後以 CardPicker2.sln 執行完整回歸測試與品質閘門

---

## Notes

- 所有使用者可見 UI copy、驗證訊息、復原訊息與文件必須使用繁體中文 zh-TW
- 不新增外部 JSON API；公開介面維持 Razor Pages、query string、form field、handler、status code 與 HTML
- 卡牌庫持久化只能使用 CardPicker2/data/cards.json，腐敗檔案不得被種子資料覆蓋
- 狀態變更表單必須包含 Anti-Forgery token，正式環境必須保留 HTTPS/HSTS 並新增 CSP
- PageModel 僅負責協調；核心資料規則、重複偵測、持久化與抽卡公平性必須在 Services 層
