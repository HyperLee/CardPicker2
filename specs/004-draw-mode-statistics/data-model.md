# 資料模型: 抽卡模式與機率統計

## 概觀

本功能擴充既有雙語餐點卡牌模型，加入抽卡模式、成功抽卡歷史、卡牌狀態與統計投影。持久化來源仍是單一 JSON 檔 `CardPicker2/data/cards.json`。使用者可見統計不以 aggregate counter 儲存，而是由成功抽卡歷史與目前卡牌集合計算。

模型名稱可使用英文識別字；使用者可見名稱、餐別、狀態、錯誤與統計文字必須依目前 runtime 語系呈現。

## Entity: DrawMode

代表一次抽卡使用的候選池規則。

| 值 | 使用者語意 | 規則 |
|----|------------|------|
| `Normal` | 正常模式 | 必須選擇有效 `MealType`；候選池只包含該餐別 active cards。 |
| `Random` | 隨機模式 | 不要求 `MealType`；候選池包含早餐、午餐、晚餐全部 active cards。 |

**驗證規則**:

- 缺失、unsupported 或無法 bind 的 mode 必須拒絕抽卡並顯示可修正訊息。
- `Random` mode 必須忽略畫面上殘留或 query string 中的餐別值。
- mode 只影響候選池，不得影響 randomizer 權重。

## Entity: DrawOperation

代表一次使用者提交抽卡的操作輸入。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `OperationId` | Guid | 是 | 首頁 GET 產生並 POST 回來；不可為 `Guid.Empty`；同一次成功操作重送時必須相同。 |
| `Mode` | DrawMode | 是 | `Normal` 或 `Random`。 |
| `MealType` | MealType? | Normal 是；Random 否 | Normal 必須是 `Breakfast`、`Lunch`、`Dinner`；Random 不使用此值。 |
| `CoinInserted` | bool | 是 | 必須為 true 才可嘗試抽卡。 |
| `RequestedCulture` | SupportedLanguage | 是 | 只影響結果投影語言，不影響候選池或統計。 |

**狀態規則**:

```text
Idle -> ModeSelected -> CoinInserted -> Spinning -> Revealed
任一驗證失敗 -> Blocked
重複提交已成功 OperationId -> Revealed(IsReplay = true)
```

**驗證規則**:

- `OperationId` 無效、未投幣、mode 無效、Normal 未選餐別、card library blocked 或候選池為空時，不得新增歷史。
- `OperationId` 不是安全秘密；Anti-Forgery token 仍是 state-changing POST 的必要保護。

## Entity: DrawHistoryRecord

代表一次成功抽卡的持久事實。只有成功抽卡可建立此紀錄。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Id` | Guid | 是 | 系統產生且不可變；不可為 `Guid.Empty`。 |
| `OperationId` | Guid | 是 | 對成功歷史唯一；同一 operation 重送時用於 replay。 |
| `DrawMode` | DrawMode | 是 | 成功時使用的 mode。 |
| `CardId` | Guid | 是 | 指向同一不可變 `MealCard.Id`。 |
| `MealTypeAtDraw` | MealType | 是 | 抽中當下的卡牌餐別。 |
| `SucceededAtUtc` | DateTimeOffset | 是 | 成功歷史成立時間；使用 UTC。 |

**不變條件**:

- 每個 `OperationId` 最多只能對應一筆成功歷史。
- 每筆紀錄必須指向 document 中存在的 active 或 retained deleted card。
- 未成功嘗試、validation failure、空卡池、資料封鎖、寫入失敗與同一次成功操作 replay 都不得新增紀錄。
- 抽卡結果與 `DrawHistoryRecord` 必須在同一次原子寫入中一起成立；寫入失敗時整次抽卡視為失敗。

## Entity: CardStatus

代表卡牌是否能進入未來抽卡池。

| 值 | 規則 |
|----|------|
| `Active` | 可在卡牌庫顯示、搜尋、編輯、刪除，並可進入候選池。 |
| `Deleted` | 不可進入候選池；不出現在一般卡牌庫 active list；若曾成功抽中，統計表保留歷史列。 |

**狀態轉換**:

```text
Active --Delete without successful history--> removed from cards
Active --Delete with successful history--> Deleted
Deleted --future draw/search/edit--> excluded
```

**驗證規則**:

- `Deleted` card 必須保留 card ID、meal type 與 localizations，供統計列顯示。
- 未曾成功抽中的 deleted card 不應只為統計表保留。
- Duplicate detection 與 create/edit 應只比對 active cards；deleted card 不得阻擋新增同名新卡，除非未來規格另行要求。

## Entity: MealCard

延續既有雙語卡牌模型，並新增生命週期狀態。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Id` | Guid | 是 | 系統產生且不可變；不可為 `Guid.Empty`。 |
| `MealType` | MealType | 是 | `Breakfast`、`Lunch`、`Dinner`。 |
| `Localizations` | map | 是 | key 僅允許 `zh-TW`、`en-US`；active card 必須有完整必要內容。 |
| `Status` | CardStatus | 是 | 預設 `Active`。 |
| `DeletedAtUtc` | DateTimeOffset? | 否 | `Status = Deleted` 時必須有值；active 時必須為 null。 |

**候選池規則**:

- 只有 `Status = Active` 且資料驗證通過的卡牌可進入 normal/random candidate pool。
- 語系、歷史抽中次數、顯示排序、狀態 badge 與動畫不得加權 active cards。

## Entity: CardLibraryDocument

代表 `CardPicker2/data/cards.json` 的根文件。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `SchemaVersion` | int | 是 | v3 值為 `3`。 |
| `Cards` | IReadOnlyList<MealCard> | 是 | 可空；seed 建立時每餐別至少 3 張 active cards。 |
| `DrawHistory` | IReadOnlyList<DrawHistoryRecord> | 是 | 只包含成功抽卡歷史；可空。 |

**schema v3 JSON 範例**:

```json
{
  "schemaVersion": 3,
  "cards": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "mealType": "Lunch",
      "status": "Active",
      "deletedAtUtc": null,
      "localizations": {
        "zh-TW": {
          "name": "牛肉麵",
          "description": "紅燒牛肉麵搭配青菜。"
        },
        "en-US": {
          "name": "Beef Noodle Soup",
          "description": "Braised beef noodle soup with greens."
        }
      }
    }
  ],
  "drawHistory": [
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "operationId": "33333333-3333-3333-3333-333333333333",
      "drawMode": "Normal",
      "cardId": "11111111-1111-1111-1111-111111111111",
      "mealTypeAtDraw": "Lunch",
      "succeededAtUtc": "2026-05-13T12:00:00+00:00"
    }
  ]
}
```

**migration 規則**:

```text
schema v1 -> 以既有 name/description 建立 zh-TW localization；en-US 缺漏；status = Active；drawHistory = []
schema v2 -> 保留 localizations；status = Active；drawHistory = []
schema v3 -> 原樣讀取並完整驗證
unsupported schema -> block operations, preserve original file
corrupted/unreadable JSON -> block operations, preserve original file
```

**文件驗證規則**:

- `DrawHistory.OperationId` 不可重複。
- 每筆 history 的 `CardId` 必須存在於 `Cards`。
- `Cards` 中 ID 不可重複。
- `DeletedAtUtc` 與 `Status` 必須一致。
- `SchemaVersion = 3` 的 active cards 必須有完整雙語內容。
- 寫入前必須驗證整份 document；失敗不得覆寫原檔。

## Entity: CandidatePool

代表一次抽卡可被等機率選取的卡牌集合。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Mode` | DrawMode | 是 | Normal 或 Random。 |
| `SelectedMealType` | MealType? | Normal 是；Random 否 | Normal 使用餐別；Random 為 null 或 ignored。 |
| `Cards` | IReadOnlyList<MealCard> | 是 | 只包含 active cards。 |
| `NominalProbability` | decimal | Cards 非空時是 | 每張候選卡為 `1 / Cards.Count`。 |

**建構規則**:

- Normal: `Cards = activeCards.Where(card.MealType == SelectedMealType)`。
- Random: `Cards = activeCards`。
- `Cards.Count == 0` 時不得呼叫 randomizer，回傳空卡池 failure。

## Entity: DrawResult

代表一次抽卡 POST 或 replay 的使用者可見結果。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Succeeded` | bool | 是 | 只有 history 寫入成功或 replay 既有成功 history 時為 true。 |
| `OperationId` | Guid | 是 | 對應 submitted operation。 |
| `DrawMode` | DrawMode | 是 | 成功或失敗都保留 submitted mode。 |
| `RequestedMealType` | MealType? | 否 | Normal 成功/失敗保留 submitted meal type；Random 可為 null。 |
| `CardId` | Guid? | 成功時是 | 成功或 replay 原 card ID。 |
| `LocalizedCard` | LocalizedMealCardView? | 成功時是 | 依目前語系投影。 |
| `IsReplay` | bool | 是 | 同一 `OperationId` 已成功時 replay 原結果。 |
| `StatusKey` | string | 是 | stable localized message key。 |

**規則**:

- `Succeeded = true` 時必須能對應一筆 `DrawHistoryRecord`。
- `IsReplay = true` 時不得新增 history，不得重新 randomize。
- 語系切換後以 `CardId` 重新 render 時不得抽另一張替代卡。

## Entity: CardDrawStatistic

代表首頁統計表的一列。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `CardId` | Guid | 是 | 卡牌不可變 ID。 |
| `DisplayName` | string | 是 | 依目前語系投影；deleted card 使用保留 localizations。 |
| `MealTypeDisplayName` | string | 是 | 依目前語系顯示餐別。 |
| `CardStatus` | CardStatus | 是 | Active 或 Deleted。 |
| `DrawCount` | int | 是 | 成功歷史中該 card ID 的筆數。 |
| `HistoricalProbability` | decimal? | 否 | 有成功歷史時為 `DrawCount / TotalSuccessfulDraws`；無成功歷史時為 null。 |
| `HistoricalProbabilityDisplay` | string | 是 | 有成功歷史時顯示百分比；無成功歷史時顯示空狀態文案或不顯示。 |

**列集合規則**:

- `TotalSuccessfulDraws == 0`: 顯示可理解空狀態，不顯示每卡 0% 作為歷史分布。
- `TotalSuccessfulDraws > 0`: 包含所有 active cards，以及曾成功抽中的 deleted cards。
- active 但尚未抽中的卡牌顯示 `DrawCount = 0` 與 `HistoricalProbability = 0%`。
- deleted 但未曾成功抽中的卡牌不列入統計。

## Entity: DrawStatisticsSummary

代表首頁統計區整體資料。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `TotalSuccessfulDraws` | int | 是 | `DrawHistory.Count`。 |
| `Rows` | IReadOnlyList<CardDrawStatistic> | 是 | 依列集合規則產生。 |
| `HasHistory` | bool | 是 | `TotalSuccessfulDraws > 0`。 |
| `StatusKey` | string | 是 | 統計區狀態或空狀態 resource key。 |

**排序建議**:

1. Active cards 在前，Deleted cards 在後。
2. 同狀態內依餐別、目前語系顯示名稱排序。
3. 排序只影響呈現，不得影響抽卡結果。

## 關係

```text
CardLibraryDocument 1 -- * MealCard
CardLibraryDocument 1 -- * DrawHistoryRecord
DrawHistoryRecord * -- 1 MealCard (by CardId)
DrawStatisticsSummary 1 -- * CardDrawStatistic
CardDrawStatistic * -- 1 MealCard (by CardId)
DrawOperation 1 -- 0..1 DrawHistoryRecord (by OperationId, only on success)
```

## 資料完整性規則

- Normal mode 的成功結果必須來自 submitted meal type 的 active candidate pool。
- Random mode 的成功結果必須來自全部 active candidate pool，且不受 meal type 欄位限制。
- 每次首次成功 draw operation 必須剛好 append 一筆 history。
- history append 與 draw result 成功必須同一 atomic write。
- write failure、blocked library、validation failure、empty pool、missing coin 與 invalid mode/meal type 不得新增 history。
- 重複提交已成功 operation 只能 replay 原 history。
- 語系切換、動畫、排序、統計、deleted badge 與鼓勵文案不得改變 candidate pool、randomizer 或 history。
