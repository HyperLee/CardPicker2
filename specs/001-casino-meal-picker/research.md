# Phase 0 研究: 餐點抽卡網站2

## 決策 1: 沿用 ASP.NET Core Razor Pages 作為主要應用模型

**Decision**: 使用既有 `CardPicker2` Razor Pages 專案承載首頁抽卡、卡牌列表、詳情與 CRUD 表單，不改寫為 MVC、Blazor 或獨立 API。

**Rationale**: 功能主要是頁面、表單、搜尋與單機互動，與 Razor Pages 的 page-focused routing、PageModel、model binding、Tag Helpers 與 Anti-Forgery 預設能力相符。既有專案已啟用 `builder.Services.AddRazorPages()`、`app.MapRazorPages().WithStaticAssets()` 與 Bootstrap/jQuery 前端資源，沿用可降低實作與測試風險。

**Alternatives considered**:

- MVC: 適合 action/filter 更複雜的大型應用，但本功能不需要 controller/action 分離帶來的額外 ceremony。
- Blazor Web App: 可提供更強互動性，但本功能的老虎機動畫可用 CSS/JavaScript progressive enhancement 完成，不值得增加 render mode 與互動連線複雜度。
- Minimal APIs + SPA: 適合 API-first 或前後端分離，與目前單機 Razor Pages 需求不符。

## 決策 2: 使用單一本機 JSON 檔案保存卡牌庫

**Decision**: 使用 `System.Text.Json` 讀寫 `{ContentRootPath}/data/cards.json`，repo 內對應 `CardPicker2/data/cards.json`。JSON shape 使用 `CardLibraryDocument` 包含 `schemaVersion` 與 `cards`，卡牌 ID 使用 `Guid` 字串。

**Rationale**: 規格明確要求單機單人本機使用與單一本機 `.json` 文字檔。資料規模以數十到數百張卡牌為主，完整載入記憶體再查詢與抽卡可滿足效能預算。`schemaVersion` 可讓未來欄位演進有明確檢查點，同時不需要資料庫服務。

**Alternatives considered**:

- SQLite/EF Core: 可提供交易與查詢能力，但規格與大綱要求不使用專案資料庫軟體，目前資料量不需要。
- 純陣列 JSON: 較簡單，但缺少 schema/version 與文件層級 metadata，不利於未來安全遷移。
- `appsettings.json`: 屬設定檔而非使用者資料，不適合 CRUD 後持久化。

## 決策 3: 缺檔重建預設資料，腐敗檔案保留並阻斷操作

**Decision**: 啟動或首次讀取時，若 `cards.json` 不存在，建立早餐、午餐、晚餐各至少 3 張預設卡牌；若檔案存在但不可讀或 JSON 解析失敗，保留原檔、不覆寫，並讓服務回傳 blocking recovery 狀態，UI 顯示阻止卡牌庫操作的復原錯誤。

**Rationale**: 這直接落實 FR-003、FR-022、FR-023 與 DI-008。缺檔可視為首次啟動；腐敗檔案可能包含使用者資料，覆寫會破壞復原可能性。

**Alternatives considered**:

- 腐敗時直接重建預設資料: 可讓網站繼續操作，但違反保留原檔與資料完整性要求。
- 自動備份再重建: 對本階段增加額外檔案管理規則，且規格未要求。
- 每次啟動都覆寫預設資料: 會遺失使用者新增、編輯、刪除結果。

## 決策 4: 寫入採原子替換並集中於服務層

**Decision**: `CardLibraryService` 負責所有新增、編輯、刪除與保存；寫入時先序列化到同目錄暫存檔，flush 完成後以檔案替換目標檔，任一步驟失敗都保留原有效資料。

**Rationale**: 規格要求狀態變更完整成功或完整失敗。集中服務層可避免不同 PageModel 以不同方式寫檔，也讓單元測試能直接驗證部分失敗不污染卡牌庫。

**Alternatives considered**:

- PageModel 直接讀寫檔案: 實作較快，但會讓驗證、重複判斷與錯誤處理分散。
- 記憶體 singleton 快取為唯一來源: 可減少 I/O，但應用重啟後仍需持久化，且 singleton 狀態會讓測試隔離更困難。
- 每次操作 append log: 有復原優勢，但超出目前簡單 CRUD 規模。

## 決策 5: 重複卡牌以正規化名稱、餐別、描述判斷

**Decision**: 新增與編輯時，將餐點名稱與描述 `Trim()` 後以 `StringComparer.OrdinalIgnoreCase` 比對，再加上餐別形成重複判斷 key；編輯時排除同一張不可變 ID。

**Rationale**: 這完整對應規格釐清與 FR-006、FR-019、DI-007。使用 ordinal case-insensitive 可避免文化特定大小寫規則影響資料一致性；保留原始使用者輸入內容，但驗證與重複判斷使用正規化值。

**Alternatives considered**:

- 只比對餐點名稱: 會錯誤拒絕同名但描述不同的有效卡牌。
- 大小寫敏感比對: 會讓 `Ramen` 與 `ramen` 這類實質重複進入資料庫。
- 儲存時強制改寫大小寫: 會改變使用者輸入，不符合最小驚訝原則。

## 決策 6: 抽卡等機率由可替換 randomizer 提供

**Decision**: 定義 `IMealCardRandomizer`，正式實作使用 BCL random API 在 `[0, count)` 產生索引，服務層只傳入所選餐別的有效卡牌集合；測試以 fake randomizer 固定索引驗證行為。

**Rationale**: 抽卡公平性是核心資料完整性要求。將隨機索引與卡牌篩選分離，可以單元測試「只從所選餐別抽」、「空卡池拒絕」、「已刪除卡牌不出現」與「動畫不影響結果」。

**Alternatives considered**:

- 在 PageModel 直接使用 `Random.Shared`: 實作簡單，但難以穩定測試。
- 以排序或時間戳模擬隨機: 會破壞等機率要求。
- 導入外部 random 套件: 對此規模沒有必要。

## 決策 7: 老虎機動畫僅為呈現層狀態，不參與結果計算

**Decision**: 抽卡結果由伺服器端服務在表單 POST handler 中決定；前端只負責投幣、拉桿、轉動中、揭示與 disabled 狀態。`prefers-reduced-motion: reduce` 時略過連續轉動，改用短暫靜態揭示狀態。

**Rationale**: 規格要求賭場老虎機體驗，但不得改變等機率抽取規則。讓動畫與結果計算解耦可避免使用者重複點擊、轉動順序或 CSS 動畫影響抽卡結果，也更容易整合 Anti-Forgery 與伺服器端驗證。

**Alternatives considered**:

- 前端 JavaScript 直接抽卡: 使用者可修改瀏覽器狀態，且需把完整卡池暴露到前端。
- 以 WebSocket/SignalR 控制動畫: 過度設計，不符合單機單人需求。
- 無動畫直接顯示結果: 實作簡單，但無法滿足本版差異化體驗。

## 決策 8: 測試採服務單元測試加 Razor Pages 整合測試

**Decision**: 建立 `tests/CardPicker2.UnitTests` 驗證模型、重複判斷、卡牌庫服務、JSON 錯誤與 randomizer；建立 `tests/CardPicker2.IntegrationTests` 使用 `WebApplicationFactory<Program>` 驗證頁面渲染、表單驗證、Anti-Forgery、搜尋、抽卡與安全標頭。

**Rationale**: 憲章要求 TDD，且本功能同時包含純業務規則與 Razor Pages 表單流程。分層測試可讓業務規則快速迭代，整合測試則覆蓋 DI、middleware、Tag Helpers、ModelState 與使用者可見行為。

**Alternatives considered**:

- 只做端到端手動驗證: 無法滿足測試優先與資料完整性要求。
- 只做 PageModel 單元測試: 容易漏掉 routing、Anti-Forgery 與 HTML 輸出問題。
- 立即加入 Playwright: 對第一版可作為後續增強；目前 WebApplicationFactory 足以覆蓋核心流程，快速入門保留手動瀏覽器驗證。

## 決策 9: 正式環境新增 CSP，日誌使用 Serilog console + rolling file

**Decision**: 保留現有 HTTPS redirection 與非開發環境 HSTS；新增 CSP response header。日誌以 Serilog 寫入 console 與 `logs/cardpicker-.log` rolling file，事件使用結構化欄位，不記錄秘密值或完整敏感內容。

**Rationale**: 憲章要求正式環境 HTTPS/HSTS/CSP 與可觀察性。Serilog 能在 console 與本機檔案間提供一致結構化日誌，方便單機部署診斷資料檔狀態與操作錯誤。

**Alternatives considered**:

- 僅使用預設 `ILogger` console: 能運作，但缺少 rolling file 供本機長期診斷。
- Application Insights: 適合雲端服務，與本階段單機本機使用情境不合。
- 寬鬆 CSP 或不設 CSP: 無法滿足憲章安全要求。
