# Phase 0 研究: 網站主題模式切換

## 決策 1: 使用 Bootstrap 5.3 `data-bs-theme` 搭配全站 CSS token

**Decision**: 在 `<html>` 上設定 `data-bs-theme="light"` 或 `data-bs-theme="dark"` 作為 Bootstrap color mode 來源，並在 `wwwroot/css/site.css` 補上 CardPicker2 自訂色彩 token、焦點樣式、卡牌、表單、警示與老虎機元件的 light/dark 對應樣式。

**Rationale**: 專案已使用 Bootstrap 5.3.3，`data-bs-theme` 是 Bootstrap 5.3 的內建 color mode 入口。沿用它可讓 `.navbar`、`.btn`、`.form-control`、`.alert` 等元件盡量使用框架既有變數，再用 `site.css` 覆蓋專案特有的餐點抽卡機色彩，降低維護成本並保持 UI 一致。

**Alternatives considered**:

- 完全自訂 `.dark-mode` class: 可控性高，但會繞過 Bootstrap 5.3 color mode，需重寫更多元件狀態。
- 只依賴 `prefers-color-scheme` CSS media query: 無法支援使用者明確選擇亮色或暗黑覆蓋系統。
- 導入前端主題套件: 對 Razor Pages 單站過重，也增加 CSP 與資源管理風險。

## 決策 2: 僅保存選取模式字串，不保存有效主題

**Decision**: 使用 `localStorage` key `cardpicker.theme.mode` 保存使用者選取模式，值只允許 `light`、`dark` 或 `system`。有效主題不保存；每次載入與系統外觀變更時由選取模式加上 `matchMedia('(prefers-color-scheme: dark)')` 即時計算。

**Rationale**: 規格 FR-008 與 DI-002 明確要求保存範圍僅限選取模式。保存有效主題會讓 `system` 模式在系統偏好變更後可能顯示過期結果。以固定字串白名單驗證也能安全忽略無效或被手動竄改的 localStorage 值。

**Alternatives considered**:

- 保存 `{ mode, effectiveTheme, updatedAt }`: 可提供更多診斷，但違反「僅保存選取模式」。
- 使用 cookie: 會把純前端偏好送到伺服器，增加隱私與測試 surface，但本功能不需要 server-side rendering 依賴該值。
- 寫入 `cards.json`: 會污染餐點卡牌持久化邊界，且無法代表同一瀏覽器與裝置偏好。

## 決策 3: 在 `<head>` 內於樣式載入前套用有效主題

**Decision**: 在 `Pages/Shared/_Layout.cshtml` 的 `<head>` 中，於 Bootstrap 與 `site.css` 載入前放置一段最小化、無外部相依的主題 bootstrap script。它只讀取 localStorage、驗證模式值、讀取 system color scheme，並立即設定 `document.documentElement.dataset.bsTheme` 與 `document.documentElement.dataset.themeMode`。

**Rationale**: 規格要求目標瀏覽器環境中首次可見呈現前套用有效主題，避免亮暗主題閃爍。外部 `site.js` 通常在 body 結尾載入，無法保證首屏前執行。head script 保持非常小且只做同步 attribute 設定，可滿足 FOUC 要求並把後續互動邏輯留在 `site.js`。

**Alternatives considered**:

- 只在 body 結尾的 `site.js` 套用: 實作集中，但容易先顯示預設 light 後再切換 dark。
- 伺服器根據 cookie 產生初始主題: 可避免 inline script，但需引入 cookie、server-side preference parsing 與跨頁寫入，不符合僅本機瀏覽器偏好的簡潔範圍。
- 純 CSS `color-scheme`: 可改善表單控制外觀，但不能保存與套用使用者明確選擇。

## 決策 4: production CSP 明確允許主題 bootstrap script

**Decision**: production CSP 必須繼續存在，並以 hash、nonce 或同等明確允許策略涵蓋 head bootstrap script。若保留現有 `'unsafe-inline'`，必須在實作任務中明確記錄原因與後續收斂計畫；優先採用固定 inline script hash 或 nonce middleware。

**Rationale**: 首屏前套用有效主題需要 head script，但憲章要求 production CSP。將 script 納入明確允許清單能同時滿足無閃爍與安全治理，不把主題功能變成降低 CSP 強度的理由。

**Alternatives considered**:

- 移除 CSP: 違反憲章安全原則。
- 一律使用 `'unsafe-inline'`: 與現有程式相容，但安全性較弱，應避免成為新增功能的永久策略。
- 把主題偏好交給外部套件: 仍需 CSP 允許外部資源，並增加供應鏈風險。

## 決策 5: 首頁使用 fieldset/radio segmented control，非首頁不顯示控制項

**Decision**: 在 `Pages/Index.cshtml` 新增一組可鍵盤操作的 fieldset/radio 控制，三個值分別是 `light`、`dark`、`system`，顯示文字為「亮色模式」、「暗黑模式」、「跟隨系統」。首頁以外頁面只套用 shared layout 產生的有效主題，不輸出主題模式控制項。

**Rationale**: Radio group 天然符合三選一語意，可透過鍵盤方向鍵與 Tab 操作，也能清楚標示目前選取值。控制項只在首頁出現，完整符合 FR-001、FR-002、FR-003 與 FR-006。

**Alternatives considered**:

- `<select>`: 可用但目前選取狀態不如 segmented radio 直觀。
- Navbar 全站切換: 使用方便，但違反「其餘分頁不提供該選項」。
- 三個普通 button: 需額外 ARIA state 與鍵盤行為，且較容易形成非標準 radio interaction。

## 決策 6: 使用 `matchMedia` 與 `storage` event 同步 system 與跨分頁變更

**Decision**: `site.js` 在載入後監聽 `matchMedia('(prefers-color-scheme: dark)')` 的 change event；目前選取模式為 `system` 時，系統外觀變更會重新推導有效主題。跨分頁同步使用同 origin 的 `storage` event，當 `cardpicker.theme.mode` 變更時驗證新值並套用最新模式。

**Rationale**: 這兩個瀏覽器 API 直接對應 FR-011 與 FR-013，不需要 SignalR、輪詢或伺服器狀態。`storage` event 在其他已開啟分頁觸發，同一分頁則在使用者選擇時立即套用，能滿足 2 秒內同步要求。

**Alternatives considered**:

- BroadcastChannel: API 簡潔，但 `storage` event 支援面更廣，且已經需要 localStorage 持久化。
- 定時輪詢 localStorage: 可行但浪費資源，且同步延遲取決於輪詢間隔。
- 伺服器 session 或 SignalR: 過度設計，不符合單機單人、本機瀏覽器偏好範圍。

## 決策 7: localStorage 或 matchMedia 不可用時採安全預設且不中斷頁面

**Decision**: 任何 localStorage 讀寫例外、matchMedia 不存在、保存值無效或瀏覽器政策阻擋時，選取模式視為 `system`；若 system 偏好也無法判斷，有效主題使用 `light` 作為安全預設。使用者剛選擇的模式在當前頁面仍以記憶體狀態立即套用，但後續回訪可能回到安全預設。

**Rationale**: 規格要求必要能力不可用時不得中斷餐點抽卡、瀏覽、搜尋或卡牌管理。Light 作為安全預設可最大化相容性；所有能力讀取都視為可能失敗並以 try/catch 包住，避免前端例外中斷其他互動。

**Alternatives considered**:

- localStorage 失敗時顯示 blocking error: 主題偏好不是核心資料，阻斷餐點流程不符合 FR-015。
- 保存失敗時回報到伺服器: 增加 API surface 與隱私風險，本階段不需要。
- matchMedia 不可用時預設 dark: 可能造成高亮環境或舊瀏覽器可讀性較差。

## 決策 8: 測試採 Razor contract 測試加 browser behavior 測試

**Decision**: 先在 `tests/CardPicker2.IntegrationTests` 建立 Razor contract 測試，驗證首頁輸出三個主題模式控制項、非首頁不輸出控制項、layout 具備初始套用 script 與 production CSP。再以 browser automation 驗證首次套用、localStorage 持久化、system 模式、跨分頁 2 秒同步、切換不清表單/結果與 responsive/contrast。

**Rationale**: WebApplicationFactory 能快速驗證伺服器輸出的 HTML contract，但無法執行 localStorage、matchMedia、storage event 與實際 CSS 計算。主題功能的關鍵風險在瀏覽器行為，因此需要 browser-level 驗證作為整合測試的一部分。

**Alternatives considered**:

- 只做手動測試: 無法滿足憲章測試優先，也容易漏掉跨分頁與首次套用回歸。
- 只測試 HTML 字串: 無法證明主題真的生效或 2 秒內同步。
- 導入大型前端測試框架: 對目前 Razor Pages + jQuery 架構過重；Playwright for .NET 可留在既有 .NET 測試工作流。

## 決策 9: 主題切換不變更餐點資料與抽卡流程

**Decision**: 主題程式只能修改 DOM attribute、CSS class/token、控制項 checked state 與 localStorage 的 theme key，不得呼叫卡牌服務、提交抽卡表單、改寫搜尋 query string、清除 validation summary 或變更 `cards.json`。

**Rationale**: 規格 FR-017、DI-004 與 SC-008 要求主題切換只影響視覺呈現。把主題狀態限制在瀏覽器呈現層，可以明確避免污染餐點卡牌資料完整性與抽卡公平性。

**Alternatives considered**:

- 將主題模式放入每個 Razor form hidden input: 會增加 state-changing form surface，且可能干擾既有表單驗證。
- 切換主題時重整頁面: 較簡單但可能清除未送出表單或可見抽卡結果。
- 從伺服器重新載入主題 CSS: 會增加請求與 flicker 風險。
