# 研究: 抽卡模式與機率統計

## Decision: 維持 Razor Pages 表單介面，不新增外部 JSON API

**決策**: 正常模式、隨機模式、抽卡提交、統計表、刪除後歷史列與復原狀態都透過既有 Razor Pages HTML、form POST、query string、hidden field、TempData 或 PageModel 狀態呈現。統計表是首頁的一部分，不新增面向外部系統的 JSON API。

**理由**: 規格 FR-026 明確限制統計表不得新增外部資料介面；目前專案也是單機單人 Razor Pages app。Razor Pages 已內建表單 model binding、Anti-Forgery、Tag Helpers 與 page handler 流程，符合 Microsoft 對 page-centered form workflow 的建議，也能讓所有使用者可見訊息依既有 localization middleware render。

**Alternatives considered**:

- 新增 Minimal API 或 controller 回傳 JSON 統計：會擴大公開介面與測試矩陣，違反 FR-026。
- 前端只用 JavaScript 從 DOM 或 localStorage 計算統計：無法滿足跨重啟持久化與伺服器端資料完整性要求。
- 建立 SPA 層：對單機 Razor Pages app 過度複雜，且會增加 state synchronization 風險。

**參考來源**:

- Microsoft Learn, [Razor Pages in ASP.NET Core](https://learn.microsoft.com/aspnet/core/razor-pages/?view=aspnetcore-10.0)

## Decision: 將 `cards.json` 擴充為 schema v3，保留單一檔案持久化

**決策**: `CardLibraryDocument.CurrentSchemaVersion` 升級為 3。v3 root 包含 `schemaVersion`、`cards` 與 `drawHistory`。`cards` 延續 schema v2 的雙語 localizations，新增 `status` 與 `deletedAtUtc` 等卡牌生命週期欄位；`drawHistory` 只保存成功抽卡事實。讀取 schema v1/v2 時在記憶體中補齊 v3 預設值：卡牌為 active、歷史為空；下一次成功寫入時以 v3 原子保存。

**理由**: 使用者要求所有資料維持在單一本機 JSON 檔；成功抽卡歷史與統計必須跨應用程式重啟保留。把卡牌與歷史放在同一文件可讓「抽卡結果成立」與「歷史紀錄新增」在單次 atomic write 中一起完成，避免結果與統計分裂。保留讀取 v1/v2 能延續既有 bilingual feature 與使用者卡牌，不把舊版合法資料誤判為 corrupted。

**Alternatives considered**:

- 另建 `draw-history.json`: 會讓抽卡結果、卡牌狀態與歷史跨檔案一致性變成兩階段問題，失敗時較難保證原子性。
- 使用 SQLite 或其他資料庫：超出 constraints 與 product goal。
- 只持久化 aggregate counters: 無法重建 deleted card 歷史列、無法審計重複提交，也容易在卡牌改名或刪除時失真。

## Decision: 正常與隨機模式共用候選池建構器與 randomizer

**決策**: 新增 `DrawCandidatePoolBuilder` 或等效服務，根據 `DrawMode` 與 `MealType?` 產生候選卡池。正常模式要求有效餐別，只納入該餐別 active cards；隨機模式忽略餐別欄位，只納入全部 active cards。`MealCardRandomizer.NextIndex(pool.Count)` 繼續負責在 `[0, N)` 中選 index，服務以 pool index 取得卡牌。

**理由**: 公平性驗收定義為候選池含 N 張有效卡牌時，每張候選卡標稱機率為 1/N。把候選池與 index randomizer 分開可讓單元測試直接驗證 pool membership 與 index range，避免 UI 模式、餐別欄位、排序、語系或動畫進入機率邏輯。

**Alternatives considered**:

- 在 PageModel 中依模式篩選 cards：會把核心業務規則放進頁面協調層，違反 architecture boundary。
- 為正常/隨機模式建立兩個 randomizer：增加重複邏輯，公平性更難集中驗證。
- 用歷史抽中次數動態加權：違反 FR-003、FR-005、DI-010 與成功標準 SC-003。

## Decision: 用 `DrawOperationId` 達成成功抽卡 idempotency

**決策**: 首頁 GET 產生或帶回一個非秘密 `DrawOperationId`，POST `/?handler=Draw` 需提交同一值。服務在同一 JSON read-modify-write critical section 中先查找既有成功 `drawHistory.OperationId`；若已存在，直接 replay 原成功結果並標示 `IsReplay = true`，不得重新 randomize 或新增歷史。若不存在且驗證成功，才建立新成功歷史紀錄。

**理由**: 規格要求同一次成功操作重複提交時重顯原成功結果且不得新增歷史紀錄。只靠前端 disable button 不能防止重新整理、瀏覽器重送、快速連點或網路重試。把 operation id 寫入持久歷史，重啟後仍能識別同一成功操作。

**Alternatives considered**:

- 只用 JavaScript 防重複點擊：可改善 UX，但不能作為資料完整性保證。
- 只用 session state: 應用程式重啟或 cookie/session 遺失後無法 replay，且 session 不是本功能的持久來源。
- 以 timestamp 或 meal type 推斷重複：可能誤把兩次合法抽卡合併，也無法精準 replay card ID。

## Decision: 統計每次 request 由成功歷史與目前卡牌集合投影，不持久化 aggregate

**決策**: 新增 `DrawStatisticsService` 讀取 v3 document 後產生 `DrawStatisticsSummary`。總成功抽取次數 = `drawHistory.Count`；單一卡牌抽中次數 = 成功歷史中該 `CardId` 的筆數；歷史機率 = 該卡抽中次數 / 總成功抽取次數。列集合為 active cards 與曾成功抽中且目前 deleted 的 cards 聯集。無成功歷史時顯示空狀態，不輸出每卡 0% 作為歷史分布。

**理由**: Aggregate counters 是可由成功歷史推導的資料，持久化後需要處理同步、回滾與修復問題。每次由 history 重建能保證總數等於保留歷史筆數，符合 DI-007，也讓 deleted card 與改名後統計自然依不可變 card ID 延續。

**Alternatives considered**:

- 在 JSON 中保存 `totalDraws` 與每卡 counter: 讀取快但增加不一致狀態與 migration 成本。
- 只顯示目前 active cards 的統計: 會讓已刪除且曾抽中的卡消失，造成總數與機率失真。
- 依餐別分母計算機率: 違反 FR-013 與 DI-008；分母必須是全部成功抽卡。

## Decision: 刪除卡牌時依歷史決定 hard delete 或 retained deleted

**決策**: delete operation 對沒有成功歷史的卡牌可沿用 hard delete；對已有成功歷史的卡牌改為 retained deleted：保留同一 `MealCard.Id`、localizations、meal type 與 `status = Deleted`，並記錄 `deletedAtUtc`。所有 browse/search/details/edit/draw candidate pool 預設只使用 active cards；統計投影會納入曾成功抽中的 deleted cards 並標示已刪除。

**理由**: 規格要求已刪除卡牌不得再進未來抽卡池，但曾抽中的已刪除卡牌必須保留歷史列。保留卡牌本體可保存最近可顯示名稱、餐別與雙語內容，避免只靠舊 history snapshot 顯示過期文字。未曾抽中的已刪除卡牌不需要統計列，仍可 hard delete，符合 DI-012。

**Alternatives considered**:

- 所有 delete 都 soft delete: 資料會永久累積未曾抽中的 deleted cards，增加列表與 duplicate detection 複雜度。
- 永遠 hard delete，只在 history 存 snapshot: 卡牌改名後歷史顯示規則較難一致，且 deleted row 需要重複保存完整 localized data。
- 讓 deleted cards 仍出現在卡牌庫一般列表: 會混淆「目前有效卡牌」與歷史統計列，並可能被誤編輯或抽出。

## Decision: 以 per-process file coordinator 序列化 JSON read-modify-write

**決策**: 新增 singleton `CardLibraryFileCoordinator` 或同等元件，內含 `SemaphoreSlim`，讓 create/edit/delete/draw-history append/schema migration 等 read-modify-write 操作在同一 process 內序列化。實際寫入仍使用同目錄 temp file、flush、atomic replace。讀取 corrupted/unreadable/unsupported 時不得嘗試覆寫。

**理由**: 單機單人仍可能因快速連點或瀏覽器重送產生並行 POST。若兩個 scoped `CardLibraryService` 同時讀取同一文件再各自寫回，可能丟失 history 或 card mutation。單一 process lock 搭配 atomic replace 可滿足本機使用情境下的實用一致性需求。

**Alternatives considered**:

- 不加 lock，只依賴 atomic replace: 可避免部分寫入，但無法避免 lost update。
- OS-level cross-process file lock: 對目前單機單 process app 過度複雜；若未來支援多 process 再升級。
- 引入資料庫 transaction: 違反不使用資料庫限制。

## Decision: 視覺、語系與 reduced motion 只影響呈現，不進入抽卡或統計核心

**決策**: `site.js` 可用於 disabled state、快速連點 UI guard、動畫 class 與 `prefers-reduced-motion` 呈現；抽卡結果、歷史紀錄與統計只由 server-side validated POST 與服務層決定。語系切換只改變 `LocalizedMealCardView` 與 resource text，不改變 `DrawMode`、candidate pool、history 或 denominator。

**理由**: 規格明確要求動畫時間、顯示排序、語系切換與 reduced motion 不得影響抽卡結果或統計。把呈現與核心決策隔離，可讓自動化測試在沒有動畫的 TestServer 中驗證業務行為。

**Alternatives considered**:

- 前端動畫結束時才決定結果: 會讓 timing 進入業務邏輯，也不利於 server validation。
- 以語系排序後的 index 作為抽卡來源: 語系切換可能改變順序與結果，違反 FR-020。
- 用賭博式文案或稀有度強化娛樂感: 違反 FR-022、FR-023 與產品邊界。

**參考來源**:

- Microsoft Learn, [Static files in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/map-static-files?view=aspnetcore-10.0)
- Microsoft Learn, [Globalization and localization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)
