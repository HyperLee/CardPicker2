# UI 契約: 抽卡模式與機率統計

## 範圍

本功能不暴露外部 JSON API。公開介面是 Razor Pages 輸出的 HTML、首頁抽卡表單、hidden operation id、query string、card management forms、統計表、ASP.NET Core culture cookie、production 安全標頭與使用者可見文字。所有 state-changing forms 必須包含 ASP.NET Core Anti-Forgery token。

## 全域規則

- 所有使用者可見文案依目前 runtime 語系呈現；未選擇語系、語系偏好無效或不可用時使用 `zh-TW`。
- 正常模式與隨機模式都必須維持等機率候選池規則。
- 語系切換、主題切換、動畫時間、顯示排序、statistics row ordering 與 reduced motion 不得改變抽卡結果、候選池、歷史紀錄或統計分母。
- card library blocked、JSON corrupted/unreadable/unsupported、寫入失敗或 validation failure 時，不得顯示成功抽卡結果，也不得新增成功歷史。
- 錯誤訊息、HTML、console 診斷與日誌不得包含秘密值、連線字串、完整 JSON、stack trace、系統提示或未清理的使用者輸入。

## GET `/` 首頁契約

**必須呈現**:

- shared layout 語系切換與既有導覽。
- 模式選擇控制項，至少包含「正常模式」與「隨機模式」。
- 正常模式下的餐別選擇，包含早餐、午餐、晚餐。
- 隨機模式下可清楚看出不需選餐別；若餐別欄位仍在畫面上，必須 disabled、隱藏或以文字/輔助描述表明不會套用。
- coin-in 或等效確認控制項。
- lever/start action。
- slot-machine visual area、狀態文字與 revealed result area。
- hidden `DrawOperationId`，每個新抽卡操作使用一個非空 Guid。
- 本網站總成功抽取次數。
- 卡牌統計區或無歷史空狀態。

**統計區規則**:

- `TotalSuccessfulDraws == 0` 時顯示可理解空狀態，不顯示每張卡牌 0% 作為歷史分布。
- `TotalSuccessfulDraws > 0` 時顯示統計表。
- 統計表每列至少包含卡牌名稱、餐別、抽中次數、歷史機率與卡牌狀態。
- active cards 必須出現在統計表；active 但尚未抽中的卡牌顯示 0 次與 0%。
- 曾成功抽中且已刪除的 cards 必須出現在統計表並標示已刪除。
- deleted status 不得只依賴顏色；需有文字或可辨識 badge。

**blocked state**:

- card library blocked 時禁用 draw action。
- 統計區顯示目前語系的 recovery message。
- create/edit/delete/draw 操作不得可提交成功。

## POST `/?handler=Draw`

**目的**: 驗證模式、餐別、coin state、operation id 與 card library 狀態後，完成一次 idempotent 抽卡。

**表單欄位**:

| 欄位 | 必填 | 規則 |
|------|------|------|
| `drawMode` | 是 | `Normal` 或 `Random`。 |
| `mealType` | Normal 是；Random 否 | Normal 必須是 `Breakfast`、`Lunch`、`Dinner`；Random 不得限制候選池。 |
| `coinInserted` | 是 | 必須為 true。 |
| `drawOperationId` | 是 | 非空 Guid；用於 replay 同一次成功操作。 |
| `resultCardId` | 否 | 只用於重新 render 或 progressive enhancement；不得作為新抽卡結果來源。 |

**必須行為**:

- 驗證 Anti-Forgery token。
- 驗證 `drawOperationId` 非空且格式有效。
- 若 `drawOperationId` 已有成功歷史，重顯原成功結果，`IsReplay = true`，不得新增歷史。
- Normal mode 必須驗證餐別，並只從該餐別 active cards 抽取。
- Random mode 不要求餐別，並從全部 active cards 抽取。
- 候選池包含 N 張 cards 時，每張標稱抽中機率為 1/N。
- 首次成功時，結果與一筆 `DrawHistoryRecord` 必須同時寫入。
- 寫入成功後顯示 result card、mode、meal type、localized name/description 與更新後統計。

**失敗行為**:

| 條件 | 回應 |
|------|------|
| Anti-Forgery 驗證失敗 | framework 預設 400；不得新增 history。 |
| invalid/missing mode | 顯示目前語系 validation message；不得新增 history。 |
| Normal missing/invalid meal type | 顯示目前語系 validation message；不得新增 history。 |
| missing coin confirmation | 顯示 coin required message；不得新增 history。 |
| empty candidate pool | 顯示空卡池 message；不得新增 history。 |
| card library blocked | 顯示 recovery message；不得新增 history。 |
| write failure | 顯示暫時無法完成抽卡 message；不得宣告成功。 |

**禁止**:

- 不得因 repeated click 建立多筆 history。
- 不得以統計、顯示排序、語系或動畫結果加權抽卡。
- 不得在 GET render 或語系切換時重新呼叫 randomizer。

## 首頁模式互動契約

- 預設 mode 可為 Normal，以維持既有操作習慣。
- 切換到 Random 時，既有 meal type state 不得進入 candidate pool。
- 切回 Normal 時，若尚未選餐別，start action 應透過 server validation 拒絕；client 可提前顯示提示，但不得取代 server validation。
- 模式控制項必須可鍵盤操作，且目前選取模式不只依賴顏色。
- 模式名稱與說明不得暗示 Random 比 Normal 更幸運或更可能抽到特定卡牌。

## GET `/` result restore 契約

**query/hidden state**:

| 欄位 | 規則 |
|------|------|
| `resultCardId` | 可用於語系切換或 redirect 後重顯同一結果。 |
| `drawOperationId` | 若同時存在成功歷史，優先依 operation id replay。 |
| `drawMode` | 用於顯示原 mode。 |
| `mealType` | Normal 結果的原 requested meal type；Random 顯示抽中卡牌餐別。 |

**必須行為**:

- 重新 render 結果時不得重新抽卡。
- 若 card 已被 deleted，但結果對應既有成功 history，仍可顯示該結果並標示狀態。
- 若 card 不存在或 history 無法驗證，顯示可復原訊息，不得抽替代卡。

## 卡牌統計表契約

**欄位**:

| 欄位 | 規則 |
|------|------|
| 卡牌名稱 | 依目前語系投影；英文缺漏可沿用既有 fallback 規則。 |
| 餐別 | 依目前語系顯示。 |
| 抽中次數 | 非負整數。 |
| 歷史機率 | `抽中次數 / 總成功抽卡次數`，以百分比顯示；無歷史時不顯示誤導性 0%。 |
| 卡牌狀態 | Active 或 Deleted，使用目前語系文字/badge。 |

**計算規則**:

- 分母是全部成功抽卡次數，不是餐別內次數。
- 驗證失敗、空卡池、資料封鎖、未投幣、寫入失敗與 replay 不增加分母。
- 卡牌改名或翻譯更新後，歷史統計仍依 card ID 歸屬同一列。
- 語系切換只改變 display text，不改變 row identity、draw count 或 probability。

## `/Cards` 卡牌管理契約調整

**Browsing/Search**:

- 一般卡牌庫列表與搜尋預設只顯示 active cards。
- deleted cards 不得出現在未來抽卡候選池。
- 搜尋仍依目前語系 visible active card name 比對。

**Create/Edit**:

- create/edit duplicate detection 預設只檢查 active cards。
- 編輯 active card 改名或翻譯後，不得切分既有 draw history。
- 若 edit 目標已 deleted 或不存在，顯示 not found/recovery message；不得重新建立為新 active card，除非未來規格另行要求。

**Delete**:

- POST 必須 Anti-Forgery。
- 若 card 沒有成功 draw history，可 hard delete。
- 若 card 有成功 draw history，必須 retained as deleted，不再可抽出，但統計表保留歷史列。
- 刪除成功/失敗訊息依目前語系呈現。
- 刪除失敗不得局部改動 JSON。

## Reduced Motion 與動畫契約

- `prefers-reduced-motion: reduce` 時不得播放連續旋轉或長時間動畫。
- reduced motion 仍必須揭示有效靜態結果。
- 動畫開始、結束、延遲、frame count 與 CSS class 不得決定 card ID。
- JS 可禁用按鈕避免 UX 上的快速連點，但 server idempotency 才是資料完整性來源。

## 安全與標頭契約

- 所有 state-changing forms 包含 Anti-Forgery token。
- Production 環境必須保留 HTTPS redirection、HSTS 與 CSP。
- CSP 不得為本功能新增不必要的外部 script/style 來源。
- 統計、抽卡與復原訊息不得輸出完整 JSON、stack trace、系統提示、秘密值或未清理輸入。

## 日誌契約

**應記錄**:

- card library load/migration status。
- draw validation failure 類型，不含未清理使用者輸入。
- draw success：mode、card ID、pool size、operation ID。
- repeat replay：operation ID、card ID。
- empty pool、blocked library、write failure。
- delete hard delete 或 retained deleted。

**不得記錄**:

- 完整 `cards.json`。
- 使用者輸入的完整描述內容。
- 秘密值、連線字串、系統提示、stack trace 直接暴露到 UI。
