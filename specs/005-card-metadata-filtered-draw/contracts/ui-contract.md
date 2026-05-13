# UI 契約: 餐點條件篩選抽卡

## 範圍

本功能不暴露外部 JSON API。公開介面是 Razor Pages 輸出的 HTML、首頁抽卡表單、卡牌庫 GET query、card create/edit/details/delete 表單與頁面狀態、ASP.NET Core culture cookie、production 安全標頭與使用者可見文字。所有 state-changing forms 必須包含 ASP.NET Core Anti-Forgery token。

## 全域規則

- 所有使用者可見文案依目前 runtime 語系呈現；未選擇、無效或不可用時使用 `zh-TW`。
- 篩選條件只縮小候選池，不得改變候選池內等機率 `1/N` 規則。
- 未套用 metadata 條件時，缺 metadata 的 active cards 仍可抽卡與搜尋。
- 套用某項 metadata 條件時，缺該欄位的 card 不符合該條件。
- 語系切換、主題切換、動畫、reduced motion、顯示排序、統計列排序與歷史抽中次數不得改變候選池權重、抽卡結果、歷史紀錄或統計口徑。
- blocked library、corrupted/unreadable/unsupported JSON、metadata validation failure、空候選池或寫入失敗時，不得顯示成功抽卡結果，也不得新增成功歷史。
- 錯誤訊息、HTML、console 診斷與日誌不得包含秘密值、連線字串、完整 JSON、stack trace、系統提示或未清理使用者輸入。

## GET `/` 首頁契約

**必須呈現**:

- shared layout 語系切換與既有導覽。
- 正常模式/隨機模式選擇。
- 正常模式的餐別選擇；隨機模式下餐別不得限制候選池。
- 決策資訊篩選控制，至少包含：
  - 價格區間。
  - 準備或等待時間區間。
  - 飲食偏好多選。
  - 最高可接受辣度。
  - 標籤多選或 tag input。
- 目前已套用條件摘要與清除條件入口。
- coin-in 或等效確認控制項。
- lever/start action。
- slot-machine visual area、狀態文字與 revealed result area。
- hidden `DrawOperationId`。
- 既有總成功抽取次數與統計表。

**篩選控制規則**:

- 控制項必須可用鍵盤、滑鼠與觸控操作。
- 目前選取條件不得只依賴顏色傳達。
- 標籤與條件摘要在手機寬度不得水平溢出。
- 清除條件不得清除語系、主題、draw operation id 或既有成功結果；只清除 metadata filters 與卡牌庫相關 query/filter state。

**blocked state**:

- card library blocked 時禁用 draw action 與 metadata-changing card operations。
- 首頁篩選控制可顯示但不得提交成功 draw。
- 統計區顯示目前語系 recovery message。

## POST `/?handler=Draw`

**目的**: 驗證模式、餐別、coin state、operation id、metadata filters 與 card library 狀態後，完成一次 idempotent filtered draw。

**表單欄位**:

| 欄位 | 必填 | 規則 |
|------|------|------|
| `drawMode` | 是 | `Normal` 或 `Random`。 |
| `mealType` | Normal 是；Random 否 | Normal 必須是 `Breakfast`、`Lunch`、`Dinner`；Random 不得限制候選池。 |
| `coinInserted` | 是 | 必須為 true。 |
| `drawOperationId` | 是 | 非空 Guid；用於 replay 同一次成功操作。 |
| `priceRange` | 否 | 空值或支援 enum。 |
| `preparationTimeRange` | 否 | 空值或支援 enum。 |
| `dietaryPreferences` | 否 | 多選；每個值必須受支援。 |
| `maxSpiceLevel` | 否 | 空值或支援 enum。 |
| `tags` | 否 | 多選或分隔文字；trim、去空白、case-insensitive 去重。 |

**必須行為**:

- 驗證 Anti-Forgery token。
- 驗證 `drawOperationId` 非空且格式有效。
- 若 `drawOperationId` 已有成功歷史，重顯原成功結果，`IsReplay = true`，不得重新套用目前畫面上的新 filters、重新 randomize 或新增 history。
- Normal mode 必須先依餐別建立 active base pool，再套用所有 metadata filters。
- Random mode 不要求餐別，從全部 active cards 建立 base pool，再套用所有 metadata filters。
- 篩選後候選池包含 N 張 cards 時，每張標稱抽中機率為 `1/N`。
- 首次成功時，結果與一筆 `DrawHistoryRecord` 必須同時寫入。
- 成功結果顯示抽中卡牌的餐別、名稱、描述與 metadata 摘要。
- 結果摘要不得宣稱某條件提高機率、推薦分數、保底或偏好加權。

**失敗行為**:

| 條件 | 回應 |
|------|------|
| Anti-Forgery 驗證失敗 | framework 預設 400；不得新增 history。 |
| invalid/missing mode | 顯示目前語系 validation message；不得新增 history。 |
| Normal missing/invalid meal type | 顯示目前語系 validation message；不得新增 history。 |
| missing coin confirmation | 顯示 coin required message；不得新增 history。 |
| invalid metadata filter value | 顯示目前語系 validation message；不得新增 history。 |
| filtered candidate pool empty | 顯示空候選池 message；不得新增 history 或改變 statistics。 |
| card library blocked | 顯示 recovery message；不得新增 history。 |
| write failure | 顯示暫時無法完成抽卡 message；不得宣告成功。 |

## GET `/` result restore 契約

**query/hidden state**:

| 欄位 | 規則 |
|------|------|
| `resultCardId` | 可用於語系切換或 redirect 後重顯同一結果。 |
| `drawOperationId` | 若同時存在成功歷史，優先依 operation id replay。 |
| `drawMode` | 用於顯示原 mode。 |
| `mealType` | Normal 結果的原 requested meal type；Random 顯示抽中卡牌餐別。 |
| metadata filter fields | 可用於顯示原提交條件摘要；不得觸發重新抽卡。 |

**必須行為**:

- 重新 render 結果時不得重新抽卡。
- 若 card 已被 deleted，但結果對應既有成功 history，仍可顯示該結果並標示狀態。
- 語系切換只重新投影 metadata display text，不改變 card ID 或 statistics。

## GET `/Cards` 卡牌庫契約

**query 欄位**:

| 欄位 | 規則 |
|------|------|
| `keyword` | Trim 後用目前語系 visible card name 比對。 |
| `mealType` | 空值或 `Breakfast`/`Lunch`/`Dinner`。 |
| `priceRange` | 空值或支援 enum。 |
| `preparationTimeRange` | 空值或支援 enum。 |
| `dietaryPreferences` | 多選，每個值必須受支援。 |
| `maxSpiceLevel` | 空值或支援 enum。 |
| `tags` | 多選或分隔文字；trim 後 case-insensitive 比對。 |

**必須呈現**:

- keyword、meal type 與 metadata 篩選控制。
- 目前已套用條件摘要。
- 清除條件入口。
- 結果數或無結果訊息。
- 每張卡牌的 metadata 摘要；缺漏欄位以不誤導的空狀態呈現。

**搜尋規則**:

- keyword、meal type 與 metadata filters 採交集。
- 一般列表與搜尋結果只包含 active cards。
- deleted cards 不得出現在卡牌庫一般篩選結果。
- 英文模式缺英文內容時，沿用既有 fallback 規則，不得顯示未翻譯 key。

## Card Details 契約

**必須呈現**:

- 既有雙語 card content、餐別與狀態。
- metadata 摘要：tags、price range、preparation time、dietary preferences、spice level。
- 缺漏 metadata 欄位時顯示目前語系的空狀態，例如「尚未填寫」或 `Not set`。
- edit/delete/back actions 維持既有規則。

**禁止**:

- 不得把缺漏 metadata 顯示為符合任何條件。
- 不得以 metadata 呈現推薦分數、稀有度或價值等級。

## Card Create/Edit 契約

**表單欄位**:

| 欄位 | 必填 | 顯示規則 |
|------|------|----------|
| 既有雙語 name/description | 是 | 維持 003 規則。 |
| `MealType` | 是 | options 依目前語系顯示。 |
| `TagsInput` 或 `Tags` | 否 | 可用文字、chips 或多輸入；說明文字依目前語系。 |
| `PriceRange` | 否 | 空值 + Low/Medium/High。 |
| `PreparationTimeRange` | 否 | 空值 + Quick/Standard/Long。 |
| `DietaryPreferences` | 否 | checkbox 多選。 |
| `SpiceLevel` | 否 | 空值 + None/Mild/Medium/Hot。 |

**必須行為**:

- POST 包含 Anti-Forgery token。
- 所有 validation message 使用目前語系。
- metadata 欄位可留空；必要雙語欄位仍必填。
- metadata 有值但無效時拒絕儲存。
- 編輯 metadata 不得改變 `MealCard.Id`、status、draw history 或 statistics。
- duplicate detection 不包含 metadata；相同名稱/描述/餐別仍依既有規則判斷。
- 儲存失敗不得局部改動 JSON。

## Reduced Motion 與動畫契約

- `prefers-reduced-motion: reduce` 時不得播放連續旋轉或長時間動畫。
- reduced motion 仍必須揭示有效靜態結果。
- 動畫開始、結束、延遲、frame count、CSS class 與 filter UI transition 不得決定 card ID。
- JS 可禁用按鈕避免 UX 快速連點，但 server idempotency 才是資料完整性來源。

## CSS 與 Responsive 契約

**檔案**: `CardPicker2/wwwroot/css/site.css`

**必須覆蓋**:

- 首頁 filter panel。
- 卡牌庫 filter panel。
- active filter chips / summary。
- metadata badges。
- tag list wrapping。
- create/edit metadata field groups。

**要求**:

- 390x844、768x1024、1366x768 下 `zh-TW` 與 `en-US` 都不得水平溢出。
- Button text、label、badge、tag chips、filter groups 與 card content 不得重疊。
- Focus state 在 light/dark theme 與兩語系下都可見。
- 不得只用顏色傳達選取 filter、辣度、deleted status 或 fallback 狀態。

## JavaScript Progressive Enhancement 契約

**檔案**: `CardPicker2/wwwroot/js/site.js`

**用途**:

- 切換語系或主題前保存 transient filter UI state。
- redirect/re-render 後還原未送出 create/edit metadata 欄位與首頁 filter state。
- 對 tag chips、clear filters、client validation 提供 UX enhancement。

**限制**:

- JS 不得決定抽卡候選池或結果。
- 不得把完整餐點資料、完整 JSON、秘密值、系統提示或 stack trace 存入 browser storage。
- Transient state 必須與 path/form scope 綁定，成功還原後清除。
- JS 不可取代 server-side validation。

## 安全與標頭契約

- 所有 state-changing forms 包含 Anti-Forgery token。
- Production 環境必須保留 HTTPS redirection、HSTS 與 CSP。
- 本功能不得新增外部 script/style/font/image 來源。
- Razor HTML encoding 必須保留；metadata tags 以文字輸出，不使用 raw HTML。
- UI、日誌與 validation message 不得輸出完整 JSON、stack trace、系統提示、秘密值或未清理輸入。

## 日誌契約

**應記錄**:

- schema v4 load/migration status。
- metadata validation failure 類型，不含未清理 tag 原文或完整 card content。
- filtered draw success：mode、card ID、pool size、operation ID、filter count。
- empty filtered pool。
- filtered search result count。
- write failure 與 blocked library。

**不得記錄**:

- 完整 `cards.json`。
- 使用者輸入的完整描述、tag list 原文或完整 metadata payload。
- 秘密值、連線字串、系統提示、stack trace 直接暴露到 UI。

## 測試契約

自動化測試至少必須驗證：

- schema v3 讀取後可映射為 v4 且舊卡 metadata 缺漏不 blocked。
- invalid metadata enum/tag value 不得持久化。
- create/edit metadata 成功後重啟仍保留。
- duplicate detection 不因 metadata 不同而放寬既有 name+description+meal type 規則。
- 首頁 Normal mode 先依餐別再套用 metadata filters。
- 首頁 Random mode 忽略餐別但套用 metadata filters。
- 多個 dietary preferences 與多個 tags 採 all-match。
- max spice level 使用小於等於規則。
- 缺 metadata 的 card 在未篩選時可抽，在套用該欄位 filter 時不符合。
- empty filtered pool 不新增 history 或 statistics。
- filtered pool 內仍維持 `1/N` nominal probability。
- `/Cards` keyword + meal type + metadata filters 採交集。
- 語系切換保留 filter state，且所有 metadata labels/options/messages 兩語系完整。
- production CSP/HSTS 與 Anti-Forgery 測試仍通過。
- 390x844、768x1024、1366x768 兩語系無水平溢出或重疊，並符合 WCAG 2.1 AA smoke 檢查。
