# 資料模型: 網站主題模式切換

## 概觀

本功能的資料邊界是瀏覽器本機主題偏好與 DOM 呈現狀態，不新增伺服器資料表、不新增 JSON API，也不修改餐點卡牌庫 `CardPicker2/data/cards.json`。主題偏好只代表同一瀏覽器與裝置上的使用者選取模式；有效主題每次由選取模式與系統外觀偏好推導。

模型名稱以英文描述技術狀態；使用者可見文字使用繁體中文。

## Entity: ThemeMode

代表使用者可選擇並保存的主題模式。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Value` | string enum | 是 | 僅允許 `light`、`dark`、`system`。 |
| `DisplayName` | string | 是 | 顯示為「亮色模式」、「暗黑模式」、「跟隨系統」。 |
| `Description` | string | 否 | 輔助說明可使用繁體中文，例如 system 代表依裝置或瀏覽器外觀偏好推導。 |

**驗證規則**:

- localStorage、DOM attribute 或控制項輸入中的模式值都必須以白名單驗證。
- 無效、空白、缺失或無法讀取的值一律視為 `system`。
- 英文值只作為穩定技術識別，不取代繁體中文 UI 標示。

## Entity: EffectiveTheme

代表網站實際呈現的 light/dark 外觀。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Value` | string enum | 是 | 僅允許 `light` 或 `dark`。 |
| `DerivedFromMode` | `ThemeMode` | 是 | 來源選取模式。 |
| `SystemPrefersDark` | bool? | 否 | `system` 模式下的瀏覽器/裝置暗黑偏好；無法判斷時為 null。 |

**推導規則**:

```text
ThemeMode = light  => EffectiveTheme = light
ThemeMode = dark   => EffectiveTheme = dark
ThemeMode = system => EffectiveTheme = dark  when matchMedia('(prefers-color-scheme: dark)').matches
ThemeMode = system => EffectiveTheme = light when system preference is light or unavailable
```

**驗證規則**:

- `EffectiveTheme` 不得保存到 localStorage。
- 系統偏好變更時，只有目前 `ThemeMode` 為 `system` 才重新推導並套用有效主題。
- 明確選擇 `light` 或 `dark` 時，系統偏好變更不得改變有效主題。

## Entity: ThemePreference

代表同一瀏覽器與裝置上保存的使用者主題偏好。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `StorageKey` | string | 是 | 固定為 `cardpicker.theme.mode`。 |
| `Mode` | `ThemeMode` | 是 | localStorage 中的完整值，只能是 `light`、`dark` 或 `system`。 |
| `CanPersist` | bool | 否 | 執行時狀態；localStorage 可用且寫入成功時為 true。 |

**持久化範例**:

```text
cardpicker.theme.mode = dark
```

**驗證規則**:

- localStorage 值不得是 JSON object、有效主題、中文顯示文字或其他 metadata。
- 寫入失敗時，不得留下無法辨識的偏好狀態；目前頁面可用記憶體狀態立即套用使用者選擇，後續載入回到 `system` 或安全預設。
- localStorage 內容視為不可信資料，讀取後必須重新驗證。

## Entity: ThemeApplicationState

代表目前文件上已套用的主題狀態。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `DocumentElement` | DOM `html` | 是 | 主題 attribute 套用在 `document.documentElement`。 |
| `data-bs-theme` | `EffectiveTheme` | 是 | Bootstrap 5.3 color mode 的實際 light/dark 值。 |
| `data-theme-mode` | `ThemeMode` | 是 | 目前選取模式，供 CSS/測試/控制項同步使用。 |
| `color-scheme` | CSS property | 是 | 應與有效主題一致，協助瀏覽器原生控制項顯示。 |

**狀態規則**:

- head bootstrap script 必須在 CSS 載入前設定初始 attribute。
- `site.js` 載入後必須讓首頁 radio checked state 與 `data-theme-mode` 一致。
- 切換主題不得移除或重建抽卡結果、搜尋表單、validation summary 或使用者已輸入內容。

## Entity: ThemeSyncEvent

代表同 origin 已開啟分頁間的主題同步訊號。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Source` | browser event | 是 | 使用 `storage` event。 |
| `Key` | string | 是 | 只處理 `cardpicker.theme.mode`。 |
| `NewValue` | string? | 否 | 驗證為 `ThemeMode`；null 或無效值視為 `system`。 |
| `TargetTabs` | browser tabs | 是 | 同一 origin 的其他已開啟分頁。 |

**同步規則**:

- 使用者在一個分頁變更模式後，目前分頁立即套用並寫入 localStorage。
- 其他同站分頁收到 `storage` event 後，必須在 2 秒內套用最新有效主題。
- 同步事件不得提交表單、重載頁面或清除可見狀態。

## Entity: ThemeControlledSurface

代表受主題影響與驗證的站內頁面範圍。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Path` | string | 是 | 目前可由使用者直接瀏覽的 Razor Page 路徑。 |
| `HasThemeSelector` | bool | 是 | 只有首頁 `/` 為 true，其餘頁面必須為 false。 |
| `AppliesEffectiveTheme` | bool | 是 | 所有主要頁面都必須為 true。 |

**範圍**:

- `/`
- `/Privacy`
- `/Error`
- `/Cards`
- `/Cards/{id}`
- `/Cards/Create`
- `/Cards/Edit/{id}`

## 關係

```text
ThemePreference 1 ── stores ── 1 ThemeMode
ThemeMode 1 ── derives ── 1 EffectiveTheme
EffectiveTheme 1 ── applies to ── 1 ThemeApplicationState
ThemeSyncEvent 0..* ── updates ── ThemePreference
ThemeApplicationState 1 ── affects visual presentation of ── * ThemeControlledSurface
```

## 狀態轉換

```text
NoPreference/InvalidPreference
  -> ThemeMode(system)
  -> EffectiveTheme(system-derived or light fallback)

ThemeMode(system)
  -> system preference changes
  -> EffectiveTheme(light/dark)

ThemeMode(light)
  -> user selects dark
  -> ThemeMode(dark)
  -> EffectiveTheme(dark)

ThemeMode(dark)
  -> user selects system
  -> ThemeMode(system)
  -> EffectiveTheme(system-derived or light fallback)
```

## 不變條件

- 主題切換不得寫入、刪除或重新產生 `CardPicker2/data/cards.json`。
- 主題切換不得改變餐點卡牌、抽卡結果、搜尋條件、表單資料、ModelState 錯誤或資料完整性規則。
- 首頁以外頁面不得輸出主題模式選擇控制項。
- 三種模式下，主要頁面的文字、導覽、按鈕、連結、表單、卡牌、警示、驗證訊息與焦點狀態必須符合 WCAG 2.2 AA 對比與可見焦點指示要求。
