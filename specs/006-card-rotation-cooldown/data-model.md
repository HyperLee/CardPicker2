# 資料模型: 餐點輪替防重複抽卡

## 概觀

本功能在 005 schema v4 的卡牌、metadata filters、成功抽卡歷史與統計模型上新增輪替防重複設定與成功歷史快照。持久化來源仍是單一 JSON 檔 `CardPicker2/data/cards.json`。schema v4 root 保留 `schemaVersion`、`cards` 與 `drawHistory`；每筆成功 `DrawHistoryRecord` 可新增 optional `rotationSnapshot`。缺少快照的既有成功歷史不得被視為 corrupted，也不得被回填推測資料。

輪替防重複只影響未來抽卡候選池，不改變既有統計公式。模型名稱可使用英文識別字；使用者可見欄位、提示、摘要、空狀態與 validation message 必須依目前 runtime 語系呈現。

## Entity: RotationCooldownSettings

代表使用者在首頁本次抽卡提交的防重複設定。本版不跨頁面或重新啟動持久保存。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `AvoidRecentRepeats` | bool | 是 | 預設 `true`；`false` 時不得套用近期排除。 |
| `RecentDrawCount` | int | 是 | 預設 `3`；有效範圍 `0..10`。 |

**衍生規則**:

```text
IsActive = AvoidRecentRepeats == true && RecentDrawCount > 0
```

**驗證規則**:

- `RecentDrawCount < 0`、`RecentDrawCount > 10` 或無法 bind 為 int 時拒絕抽卡。
- `RecentDrawCount = 0` 等同關閉防重複；可保留 `AvoidRecentRepeats = true`，但不得套用近期排除。
- 設定本身不持久化為使用者偏好；成功抽卡只保存該次 `RotationSnapshot`。
- 無效設定不得寫入 history、statistics 或任何 JSON 欄位。

## Entity: RecentSuccessfulDrawRange

代表依目前 document 中成功抽卡歷史取出的最近 N 筆紀錄。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `RequestedCount` | int | 是 | 來源 `RotationCooldownSettings.RecentDrawCount`。 |
| `Records` | IReadOnlyList<DrawHistoryRecord> | 是 | 最多 N 筆，依新到舊排序。 |

**排序規則**:

```text
records =
  drawHistory
    .Select((record, index) => (record, index))
    .OrderByDescending(record.SucceededAtUtc)
    .ThenByDescending(index)
    .Take(N)
```

**驗證規則**:

- 只包含已持久化成功歷史；失敗嘗試、空候選池、資料封鎖、未投幣、write failure 與 replay 不會形成紀錄，因此不在範圍內。
- N 代表最近 N 筆成功紀錄，不是最近 N 張唯一卡牌。

## Entity: RecentExclusionSet

代表最近成功範圍中需要從本次候選池排除的 card ID 集合。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `CardIds` | IReadOnlySet<Guid> | 是 | 從 `RecentSuccessfulDrawRange.Records.CardId` 去重而來。 |

**規則**:

- 去重使用不可變 `Guid` card ID。
- 最近 N 筆中同一 card ID 出現多次時，集合仍只包含一次。
- 集合可包含已刪除或不存在 card ID，但套用時只會移除同時存在於目前候選池內的 active card。
- 不得依名稱、描述、翻譯、metadata、餐別顯示文字或目前語系建立排除關係。

## Entity: RotationCandidatePool

代表套用輪替防重複前後的候選池資訊。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `PreRotationCards` | IReadOnlyList<MealCard> | 是 | 005 base + metadata filters 後的 active candidates。 |
| `PostRotationCards` | IReadOnlyList<MealCard> | 是 | 移除近期排除集合中仍在候選池內的 cards。 |
| `ExcludedCardIds` | IReadOnlySet<Guid> | 是 | 實際從候選池移除的 active card IDs。 |
| `Settings` | RotationCooldownSettings | 是 | 已驗證設定。 |
| `Snapshot` | RotationSnapshot | 是 | 可持久化最小摘要。 |

**建構規則**:

```text
if settings.IsActive == false:
  PostRotationCards = PreRotationCards
  ExcludedCardIds = []
else:
  excluded = PreRotationCards where card.Id in RecentExclusionSet.CardIds
  PostRotationCards = PreRotationCards except excluded by card.Id
```

**不變條件**:

- `PostRotationCards.Count <= PreRotationCards.Count`。
- `ExcludedCardIds.Count == PreRotationCards.Count - PostRotationCards.Count`。
- `PostRotationCards` 內每張卡牌仍為 active 且符合 005 模式、餐別與 metadata filters。
- `PostRotationCards.Count > 0` 時，每張候選卡牌標稱機率為 `1 / PostRotationCards.Count`。

## Entity: RotationSnapshot

代表每筆成功抽卡歷史保存的最小輪替摘要，用於 replay、重新整理、語系切換與重啟後重顯同一次成功結果。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `AvoidRecentRepeats` | bool | 是 | 成功時本次是否勾選避免最近重複。 |
| `RecentDrawCount` | int | 是 | 成功時使用的 N 值，範圍 `0..10`。 |
| `PreRotationCandidateCount` | int | 是 | 005 base + metadata filters 後候選池大小。 |
| `ExcludedCandidateCount` | int | 是 | 實際從候選池移除的 active cards 數量。 |
| `PostRotationCandidateCount` | int | 是 | 最終可抽候選池大小。 |

**驗證規則**:

- 所有 count 必須為非負整數。
- `ExcludedCandidateCount <= PreRotationCandidateCount`。
- `PostRotationCandidateCount == PreRotationCandidateCount - ExcludedCandidateCount`。
- 成功抽卡保存的 snapshot 必須與 history append 同一 atomic write 成立。
- 成功抽卡若無法保存 snapshot，不得宣告成功。
- 舊 history 缺少 snapshot 時合法；顯示時視為「當時未保存輪替摘要」，不得推測補值。

**JSON 範例**:

```json
{
  "id": "22222222-2222-2222-2222-222222222222",
  "operationId": "33333333-3333-3333-3333-333333333333",
  "drawMode": "Normal",
  "cardId": "11111111-1111-1111-1111-111111111111",
  "mealTypeAtDraw": "Lunch",
  "succeededAtUtc": "2026-05-19T12:00:00+00:00",
  "rotationSnapshot": {
    "avoidRecentRepeats": true,
    "recentDrawCount": 3,
    "preRotationCandidateCount": 5,
    "excludedCandidateCount": 2,
    "postRotationCandidateCount": 3
  }
}
```

## Entity: DrawOperation

延伸既有抽卡操作輸入。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| 既有 operation id / mode / meal type / coin / language / filters | mixed | 是 | 維持 004/005 規則。 |
| `RotationCooldown` | RotationCooldownSettings | 否 | 缺漏時使用預設 `AvoidRecentRepeats = true`、`RecentDrawCount = 3`。 |

**規則**:

- validation 順序：operation ID、mode、coin、meal type、metadata filters、rotation settings。
- `DrawOperationId` 已有成功 history 時，直接 replay 原 result/snapshot，不使用目前提交的 rotation settings。
- Random mode 忽略 meal type，但仍套用 metadata filters 與 rotation cooldown。

## Entity: DrawHistoryRecord

延伸既有成功抽卡持久事實。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| 既有 `Id` / `OperationId` / `DrawMode` / `CardId` / `MealTypeAtDraw` / `SucceededAtUtc` | mixed | 是 | 維持 004/005 規則。 |
| `RotationSnapshot` | RotationSnapshot? | 否 | 006 後新成功 history 建議必填；006 前舊 history 可為 null。 |

**驗證規則**:

- `RotationSnapshot = null` 不代表 corrupted。
- 新增成功 history 時應保存 non-null snapshot，即使本次關閉防重複或 N=0，也保存 count 摘要以便 replay。
- `RotationSnapshot` 不改變統計公式；總成功抽卡次數仍為 `drawHistory.Count`。

## Entity: DrawResult

延伸既有抽卡結果。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| 既有 success / card / mode / operation / replay / applied filters | mixed | 是 | 維持 004/005 規則。 |
| `RotationSettings` | RotationCooldownSettings? | 否 | 本次提交或 replay 原設定投影。 |
| `RotationSnapshot` | RotationSnapshot? | 成功時建議 | 成功或 replay 顯示用摘要。 |
| `CandidatePoolEmptyReason` | enum/string? | 失敗時建議 | 區分 base/metadata empty 與 rotation empty。 |

**失敗狀態**:

```text
InvalidRotationSettings -> validation message, no history
BaseCandidatePoolEmpty -> existing empty pool/filter message, no history
RotationCandidatePoolEmpty -> cooldown relaxation message, no history
```

**規則**:

- 成功 result 必須對應一筆 `DrawHistoryRecord` 或 replay 既有 history。
- replay result 的 rotation summary 必須來自 history snapshot；缺 snapshot 時不得重算。
- 語系切換只改變 display text，不改變 card ID、snapshot counts 或 statistics。

## Entity: CardLibraryDocument

沿用 schema v4 root。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `SchemaVersion` | int | 是 | 值維持 `4`。 |
| `Cards` | IReadOnlyList<MealCard> | 是 | 維持 005 metadata 規則。 |
| `DrawHistory` | IReadOnlyList<DrawHistoryRecord> | 是 | 每筆可含 optional `RotationSnapshot`。 |

**migration 規則**:

```text
schema v1 -> 既有單語轉 zh-TW localization；status = Active；drawHistory = []; decisionMetadata = null
schema v2 -> 保留 localizations；status = Active；drawHistory = []; decisionMetadata = null
schema v3 -> 保留 cards/status/drawHistory；decisionMetadata = null; rotationSnapshot = null
schema v4 -> 原樣讀取；history missing rotationSnapshot remains valid
unsupported schema -> block operations, preserve original file
corrupted/unreadable JSON -> block operations, preserve original file
```

**文件驗證規則**:

- 維持 005 的 card、metadata、status、history ID、operation ID、history card reference 驗證。
- 若 `RotationSnapshot` 存在，必須符合 non-negative count 與 count equation。
- 缺少 `RotationSnapshot` 不得封鎖資料。
- 寫入前驗證整份 document；失敗不得覆寫原檔。

## 關係

```text
DrawOperation 1 ── contains ── 0..1 RotationCooldownSettings
CardLibraryDocument 1 ── contains ── * DrawHistoryRecord
DrawHistoryRecord 1 ── contains optional ── 0..1 RotationSnapshot
RecentSuccessfulDrawRange 1 ── derives from ── * DrawHistoryRecord
RecentExclusionSet 1 ── derives from ── RecentSuccessfulDrawRange
RotationCandidatePool 1 ── derives from ── FilteredCandidatePool + RecentExclusionSet
DrawResult 1 ── displays ── 0..1 RotationSnapshot
```

## 狀態轉換與不變條件

```text
DrawSubmitted
  -> validate operation/mode/coin/meal/filter/rotation settings
  -> if OperationId has persisted success: replay existing card + rotation snapshot
  -> build 005 base/metadata pool
  -> if base pool empty: fail without history/statistics
  -> apply rotation cooldown
  -> if post-rotation pool empty: fail without history/statistics
  -> uniform random index over post-rotation pool
  -> append DrawHistoryRecord with RotationSnapshot atomically
```

- 防重複規則只能縮小候選池，不得改變剩餘候選池內權重。
- 最近排除使用 card ID，不使用文字、翻譯、metadata、排序或語系。
- deleted cards 不得進入 005 base pool；若 deleted card 出現在最近 N 筆 history，只作為歷史事實存在，不會排除其他 active cards。
- missing snapshot 的舊 history 仍可參與最近 N 次排除與統計。
- 語系、主題、動畫、reduced motion、顯示排序與統計投影不得改變候選池、排除集合、抽中 card ID 或 snapshot。
- 使用者可見訊息、日誌與 HTML 不得包含秘密值、完整 JSON、stack trace、系統提示或未清理輸入。
