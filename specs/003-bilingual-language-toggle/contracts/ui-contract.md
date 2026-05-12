# UI 契約: 雙語語系切換

## 範圍

本功能不暴露外部 JSON API。公開介面是 Razor Pages 輸出的 HTML、shared layout 語系切換表單、ASP.NET Core culture cookie、Razor Page handlers、DOM state、表單欄位、query string、production 安全標頭與使用者可見文字。所有 state-changing forms 必須包含 ASP.NET Core Anti-Forgery token。

## 全域規則

- 支援語系僅包含繁體中文 `zh-TW` 與英文 `en-US`。
- 無 cookie、cookie 無效、cookie unsupported 或 cookie 無法讀取時，必須使用 `zh-TW`。
- 不得使用 browser `Accept-Language` 將首次造訪自動切成英文。
- `<html lang>` 必須依目前語系輸出 `zh-Hant` 或 `en`。
- shared layout 必須在所有主要頁面顯示語系切換入口與目前語系。
- 語系切換不得重新抽卡、不得改變搜尋條件、不得清除未送出表單輸入、不得清除 validation message、不得清除刪除確認狀態、不得改變餐點卡牌資料。
- 使用者可見文字、表單 label、validation message、成功/失敗/復原訊息、fallback prompt 與餐別名稱必須依目前語系呈現。
- 錯誤訊息、console 診斷與日誌不得包含秘密值、連線字串、完整資料檔內容、stack trace、系統提示或未清理的使用者輸入。

## Localization Middleware Contract

**檔案**: `Program.cs`

**必須設定**:

- `AddLocalization(options => options.ResourcesPath = "Resources")`
- Razor Pages view localization。
- DataAnnotations localization，使用 shared resource 或等效集中 resource。
- `RequestLocalizationOptions`:
  - `DefaultRequestCulture = zh-TW`
  - `SupportedCultures = [zh-TW, en-US]`
  - `SupportedUICultures = [zh-TW, en-US]`
  - Request culture providers 僅保留 cookie provider 或等效「明確使用者偏好」provider。

**middleware order**:

- `UseRequestLocalization` 必須在 Razor Pages handler 執行前。
- production HSTS/CSP 必須保留。
- 靜態資源仍使用 `MapStaticAssets` / `WithStaticAssets`。

## Shared Layout Language Switch

**檔案**:

- `Pages/Shared/_Layout.cshtml`
- `Pages/Shared/_LanguageSwitcher.cshtml`

**必須呈現**:

- 目前語系顯示，例如繁中模式顯示「目前語言：繁體中文」，英文模式顯示 `Current language: English`。
- 可切換到另一語系的按鈕或 segmented control。
- 控制項必須可用鍵盤、滑鼠與觸控操作。
- 控制項必須具有可見 focus state 與輔助科技可辨識名稱。

**禁止**:

- 不得只在首頁顯示語系切換。
- 不得使用只有色彩差異的目前語系狀態。
- 不得把 culture value 儲存在 URL 作為主要偏好。

## POST `/Language?handler=Set`

**Page**: `Pages/Language.cshtml.cs`

**目的**: 設定 ASP.NET Core culture cookie 並回到原頁。

**表單欄位**:

| 欄位 | 必填 | 規則 |
|------|------|------|
| `culture` | 是 | 僅允許 `zh-TW` 或 `en-US`。 |
| `returnUrl` | 是 | 必須是 local URL；保留 path/query/fragment 可用資訊。 |
| `stateToken` | 否 | 若 JS enhancement 用於還原表單狀態，必須是 session-scoped non-secret key。 |

**必須行為**:

- 驗證 Anti-Forgery token。
- 驗證 `culture` 白名單。
- 無效 `culture` 使用 `zh-TW` 或回傳目前語系可理解的 validation error；不得寫入 unsupported cookie。
- `returnUrl` 非本機 URL 時改回 `/`。
- 使用 ASP.NET Core culture cookie 格式寫入 `.AspNetCore.Culture`。
- Cookie 建議屬性：
  - `HttpOnly = true`
  - `SameSite = Lax`
  - `Secure = Request.IsHttps`
  - `IsEssential = true`
  - `Expires` 約 1 年
- 成功後 redirect 回 `returnUrl`。

**失敗行為**:

| 條件 | 回應 |
|------|------|
| Anti-Forgery 驗證失敗 | 回傳 framework 預設 400；不得設定 cookie。 |
| unsupported culture | 回到 `zh-TW` 或顯示目前語系錯誤；不得寫入 unsupported value。 |
| cookie 寫入不可用 | 當前 request 可用選定語系呈現可理解訊息；後續 request 回到 `zh-TW`。 |
| unsafe returnUrl | redirect `/`。 |

## GET `/` 首頁契約

**必須呈現**:

- localized 導覽、功能名稱、餐別、投幣/確認、拉桿/start、slot-machine state text、抽卡結果與錯誤訊息。
- shared layout language switch。
- 若 URL 或 hidden state 指向已揭示 draw result，必須以目前語系重新 render 同一 `CardId`，不得重新抽卡。
- 英文模式下，卡牌缺少英文內容時顯示繁中 fallback 並提供補齊英文翻譯 action。

**狀態保留**:

- 語系切換後，已選餐別、coin state、未送出欄位與已揭示 result 不得消失。
- 若 draw result card 已被刪除或不再符合餐別，顯示目前語系的復原訊息，不得替換抽卡結果。

## POST `/?handler=Draw`

**目的**: 驗證餐別與 coin/confirmation state 後抽出一張卡牌。

**必須行為**:

- 驗證 Anti-Forgery token。
- 驗證 `MealType` 是 `Breakfast`、`Lunch` 或 `Dinner`。
- 驗證 coin/confirmation state。
- 若 card library blocked，顯示目前語系 recovery message，並禁用 draw。
- 成功時只從所選餐別有效卡牌中等機率抽取。
- 成功 result 必須包含 card ID，使後續語系切換可重新 render 同一張卡牌。
- 顯示名稱、描述與餐別使用目前語系投影。

**禁止**:

- 不得因語系、動畫 timing、display order 或 repeated click 影響隨機選取。
- 不得在語系切換時再次呼叫 randomizer。

## `/Cards` 卡牌列表與搜尋契約

**GET `/Cards` query**:

| 欄位 | 規則 |
|------|------|
| `keyword` | Trim 後用目前語系 visible card name 比對。 |
| `mealType` | 空值或 `Breakfast`/`Lunch`/`Dinner`。 |

**必須呈現**:

- localized 搜尋 label、placeholder、button、結果數、無結果訊息、餐別名稱與卡牌內容。
- 英文模式下缺少英文內容的卡牌使用繁中 fallback，並顯示補齊英文翻譯 prompt/action。
- 語系切換後保留 query string 搜尋條件。

**搜尋規則**:

- 只搜尋目前語系可見 `DisplayName`。
- 英文模式 fallback 卡牌以繁中 fallback name 比對。
- 不得因語系切換改變卡牌 ID、排序穩定性以外的資料內容或已刪除狀態。

## 卡牌詳情契約

**GET `/Cards/Details?id={guid}` 或既有 route equivalent**

**必須呈現**:

- localized page title、labels、actions、餐別名稱、name、description。
- 英文缺漏時顯示 fallback badge/prompt，例如目前語系的「需要英文翻譯」/`Needs English translation`。
- 補齊翻譯 action 連到 edit page 並保留 card ID。

**失敗行為**:

- 找不到 card 時顯示目前語系 not-found message。
- Card library blocked 時顯示目前語系 recovery message。

## 卡牌 Create/Edit 契約

**Pages**:

- `GET /Cards/Create`
- `POST /Cards/Create`
- `GET /Cards/Edit?id={guid}` 或既有 route equivalent
- `POST /Cards/Edit?id={guid}` 或既有 route equivalent

**表單欄位**:

| 欄位 | 必填 | 顯示規則 |
|------|------|----------|
| `NameZhTw` | 是 | 繁中餐點名稱 label 依目前 UI 語系呈現。 |
| `DescriptionZhTw` | 是 | 繁中餐點描述 label 依目前 UI 語系呈現。 |
| `NameEnUs` | 是 | 英文餐點名稱 label 依目前 UI 語系呈現。 |
| `DescriptionEnUs` | 是 | 英文餐點描述 label 依目前 UI 語系呈現。 |
| `MealType` | 是 | options 顯示為目前語系餐別名稱。 |

**必須行為**:

- 所有 POST 包含 Anti-Forgery token。
- 所有 validation message 使用目前語系。
- 任一必要語系 name/description 缺漏時拒絕儲存。
- 任一語系 duplicate 時拒絕儲存，原卡牌不得被局部改動。
- 語系切換不得清除未送出表單輸入或目前 validation state。
- 若 edit 進入英文缺漏卡牌，英文欄位可先為空並顯示補齊提示；送出時仍必須填完整英文。

## 刪除確認契約

**Pages**:

- `GET /Cards/Delete?id={guid}` 或既有 delete confirmation flow
- `POST /Cards/Delete?id={guid}`

**必須行為**:

- 確認文字、按鈕、餐別、餐點名稱與描述依目前語系呈現。
- 英文缺漏時 fallback 顯示並提示缺翻譯，不得顯示空白。
- 語系切換不得清除刪除確認上下文。
- POST 必須 Anti-Forgery；成功/失敗訊息依目前語系呈現。

## Privacy 與 Error 頁契約

- 必須顯示 shared language switch。
- 所有標題、說明、錯誤提示與 recovery message 依目前語系呈現。
- Error page 不得輸出 stack trace、系統提示、完整例外或秘密值。

## JSON Persistence Contract

**檔案**: `CardPicker2/data/cards.json`

**schema v2 root**:

| 欄位 | 必填 | 規則 |
|------|------|------|
| `schemaVersion` | 是 | 值為 `2`。 |
| `cards` | 是 | array，可空但 seed 建立時每餐別至少 3 張。 |

**card object**:

| 欄位 | 必填 | 規則 |
|------|------|------|
| `id` | 是 | Guid，不可空。 |
| `mealType` | 是 | `Breakfast`、`Lunch`、`Dinner`。 |
| `localizations` | 是 | 包含 `zh-TW`；新增/編輯後必須包含 `en-US`。 |

**寫入規則**:

- 建立完整新 document。
- 寫入同目錄 temp file。
- flush。
- 原子替換 target。
- 失敗時刪除 temp file 並保留原 target。
- corrupted/unreadable/unsupported 原檔不得被 seed 或 migration 覆蓋。

## CSS 與 Responsive 契約

**檔案**: `wwwroot/css/site.css`

**必須覆蓋**:

- shared language switcher。
- 目前語系狀態。
- 雙語 card form 欄位群組。
- fallback/missing translation badge 與 action。
- 首頁 slot-machine、卡牌列表、詳情、表單、刪除確認在英文長字串下的 spacing。

**要求**:

- 390x844、768x1024、1366x768 下兩語系都不得水平溢出。
- Button text、label、badge 與 card content 不得重疊。
- Focus state 在 light/dark theme 與兩語系下都可見。
- 不得只用顏色傳達目前語系或 fallback 狀態。

## JavaScript Progressive Enhancement Contract

**檔案**: `wwwroot/js/site.js`

**用途**:

- 切換語系前保存目前頁面的 transient UI state。
- redirect/re-render 後還原未送出表單輸入、搜尋欄位、validation message context 與 draw result display state。
- 重新觸發 jQuery Validation，使 message 使用新語系的 data-val attributes。

**限制**:

- 不得把完整餐點資料、秘密值、系統提示、stack trace 或完整 JSON 存入 browser storage。
- Transient state 必須與 path/form scope 綁定，並在成功還原後清除。
- JS 不可取代 server-side validation；只用於 UX state preservation。

## Production 安全標頭契約

正式環境回應必須保留：

- `Strict-Transport-Security`。
- `Content-Security-Policy`，至少限制 `default-src 'self'`。

語系功能不得新增外部 script source。若新增 inline script，必須使用既有 nonce 或等效 CSP 允許策略。不得移除 HSTS、CSP、Razor HTML encoding 或 Anti-Forgery。

## 測試契約

自動化測試至少必須驗證：

- 無 cookie 時首頁與主要頁面為繁體中文。
- `en-US` culture cookie 讓首頁與主要頁面呈現英文。
- unsupported cookie 值 fallback 到繁體中文。
- shared layout 在所有主要頁面顯示語系切換與目前語系。
- `POST /Language?handler=Set` 要求 Anti-Forgery，且只接受 `zh-TW`/`en-US`。
- 語系切換保留搜尋 query、未送出表單、validation state、刪除確認與 draw result card ID。
- Resource key 兩語系完整，主要頁面沒有未翻譯 label/button/status/message。
- DataAnnotations 與服務結果訊息使用目前語系。
- schema v1 檔案可讀取為繁中內容 + 英文缺漏 fallback，不會被 corrupted handling 誤判。
- 缺檔建立 schema v2 雙語 seed，每餐別至少 3 張。
- 新增/編輯缺少任一語系欄位會失敗。
- 任一語系 duplicate name+description 同餐別會失敗且不改變原卡。
- 搜尋只比對目前語系 visible name，英文缺漏時比對 fallback name。
- 抽卡只從所選餐別選取，語系切換只改變顯示投影，不重新抽卡。
- production CSP/HSTS 測試仍通過。
- 390x844、768x1024、1366x768 兩語系無水平溢出或重疊，並符合 WCAG 2.1 AA smoke 檢查。
