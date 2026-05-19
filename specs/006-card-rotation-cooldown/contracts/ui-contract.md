# UI 契約: 餐點輪替防重複抽卡

## 範圍

本功能不暴露外部 JSON API。公開介面是 Razor Pages 輸出的 HTML、首頁抽卡表單、hidden operation id、query/form state、ASP.NET Core culture cookie、production 安全標頭、使用者可見文字與前端互動狀態。所有 state-changing forms 必須包含 ASP.NET Core Anti-Forgery token。

## 全域規則

- 所有使用者可見文案依目前 runtime 語系呈現；未選擇、無效或不可用時使用 `zh-TW`。
- 防重複只在既有 005 base + metadata candidate pool 後套用。
- 防重複只能縮小候選池，不得改變剩餘候選池內等機率 `1/N` 規則。
- `avoidRecentRepeats = false` 或 `recentDrawCount = 0` 時，候選池必須完全回到既有 005 規則。
- 語系切換、主題切換、動畫、reduced motion、顯示排序、統計列排序、card edit/delete 與翻譯更新不得改變已成立抽卡結果、rotation snapshot、history 或 statistics。
- blocked library、JSON corrupted/unreadable/unsupported、invalid cooldown settings、空候選池或寫入失敗時，不得顯示成功抽卡結果，也不得新增成功 history。
- 錯誤訊息、HTML、console 診斷與日誌不得包含秘密值、連線字串、完整 JSON、stack trace、系統提示或未清理使用者輸入。

## GET `/` 首頁契約

**必須呈現**:

- shared layout 語系切換、主題切換與既有導覽。
- 正常模式/隨機模式選擇。
- 正常模式的餐別選擇；隨機模式下餐別不得限制候選池。
- 005 決策資訊篩選控制與目前條件摘要。
- 「避免最近重複」控制，預設啟用。
- 最近成功抽卡排除次數 N 控制，預設值為 `3`，有效範圍提示為 `0..10`。
- 防重複控制與 N 值不得作為跨頁面或跨重新啟動的使用者偏好持久保存；新的首頁 GET 必須回到預設啟用與 N=3，除非目前 request/form state 明確帶入值。
- coin-in 或等效確認控制項。
- lever/start action。
- slot-machine visual area、狀態文字與 revealed result area。
- hidden `DrawOperationId`。
- 既有總成功抽取次數與統計表。

**防重複控制規則**:

- 控制項必須可用鍵盤、滑鼠與觸控操作。
- 啟用/停用狀態不得只依賴顏色傳達。
- N 值可用 number input、stepper 或 equivalent control；server validation 是最終來源。
- N=0 時 UI 可顯示為「不排除近期紀錄」或等效說明，但不得暗示提高機率。
- 防重複摘要在手機寬度不得水平溢出，長文字必須換行。

**blocked state**:

- card library blocked 時禁用 draw action。
- 防重複控制可顯示但不得提交成功 draw。
- 統計區顯示目前語系 recovery message。

## POST `/?handler=Draw`

**目的**: 驗證模式、餐別、coin state、operation id、metadata filters、防重複設定與 card library 狀態後，完成一次 idempotent metadata + rotation filtered draw。

**表單欄位**:

| 欄位 | 必填 | 規則 |
|------|------|------|
| `drawMode` | 是 | `Normal` 或 `Random`。 |
| `mealType` | Normal 是；Random 否 | Normal 必須是 `Breakfast`、`Lunch`、`Dinner`；Random 不得限制候選池。 |
| `coinInserted` | 是 | 必須為 true。 |
| `drawOperationId` | 是 | 非空 Guid；用於 replay 同一次成功操作。 |
| 005 metadata filter fields | 否 | 維持 005 validation 與交集規則。 |
| `avoidRecentRepeats` | 否 | 缺漏時採預設啟用；提交值必須可 bind 為 bool。 |
| `recentDrawCount` | 否 | 缺漏時預設 `3`；有效整數範圍 `0..10`。 |

**必須行為**:

- 驗證 Anti-Forgery token。
- 驗證 `drawOperationId` 非空且格式有效。
- 若 `drawOperationId` 已有成功 history，重顯原成功結果與原 rotation snapshot，`IsReplay = true`，不得重新套用目前畫面上的 N、filters 或最新 history。
- Normal mode 必須先依餐別建立 active base pool，再套用 metadata filters。
- Random mode 不要求餐別，從全部 active cards 建立 base pool，再套用 metadata filters。
- 若防重複啟用且 N > 0，讀取最近 N 筆成功 history，依 card ID 去重形成排除集合，再從 005 pool 移除同時存在的 active cards。
- 近期排除後候選池包含 M 張 cards 時，每張標稱抽中機率為 `1/M`。
- 首次成功時，結果、成功 history 與 `RotationSnapshot` 必須同時寫入。
- 成功結果顯示抽中卡牌的餐別、名稱、描述、metadata 摘要與輪替摘要。
- 輪替摘要至少包含是否啟用防重複、N 值、排除數與輪替後候選池大小。
- 本版輪替摘要採僅顯示數量的呈現；不得列出被排除卡牌名稱。

**失敗行為**:

| 條件 | 回應 |
|------|------|
| Anti-Forgery 驗證失敗 | framework 預設 400；不得新增 history。 |
| invalid/missing mode | 顯示目前語系 validation message；不得新增 history。 |
| Normal missing/invalid meal type | 顯示目前語系 validation message；不得新增 history。 |
| missing coin confirmation | 顯示 coin required message；不得新增 history。 |
| invalid metadata filter value | 顯示目前語系 validation message；不得新增 history。 |
| invalid `recentDrawCount` | 顯示目前語系可修正訊息；不得新增 history。 |
| 005 base/metadata candidate pool empty | 顯示既有空卡池或無符合條件訊息；不得新增 history 或改變 statistics。 |
| 005 pool 非空但 rotation pool empty | 顯示防重複造成空候選池訊息，提示降低 N、關閉防重複或調整條件；不得新增 history 或改變 statistics。 |
| card library blocked | 顯示 recovery message；不得新增 history。 |
| write failure | 顯示暫時無法完成抽卡 message；不得宣告成功。 |

**禁止**:

- 不得自動降低 N。
- 不得自動關閉防重複。
- 不得抽出近期排除卡牌作為 fallback。
- 不得宣稱關閉/開啟防重複會提高幸運、提高機率、觸發保底或偏好加權。

## GET `/` result restore 契約

**query/hidden state**:

| 欄位 | 規則 |
|------|------|
| `resultCardId` | 可用於語系切換或 redirect 後重顯同一結果。 |
| `drawOperationId` | 若同時存在成功 history，優先依 operation id replay。 |
| `drawMode` | 用於顯示原 mode。 |
| `mealType` | Normal 結果的原 requested meal type；Random 顯示抽中卡牌餐別。 |
| metadata filter fields | 可用於顯示原提交條件摘要；不得觸發重新抽卡。 |
| `avoidRecentRepeats` / `recentDrawCount` | 可用於顯示原提交或目前表單狀態；成功 replay 摘要以 history snapshot 為準。 |

**必須行為**:

- 重新 render 結果時不得重新抽卡。
- 若 history 有 rotation snapshot，摘要使用 snapshot 的 counts。
- 若 history 缺少 rotation snapshot，顯示目前語系「此筆舊紀錄未保存輪替摘要」或等效狀態，不得回填或重算。
- 若 card 已 deleted，但 result 對應既有成功 history，仍可顯示該結果並標示狀態。
- 語系切換只重新投影 display text，不改變 card ID、snapshot、history 或 statistics。

## 結果與空候選池摘要契約

**成功摘要必須包含**:

- 是否套用避免最近重複。
- N 值。
- 輪替前候選池大小。
- 因近期排除移除的候選卡牌數。
- 輪替後候選池大小。
- 本版不得顯示被排除卡牌名稱；若未來要顯示名稱，必須另開規格定義 deleted card、語系與隱私呈現規則。

**空候選池提示**:

- 005 pool 為空時，沿用既有「目前沒有符合條件的餐點」或空卡池訊息。
- rotation 後為空時，明確指出防重複排除了所有目前符合條件的餐點，並提供下一步：降低 N、關閉避免最近重複或調整其他條件。
- 不得列出 deleted card 作為可抽候選。
- 不得列出被排除卡牌名稱；只顯示被排除數量與可修正下一步。

## 語系、主題與狀態保留契約

- `zh-TW` 與 `en-US` 都必須有防重複 label、N label、validation message、success summary、empty-after-rotation message 與 old-history-missing-snapshot message。
- 語系切換後，首頁尚未提交的防重複 toggle 與 N 值應盡可能保留。
- 語系切換後，已揭示成功結果的 rotation summary 由 persisted snapshot 顯示，不使用目前表單值。
- 主題切換不得清除防重複設定、metadata filters、operation id 或 result state。
- 上述保留僅限目前頁面互動或表單狀態；防重複設定不得寫入 `cards.json`、server-side user preference、長期 cookie 或跨重新啟動偏好。
- 不得顯示未翻譯 resource key。

## Reduced Motion 與動畫契約

- `prefers-reduced-motion: reduce` 時不得播放連續旋轉或長時間動畫。
- reduced motion 仍必須揭示有效靜態結果或空候選池提示。
- 動畫開始、結束、延遲、frame count、CSS class 與 filter UI transition 不得決定 card ID、近期排除集合或 snapshot。
- JS 可禁用按鈕改善快速連點 UX，但 server idempotency 才是資料完整性來源。

## CSS 與 Responsive 契約

**檔案**: `CardPicker2/wwwroot/css/site.css`

**必須覆蓋**:

- 防重複控制列。
- N 值 input/stepper。
- 輪替摘要 chips 或 description list。
- 防重複造成空候選池提示。
- 與既有 metadata filter panel 的 spacing。

**要求**:

- 390x844、768x1024、1366x768 下 `zh-TW` 與 `en-US` 都不得水平溢出。
- Button text、label、number input、badge、summary chips、filter groups 與 result card 不得重疊。
- Focus state 在 light/dark theme 與兩語系下都可見。
- 不得只用顏色傳達防重複啟用狀態、空候選池原因或 validation state。

## JavaScript Progressive Enhancement 契約

**檔案**: `CardPicker2/wwwroot/js/site.js`

**用途**:

- 切換語系或主題前保存 transient 防重複 UI state。
- redirect/re-render 後還原尚未送出的防重複 toggle、N 值與 metadata filters。
- 對 N 值 input 提供 UX guard，例如 min/max hints；server validation 仍為唯一資料完整性來源。

**限制**:

- JS 不得決定候選池、近期排除集合、抽卡結果或 rotation snapshot。
- 不得把完整餐點資料、完整 JSON、秘密值、系統提示或 stack trace 存入 browser storage。
- Transient state 必須與 path/form scope 綁定，成功還原後清除。

## 安全與標頭契約

- 所有 state-changing forms 包含 Anti-Forgery token。
- Production 環境必須保留 HTTPS redirection、HSTS 與 CSP。
- 本功能不得新增外部 script/style/font/image 來源。
- Razor HTML encoding 必須保留；任何 card/tag/summary 文字以 encoded text 輸出。
- UI、日誌與 validation message 不得輸出完整 JSON、stack trace、系統提示、秘密值或未清理輸入。

## 日誌契約

**應記錄**:

- invalid cooldown setting 類型，不含原始未清理 payload。
- rotation applied：mode、operation ID、N、pre count、excluded count、post count。
- empty base/metadata pool。
- empty after rotation。
- draw success：mode、card ID、operation ID、post-rotation pool size、excluded count。
- replay：operation ID、card ID、snapshot 是否存在。
- write failure 與 blocked library。

**不得記錄**:

- 完整 `cards.json`。
- 使用者輸入的完整描述、tag list 原文或完整 metadata payload。
- 秘密值、連線字串、系統提示、stack trace 直接暴露到 UI。

## 測試契約

自動化測試至少必須驗證：

- 預設啟用防重複且 N=3。
- 新首頁 GET 或應用程式重新啟動後回到預設啟用與 N=3，且不讀取先前本次抽卡設定作為持久偏好。
- N 有效範圍為 0..10；無效 N 拒絕抽卡且不新增 history。
- N=0 或防重複關閉時，候選池與 005 規則一致。
- 正常模式先餐別、再 metadata、再 rotation。
- 隨機模式忽略餐別、套用 metadata、再 rotation。
- 最近 N 筆成功 history 依 `SucceededAtUtc` 新到舊排序，時間相同時持久化順序較後者較新。
- 最近 N 筆中的重複 card ID 只排除一次。
- deleted/missing cards 在 recent exclusion set 中不會被抽出，也不會排除其他候選卡。
- rotation 後 pool 非空時仍維持 `1/M` nominal probability。
- base pool empty 與 rotation empty 顯示不同 message。
- rotation empty 不新增 history、不改變 statistics。
- 成功 history 保存 `RotationSnapshot`，且 replay 使用原 snapshot。
- 缺少 snapshot 的舊 history 不 blocked、不回填、不影響統計與最近 N 次排除。
- 語系/主題/reduced motion/動畫不改變 candidate pool、排除集合、card ID、snapshot 或 statistics。
- production CSP/HSTS 與 Anti-Forgery 測試仍通過。
- 390x844、768x1024、1366x768 兩語系無水平溢出或重疊，並符合 WCAG 2.1 AA smoke 檢查。
