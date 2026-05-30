# UI 契約: 餐點收藏與手動排除抽卡

## 範圍

本功能不暴露外部 JSON API。公開介面是 Razor Pages 輸出的 HTML、首頁抽卡表單、首頁結果區偏好表單、卡牌庫 GET query、卡牌庫/詳情偏好表單、card create/edit/delete 表單與頁面狀態、ASP.NET Core culture cookie、production 安全標頭與使用者可見文字。所有 state-changing forms 必須包含 ASP.NET Core Anti-Forgery token。

## 全域規則

- 所有使用者可見文案依目前 runtime 語系呈現；未選擇、無效或不可用時使用 `zh-TW`。
- 收藏只用於辨識、卡牌庫篩選與結果區整理，不得影響抽卡候選池、候選池排序、randomizer、近期輪替、成功歷史或統計。
- 排除抽卡只縮小未來候選池；不得等同刪除，不得修改既有成功歷史、統計分母、單卡抽中次數、rotation snapshot 或已揭示結果。
- 被排除抽卡的 active card 預設仍顯示於 `/Cards`，可搜尋、查看、編輯、取消排除與刪除。
- deleted cards 不得因偏好狀態回到一般卡牌庫、edit flow 或未來候選池；已抽中歷史仍依 004/006 統計規則呈現。
- 語系切換、主題切換、動畫、reduced motion、顯示排序、統計列排序、card edit/delete 與翻譯/metadata 更新不得改變 preferences、已成立抽卡結果、history 或 statistics。
- blocked library、JSON corrupted/unreadable/unsupported、invalid preference target、空候選池或寫入失敗時，不得顯示成功偏好更新或成功抽卡。
- 錯誤訊息、HTML、console 診斷與日誌不得包含秘密值、連線字串、完整 JSON、完整描述、tag list 原文、stack trace、系統提示或未清理使用者輸入。

## GET `/` 首頁契約

**必須呈現**:

- shared layout 語系切換、主題切換與既有導覽。
- 正常模式/隨機模式選擇。
- 正常模式的餐別選擇；隨機模式下餐別不得限制候選池。
- 005 決策資訊篩選控制與目前條件摘要。
- 006 防重複控制與 N 值。
- coin-in 或等效確認控制項。
- lever/start action。
- slot-machine visual area、狀態文字與 revealed result area。
- hidden `DrawOperationId`。
- 既有總成功抽取次數與統計表。
- 若有已揭示結果，結果區必須顯示該 card 目前收藏與排除狀態，並提供 target-state 收藏/取消收藏、排除/取消排除操作。

**結果區偏好控制規則**:

- 控制項必須可用鍵盤、滑鼠與觸控操作。
- 目前收藏/排除狀態不得只依賴顏色；需有文字、badge、aria-label 或等效可辨識狀態。
- 收藏與排除操作不得重新抽卡。
- 排除剛抽中的 card 後，結果仍顯示同一 card ID，並標示目前已排除。
- 成功/失敗訊息依目前語系呈現。

**blocked state**:

- card library blocked 時禁用 draw action 與 preference-changing forms。
- 首頁偏好控制可顯示 disabled state，但不得提交成功 mutation。
- 統計區顯示目前語系 recovery message。

## POST `/?handler=Draw`

**目的**: 驗證模式、餐別、coin state、operation id、metadata filters、防重複設定與 card library 狀態後，完成一次 idempotent preference-aware draw。

**表單欄位**:

| 欄位 | 必填 | 規則 |
|------|------|------|
| `drawMode` | 是 | `Normal` 或 `Random`。 |
| `mealType` | Normal 是；Random 否 | Normal 必須是 `Breakfast`、`Lunch`、`Dinner`；Random 不得限制候選池。 |
| `coinInserted` | 是 | 必須為 true。 |
| `drawOperationId` | 是 | 非空 Guid；用於 replay 同一次成功操作。 |
| 005 metadata filter fields | 否 | 維持 005 validation 與交集規則。 |
| 006 rotation fields | 否 | 維持 006 validation 與空候選池區分規則。 |

**必須行為**:

- 驗證 Anti-Forgery token。
- 驗證 `drawOperationId` 非空且格式有效。
- 若 `drawOperationId` 已有成功 history，重顯原成功結果與原 rotation snapshot，`IsReplay = true`，不得重新套用目前畫面上的 preferences、N、filters 或最新 history。
- 建立候選池時，先移除 `IsExcludedFromDraw == true` 的 active cards。
- Normal mode 必須從未排除 active cards 依餐別建立 base pool，再套用 metadata filters 與 rotation。
- Random mode 不要求餐別，從全部未排除 active cards 建立 base pool，再套用 metadata filters 與 rotation。
- 最終候選池包含 M 張 cards 時，每張標稱抽中機率為 `1/M`。
- 首次成功時，結果、成功 history 與 006 `RotationSnapshot` 必須同時寫入。
- 成功結果顯示抽中卡牌的餐別、名稱、描述、metadata 摘要、輪替摘要與目前偏好狀態。
- 收藏狀態不得作為候選池條件、排序或權重。

**失敗行為**:

| 條件 | 回應 |
|------|------|
| Anti-Forgery 驗證失敗 | framework 預設 400；不得新增 history。 |
| invalid/missing mode | 顯示目前語系 validation message；不得新增 history。 |
| Normal missing/invalid meal type | 顯示目前語系 validation message；不得新增 history。 |
| missing coin confirmation | 顯示 coin required message；不得新增 history。 |
| invalid metadata filter value | 顯示目前語系 validation message；不得新增 history。 |
| invalid rotation setting | 顯示目前語系可修正訊息；不得新增 history。 |
| preference exclusion 造成無可抽卡牌 | 顯示取消排除、調整條件或新增卡牌等提示；不得新增 history 或改變 statistics。 |
| 005 base/metadata candidate pool empty | 顯示既有空卡池或無符合條件訊息；不得新增 history 或改變 statistics。 |
| 006 rotation pool empty | 顯示防重複造成空候選池訊息；不得新增 history 或改變 statistics。 |
| card library blocked | 顯示 recovery message；不得新增 history。 |
| write failure | 顯示暫時無法完成抽卡 message；不得宣告成功。 |

**禁止**:

- 不得因收藏狀態加權或優先抽中。
- 不得在排除後自動忽略排除狀態抽出 fallback。
- 不得在 GET render、語系切換、主題切換或 result preference update 時重新呼叫 randomizer。

## POST `/?handler=Preference`

**目的**: 從首頁結果區更新已揭示 result card 的收藏或排除狀態，並重顯同一結果。

**表單欄位**:

| 欄位 | 必填 | 規則 |
|------|------|------|
| `cardId` | 是 | 目標 card ID；必須是 active card。 |
| `targetIsFavorite` | 條件 | 有值時將收藏狀態設為該 bool。 |
| `targetIsExcludedFromDraw` | 條件 | 有值時將排除抽卡狀態設為該 bool。 |
| `drawOperationId` | 否 | 用於重顯同一次成功結果；不得觸發新抽卡。 |
| `resultCardId` | 否 | 用於重顯同一 result card；不得作為新抽卡來源。 |
| mode/filter/rotation state | 否 | 僅用於維持畫面狀態；不得重新抽卡。 |

**必須行為**:

- 驗證 Anti-Forgery token。
- 至少一個 target state 欄位必須有值。
- target state 重複提交時，最終狀態必須等於提交值，不得反轉。
- 成功後重顯同一 `resultCardId` 與同一 `drawOperationId` 對應結果。
- 不新增 `DrawHistoryRecord`，不改變 statistics，不修改 rotation snapshot。
- 排除成功只影響後續新的 draw。

**失敗行為**:

| 條件 | 回應 |
|------|------|
| Anti-Forgery 驗證失敗 | framework 預設 400；不得改變 preferences。 |
| invalid/missing card ID | 顯示目前語系 validation message；不得改變 preferences。 |
| card 不存在或已 deleted | 顯示目前語系 not-found/recovery message；不得改變 preferences。 |
| card library blocked | 顯示 recovery message；不得改變 preferences。 |
| write failure | 顯示暫時無法更新偏好 message；不得宣告成功。 |

## GET `/Cards` 卡牌庫契約

**query 欄位**:

| 欄位 | 規則 |
|------|------|
| `keyword` | Trim 後用目前語系 visible card name 比對。 |
| `mealType` | 空值或 `Breakfast`/`Lunch`/`Dinner`。 |
| 005 metadata fields | 維持 005 支援值與 validation。 |
| `favoriteFilter` | `All`、`FavoritesOnly`、`NotFavoritesOnly`。 |
| `drawEligibilityFilter` | `All`、`DrawableOnly`、`ExcludedOnly`。 |

**必須呈現**:

- keyword、meal type、metadata filters 與 preference filters。
- 目前已套用條件摘要。
- 清除條件入口。
- 結果數或無結果訊息。
- 每張 active card 的 metadata 摘要、收藏狀態與排除抽卡狀態。
- 已排除 card 的 badge/文字狀態，且不得只依賴顏色。
- 每張 active card 的 target-state 收藏/排除操作入口。

**搜尋規則**:

- keyword、meal type、metadata filters、favorite filter 與 draw eligibility filter 採交集。
- 預設列表與搜尋結果包含所有 active cards，包含被排除抽卡 cards。
- `DrawableOnly` 排除 `IsExcludedFromDraw == true` 的 cards。
- `ExcludedOnly` 只顯示 `IsExcludedFromDraw == true` 的 active cards。
- deleted cards 不得出現在卡牌庫一般篩選結果。

## POST `/Cards?handler=Preference`

**目的**: 從卡牌庫列表更新某張 active card 的收藏或排除狀態，並回到目前篩選結果。

**表單欄位**:

| 欄位 | 必填 | 規則 |
|------|------|------|
| `cardId` | 是 | 目標 card ID；必須是 active card。 |
| `targetIsFavorite` | 條件 | 有值時將收藏狀態設為該 bool。 |
| `targetIsExcludedFromDraw` | 條件 | 有值時將排除抽卡狀態設為該 bool。 |
| current query fields | 否 | 用於 redirect 回目前列表狀態。 |

**必須行為**:

- 驗證 Anti-Forgery token。
- 以 target-state mutation 更新 preferences。
- 成功/失敗訊息依目前語系呈現。
- 保留目前 query/filter state。
- 更新不得修改 card content、metadata、drawHistory、statistics 或 deleted state。

## Card Details 契約

**必須呈現**:

- 既有雙語 card content、餐別、metadata 與狀態。
- 收藏狀態與排除抽卡狀態。
- target-state 收藏/取消收藏與排除/取消排除操作。
- 回卡牌庫、編輯、刪除等既有 action。

**禁止**:

- 不得把排除抽卡顯示為刪除。
- 不得讓收藏或排除操作需要使用者重新提交完整 edit form。

## POST `/Cards/Details?handler=Preference` 或等效 details preference handler

**目的**: 從詳情頁更新某張 active card 的收藏或排除狀態。

**規則**:

- 與 `/Cards?handler=Preference` 相同的 Anti-Forgery、target-state、validation、blocked/write failure 規則。
- 成功後返回同一 card details，顯示最新 preference state。
- 若 card 於提交前已 deleted 或不存在，顯示目前語系 not-found/recovery message。

## Card Create/Edit/Delete 契約

**Create**:

- 新增 card 預設 `IsFavorite = false`、`IsExcludedFromDraw = false`。
- create form 不必提供偏好欄位；偏好可在列表、詳情或結果區管理。

**Edit**:

- 編輯 card content、meal type 或 metadata 不得重置既有 preferences。
- duplicate detection 不包含 preferences。
- 若 edit target 已 deleted 或不存在，維持既有 not-found/recovery 行為。

**Delete**:

- 刪除收藏或排除 card 時，刪除後歷史保留與統計規則沿用 004/006。
- 若 hard delete，preferences 隨 card 移除。
- 若 retained as deleted，preferences 可保留作歷史資料，但不得讓 deleted card 回到一般卡牌庫或候選池。
- delete POST 必須 Anti-Forgery。

## 結果、統計與輪替契約

- 偏好更新不得新增、刪除或修改 `DrawHistoryRecord`。
- 偏好更新不得改變總成功抽卡次數、單卡抽中次數、歷史機率或 statistics rows。
- 既有成功結果重顯時，card 後來被收藏或排除只改變目前 preference badge，不改變原 result 身分。
- replay 使用原成功 history 與原 rotation snapshot；不得因 card 目前被排除而 replay 失敗。
- 近期輪替排除集合仍依成功 history card ID 計算；已被手動排除的 card 在建立本次 base pool 前已被移除。

## 語系、主題與狀態保留契約

- `zh-TW` 與 `en-US` 都必須有收藏/排除 labels、button text、badges、filters、success messages、error messages、empty-after-preference message 與 result action message。
- 語系切換後，卡牌庫 preference filter query state 應保留。
- 語系切換後，已揭示成功結果的 card ID、history、statistics、rotation snapshot 與 preference state 不變，只重新投影 display text。
- 主題切換不得清除 preference filters、operation ID、result state 或 preferences。
- 不得顯示未翻譯 resource key。

## Reduced Motion 與動畫契約

- `prefers-reduced-motion: reduce` 時不得播放連續旋轉或長時間動畫。
- reduced motion 仍必須揭示有效靜態結果或可理解空候選池提示。
- 動畫開始、結束、延遲、frame count、CSS class 與 preference UI transition 不得決定 card ID、preferences 或 candidate pool。
- JS 可禁用按鈕改善快速連點 UX，但 server target-state mutation 才是資料完整性來源。

## CSS 與 Responsive 契約

**檔案**: `CardPicker2/wwwroot/css/site.css`

**必須覆蓋**:

- 卡牌庫 preference filter panel。
- 收藏/排除 badge。
- 列表與詳情頁 preference controls。
- 首頁 result preference controls。
- preference empty-state prompt。
- 與既有 metadata filter panel、rotation summary、result card 的 spacing。

**要求**:

- 390x844、768x1024、1366x768 下 `zh-TW` 與 `en-US` 都不得水平溢出。
- Button text、label、number input、badge、summary chips、filter groups、result card 與 preference controls 不得重疊。
- Focus state 在 light/dark theme 與兩語系下都可見。
- 不得只用顏色傳達收藏、排除、可抽、empty reason 或 validation state。

## JavaScript Progressive Enhancement 契約

**檔案**: `CardPicker2/wwwroot/js/site.js`

**用途**:

- 切換語系或主題前保存 transient preference filter UI state。
- redirect/re-render 後還原尚未送出的 preference filters、metadata filters、rotation controls 與 result state。
- 對 target-state buttons 提供 UX guard，例如防止連續點擊；server validation 仍為唯一資料完整性來源。

**限制**:

- JS 不得決定 preferences、候選池、近期排除集合、抽卡結果或 rotation snapshot。
- 不得把完整餐點資料、完整 JSON、秘密值、系統提示或 stack trace 存入 browser storage。
- Transient state 必須與 path/form scope 綁定，成功還原後清除。

## 安全與標頭契約

- 所有 state-changing forms 包含 Anti-Forgery token。
- Production 環境必須保留 HTTPS redirection、HSTS 與 CSP。
- 本功能不得新增外部 script/style/font/image 來源。
- Razor HTML encoding 必須保留；任何 card/tag/preference summary 文字以 encoded text 輸出。
- UI、日誌與 validation message 不得輸出完整 JSON、stack trace、系統提示、秘密值或未清理輸入。

## 日誌契約

**應記錄**:

- schema v5 load/migration status。
- preference update：card ID、target properties、result status。
- invalid preference target 類型，不含原始未清理 payload。
- draw candidate pool preference exclusion count。
- empty after preference exclusion。
- result preference action success/failure。
- draw success：mode、card ID、operation ID、final pool size、preference excluded count。
- replay：operation ID、card ID、snapshot 是否存在、current preference state。
- write failure 與 blocked library。

**不得記錄**:

- 完整 `cards.json`。
- 使用者輸入的完整描述、tag list 原文或完整 metadata/preference payload。
- 秘密值、連線字串、系統提示、stack trace 直接暴露到 UI。

## 測試契約

自動化測試至少必須驗證：

- schema v4 讀取後可映射為 v5 且舊卡 preferences 缺漏不 blocked。
- 新卡與舊資料預設未收藏且未排除。
- 偏好 target-state mutation 成功後重啟仍保留。
- 同一 target-state 重複提交不反轉。
- 偏好 mutation 對 missing/deleted/blocked/write failure 不局部更新。
- 收藏不影響 candidate pool、history、statistics、rotation snapshot 或 duplicate detection。
- 排除抽卡在 metadata filters 與 rotation 前生效。
- normal/random/metadata/rotation draw 永不抽出被排除 card。
- 排除全部符合條件 cards 時不新增 history、不改變 statistics，並顯示偏好排除造成的可修正提示。
- 取消排除後 card 可在符合全部規則時回到候選池。
- `/Cards` 預設顯示已排除 active cards。
- favorite filter、draw eligibility filter 與 keyword/meal/metadata filters 採交集。
- 詳情頁與結果區 preference actions 顯示/更新狀態且保留頁面上下文。
- result preference action 不重新抽卡、不新增 history、不改變 statistics。
- 語系/主題/reduced motion/動畫不改變 preferences、candidate pool facts、card ID、snapshot 或 statistics。
- production CSP/HSTS 與 Anti-Forgery 測試仍通過。
- 390x844、768x1024、1366x768 兩語系無水平溢出或重疊，並符合 WCAG 2.1 AA smoke 檢查。
