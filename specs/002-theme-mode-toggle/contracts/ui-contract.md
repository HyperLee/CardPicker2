# UI 契約: 網站主題模式切換

## 範圍

本功能不暴露外部 JSON API。公開介面是 Razor Pages 輸出的 HTML、首頁主題控制項、DOM attribute、localStorage key、前端互動狀態、production 安全標頭與使用者可見文案。所有既有狀態變更表單仍必須包含 ASP.NET Core Anti-Forgery token；主題切換本身是 client-side 偏好更新，不提交伺服器表單。

## 全域規則

- 所有使用者可見主題文案使用繁體中文。
- `document.documentElement` 必須在 CSS 載入前取得有效 `data-bs-theme`。
- 所有站內主要頁面都必須套用有效主題。
- 只有首頁 `/` 可以顯示主題模式選擇控制項。
- 主題切換不得重新載入頁面、提交抽卡或卡牌表單、清除搜尋條件、清除未送出欄位、清除驗證訊息或移除已揭示抽卡結果。
- 無效 localStorage 值、localStorage 不可用或 system preference 不可判斷時，不得中斷頁面；回到 `system` 與 light 安全預設。
- 錯誤訊息、console 診斷與日誌不得包含秘密值、連線字串、完整資料檔內容、stack trace、系統提示或未清理的 localStorage 值。
- localStorage 讀取、寫入或跨分頁同步失敗時，若實作輸出診斷，必須使用非敏感且可測試的 console warning 名稱：`CardPickerThemePreferenceReadFailed`、`CardPickerThemePreferenceWriteFailed` 或 `CardPickerThemeSyncFailed`；診斷內容不得包含例外堆疊或原始偏好值。

## Layout 初始主題套用

**檔案**: `Pages/Shared/_Layout.cshtml`

**目的**: 在首次可見呈現前套用有效主題，避免亮暗主題閃爍。

**必須行為**:

- 在 Bootstrap 與 `site.css` stylesheet 載入前執行最小 head bootstrap script。
- 讀取 localStorage key `cardpicker.theme.mode`。
- 僅接受 `light`、`dark`、`system`。
- 無偏好或無效偏好時使用 `system`。
- `system` 模式下透過 `matchMedia('(prefers-color-scheme: dark)')` 推導有效主題；能力不可用時有效主題為 `light`。
- 設定：

```html
<html lang="zh-Hant" data-bs-theme="light|dark" data-theme-mode="light|dark|system">
```

**失敗行為**:

| 條件 | 回應 |
|------|------|
| localStorage 讀取丟出例外 | 套用 `data-theme-mode="system"` 與可判斷的 system effective theme；不可判斷時使用 `light`。 |
| matchMedia 不存在或丟出例外 | 套用 `data-theme-mode="system"` 與 `data-bs-theme="light"`。 |
| localStorage 值不是三個允許值 | 忽略該值並採 `system`。 |

## GET `/` 首頁主題控制

**Page**: `Pages/Index.cshtml`

**目的**: 提供唯一的主題模式選擇入口。

**必須呈現**:

- 一組主題模式 radio group 或等效 segmented radio 控制。
- 群組標題使用繁體中文，例如「網站主題」。
- 三個選項與 value：

| 顯示文字 | 技術值 | 行為 |
|----------|--------|------|
| 亮色模式 | `light` | 立即套用亮色有效主題並保存 `light`。 |
| 暗黑模式 | `dark` | 立即套用暗黑有效主題並保存 `dark`。 |
| 跟隨系統 | `system` | 保存 `system`，並依目前系統外觀偏好推導有效主題。 |

**可及性契約**:

- 控制項必須可用滑鼠、觸控與鍵盤操作。
- 目前選取模式必須有可見狀態，且可被輔助科技辨識。
- 焦點指示在 light/dark 兩種有效主題下都必須清楚可見。
- 控制項不得與首頁既有餐別選擇、投幣控制、拉桿按鈕或老虎機視覺區重疊。

**互動契約**:

- 使用者選擇任何模式後，首頁 1 秒內套用一致有效主題。
- 成功寫入 localStorage 時，後續站內頁面與回訪沿用該選取模式。
- 寫入 localStorage 失敗時，當前頁面仍立即套用使用者剛選擇的模式，但後續回訪可回到安全預設。
- 切換主題不得改變 `MealType`、`CoinInserted`、validation summary、目前揭示的抽卡結果或任何卡牌資料。
- localStorage 寫入失敗時，若輸出 console 診斷，僅能輸出 `CardPickerThemePreferenceWriteFailed` 或等效非敏感事件名稱，不得輸出使用者輸入、完整例外或 stack trace。

## 非首頁頁面

**Pages**:

- `/Privacy`
- `/Error`
- `/Cards`
- `/Cards/{id}`
- `/Cards/Create`
- `/Cards/Edit/{id}`

**契約**:

- 必須透過 shared layout 套用 `data-bs-theme` 與 `site.css` 主題樣式。
- 不得輸出「亮色模式」、「暗黑模式」、「跟隨系統」主題選擇控制項。
- 既有頁面功能、表單欄位、查詢字串、狀態碼與 validation 行為不因主題功能改變。
- 使用者從首頁切換主題後前往任一非首頁頁面，該頁面必須使用同一選取模式推導出的有效主題。

## localStorage 契約

**Key**: `cardpicker.theme.mode`

**允許值**:

- `light`
- `dark`
- `system`

**禁止內容**:

- 有效主題推導結果作為額外欄位。
- JSON object、timestamp、使用者識別資訊、餐點資料或任何秘密值。
- 繁體中文顯示文字。

**讀取規則**:

- 缺失或空字串視為 `system`。
- 無效值視為 `system`。
- 讀取例外不得中斷頁面。
- 讀取例外若輸出 console 診斷，僅能輸出 `CardPickerThemePreferenceReadFailed` 或等效非敏感事件名稱，不得輸出原始儲存值、完整例外或 stack trace。

**寫入規則**:

- 使用者在首頁選擇模式時寫入完整模式值。
- 寫入例外不得阻止當前頁面套用新外觀。
- 寫入失敗不得建立其他 fallback key。
- 寫入例外不得留下無法辨識的偏好狀態；下一次載入必須能回到 `system` 或 light 安全預設。

## System 模式契約

**觸發**: 使用者選擇「跟隨系統」。

**規則**:

- `matchMedia('(prefers-color-scheme: dark)')` 為 true 時，有效主題為 dark。
- `matchMedia('(prefers-color-scheme: dark)')` 為 false 時，有效主題為 light。
- system preference 在使用期間變更時，網站 2 秒內更新有效主題。
- 使用者明確選擇 `light` 或 `dark` 時，system preference 變更不得改變有效主題。

## 跨分頁同步契約

**觸發**: 同一 origin 的另一分頁更新 `cardpicker.theme.mode`。

**規則**:

- 透過 `storage` event 接收新值。
- 僅處理 key 為 `cardpicker.theme.mode` 的事件。
- 新值無效或 null 時視為 `system`。
- 已開啟同站分頁必須在 2 秒內套用最新有效主題。
- 同步不得重載頁面或清除使用者目前頁面的表單、搜尋條件、驗證訊息或抽卡結果。
- storage event 處理失敗若輸出 console 診斷，僅能輸出 `CardPickerThemeSyncFailed` 或等效非敏感事件名稱，不得輸出原始事件值、完整例外或 stack trace。

## CSS 與視覺契約

**檔案**: `wwwroot/css/site.css`

**必須覆蓋範圍**:

- `body`
- shared navbar/footer
- 首頁 slot-machine、meal selector、coin control、lever button、draw result
- card library、search panel、card list、card detail、card form、delete panel
- Bootstrap form control、button、alert、link、validation message 與 focus state

**可及性要求**:

- light/dark 有效主題下，文字與互動元件對比符合 WCAG 2.2 AA。
- 焦點指示在所有主要背景上可見。
- 主題控制必須可用鍵盤完成選取，且 mobile viewport 必須以觸控或等效 pointer event 驗證可操作。
- 手機、平板與桌面寬度下不得有文字、按鈕、卡牌、表單或老虎機元素重疊或水平溢出。
- 不得以色彩作為唯一狀態提示；目前選取模式需有 checked/active 語意。

## Production 安全標頭契約

正式環境回應必須保留：

- `Strict-Transport-Security`，由 `UseHsts()` 提供。
- `Content-Security-Policy`，至少限制 `default-src 'self'`。

主題 head bootstrap script 必須使用下列其中一種策略納入 CSP：

- 固定 inline script hash。
- 每個 response 產生 nonce 並套用至 script 與 CSP。
- 其他等效且可測試的明確允許策略。

不得因主題功能移除 CSP 或新增不必要的外部 script 來源。

**最終採用策略（2026-05-12）**:

- `Program.cs` 在 production response 產生每次請求專用 nonce，存入 `HttpContext.Items["CspNonce"]`。
- `_Layout.cshtml` 將同一 nonce 套用在主題 head bootstrap script 的 `nonce` attribute。
- CSP 使用 `script-src 'self' 'nonce-{value}'`，移除 script `unsafe-inline`，保留 `default-src 'self'`、HSTS、`style-src 'self' 'unsafe-inline'`、`img-src 'self' data:`、`font-src 'self'`、`object-src 'none'`、`base-uri 'self'`、`form-action 'self'` 與 `frame-ancestors 'none'`。

## DOM Attribute Contract

Shared layout 會在 CSS 載入前於 `<html>` 設定下列 attribute：

```html
<html lang="zh-Hant" data-bs-theme="light|dark" data-theme-mode="light|dark|system">
```

- `data-theme-mode` 永遠代表使用者選取模式，僅允許 `light`、`dark`、`system`。
- `data-bs-theme` 永遠代表 Bootstrap 有效主題，僅允許 `light` 或 `dark`。
- 首頁主題控制項使用 `data-theme-mode-selector`，非首頁不得輸出此 attribute。
- 主題偏好 storage key 固定為 `cardpicker.theme.mode`。

## 測試契約

自動化測試至少必須驗證：

- 首頁包含三個主題模式選項，值為 `light`、`dark`、`system`，文案為繁體中文。
- 非首頁不包含主題模式選擇控制項。
- 無 localStorage 時預設模式為 `system`。
- 無效 localStorage 值回到 `system`。
- `light` 與 `dark` 選擇會保存並立即套用。
- `system` 模式會依 browser color scheme 推導有效主題。
- system preference 變更與跨分頁 storage event 在 2 秒內更新有效主題。
- 首頁主題控制可用鍵盤與 mobile pointer/touch 操作切換，並在 1 秒內更新 `data-bs-theme` 與 `data-theme-mode`。
- localStorage 讀取、寫入與 storage event 失敗情境會安全 fallback，且 console 診斷不包含秘密值、原始偏好值、完整例外或 stack trace。
- 主要頁面在 light/dark/system 模式與手機、平板、桌面 viewport 下通過 automated axe 或等效可及性 smoke 檢查；若自動化工具無法完整量測 WCAG 2.2 AA 對比，必須記錄人工 contrast/focus 驗證證據。
- 主題切換不改變抽卡結果、搜尋條件、未送出表單輸入或餐點資料檔。
- production CSP 與 HSTS 測試仍通過。
