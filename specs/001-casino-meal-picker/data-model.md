# 資料模型: 餐點抽卡網站2

## 概觀

資料模型以單一本機 JSON 文件為持久化邊界。Razor Pages 不直接操作檔案；所有讀取、驗證、搜尋、抽卡與寫入皆經由服務層完成。模型欄位以英文命名，使用者可見顯示文字與驗證訊息使用繁體中文。

## Entity: MealType

代表支援的餐別列舉。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Value` | enum/string | 是 | 僅允許 `Breakfast`、`Lunch`、`Dinner`。 |
| `DisplayName` | string | 是 | 顯示為 `早餐`、`午餐`、`晚餐`。 |

**驗證規則**:

- 任何輸入、查詢、抽卡與 JSON 反序列化結果都不得接受三種餐別以外的值。
- UI 顯示使用繁體中文；持久化建議使用穩定英文 enum 值，避免文案調整破壞資料相容性。

## Entity: MealCard

代表一張可瀏覽、搜尋、管理與抽出的餐點卡牌。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Id` | `Guid` | 是 | 系統自動產生；建立後不可變；用於編輯、刪除、詳情與抽卡結果引用。 |
| `Name` | `string` | 是 | 餐點名稱；trim 後不得為空；使用者可見。 |
| `MealType` | `MealType` | 是 | 必須是早餐、午餐、晚餐之一。 |
| `Description` | `string` | 是 | 完整描述；trim 後不得為空；可包含店家、推薦品項或決策資訊。 |

**驗證規則**:

- `Name`、`MealType`、`Description` 均為必填。
- `Name.Trim()` 與 `Description.Trim()` 不得為空白字串。
- 新增時由服務層產生新 `Guid`；任何使用者輸入的 ID 不可信。
- 編輯時不得改變 `Id`。
- 新增與編輯後不得與另一張卡牌形成完全重複。

**重複判斷 key**:

```text
NormalizedName = Name.Trim()，以 OrdinalIgnoreCase 比對
MealType = enum 值完全相同
NormalizedDescription = Description.Trim()，以 OrdinalIgnoreCase 比對
```

兩張卡牌只有在三者都相同時才視為重複。名稱相同但描述不同，或描述相同但餐別不同，都可以並存。

## Entity: CardLibraryDocument

代表 JSON 檔案的根文件。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `SchemaVersion` | `int` | 是 | 第一版固定為 `1`。不支援版本時應回報復原錯誤，不應靜默忽略。 |
| `Cards` | `IReadOnlyList<MealCard>` | 是 | 可為空集合，但首次啟動預設資料必須早餐、午餐、晚餐各至少 3 張。 |

**JSON 範例**:

```json
{
  "schemaVersion": 1,
  "cards": [
    {
      "id": "018f4c92-7a7d-4b7e-b34a-88f4f3a82d91",
      "name": "鮪魚蛋餅",
      "mealType": "Breakfast",
      "description": "附近早餐店的鮪魚蛋餅，加一杯無糖豆漿。"
    }
  ]
}
```

**驗證規則**:

- 檔案不存在時，服務層建立預設 `CardLibraryDocument` 並寫入 `data/cards.json`。
- 檔案存在但不可讀、JSON 格式錯誤、schema 不支援或必要欄位缺失時，保留原檔並回傳 blocking recovery 狀態。
- 有效文件載入後，服務層仍需驗證每張卡牌的必要欄位、餐別與重複規則。

## Entity: SearchCriteria

代表列表頁使用者輸入的搜尋條件。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Keyword` | `string?` | 否 | trim 後若為空視為未提供；比對 `MealCard.Name`，大小寫不敏感，部分比對。 |
| `MealType` | `MealType?` | 否 | 提供時必須為支援餐別；未提供時搜尋全部餐別。 |

**搜尋規則**:

- 未提供任何條件時回傳全部卡牌。
- 同時提供 `Keyword` 與 `MealType` 時，結果必須同時符合兩者。
- 搜尋結果不得包含已刪除或無效卡牌。
- 無結果時 UI 顯示「查無符合條件的餐點卡牌」並保留調整條件入口。

## Entity: DrawOperationState

代表首頁抽卡流程的使用者可見狀態。這是 UI state，不應寫入 JSON 卡牌庫。

| 狀態 | 可轉移至 | 說明 |
|------|----------|------|
| `Idle` | `MealSelected` | 尚未選擇餐別或尚未投幣。 |
| `MealSelected` | `CoinInserted`, `Idle` | 已選餐別，可投幣或重新選擇。 |
| `CoinInserted` | `Spinning`, `Idle` | 已投幣，可拉桿/開始或取消本次操作。 |
| `Spinning` | `Revealed`, `Blocked` | 抽卡進行中；UI 必須禁用重複啟動。 |
| `Revealed` | `MealSelected`, `Idle` | 已揭示結果，可再抽一次或清除狀態。 |
| `Blocked` | `Idle` | 因未選餐別、空卡池、資料檔不可用或驗證錯誤而阻止抽卡。 |

**轉換規則**:

- 未選餐別不得從 `Idle` 進入 `CoinInserted` 或 `Spinning`。
- `Spinning` 期間不得再次提交抽卡。
- `prefers-reduced-motion: reduce` 時仍可經過 `Spinning` 邏輯狀態，但 UI 略過連續轉動動畫，直接顯示短暫靜態揭示。
- 動畫時間、顯示順序與 UI 狀態不得影響實際抽卡結果。

## Entity: DrawResult

代表單次抽卡結果。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `SelectedMealType` | `MealType` | 是 | 使用者提交的餐別。 |
| `CardId` | `Guid` | 是 | 必須對應仍存在且有效的 `MealCard.Id`。 |
| `Name` | `string` | 是 | 抽中卡牌的餐點名稱。 |
| `MealType` | `MealType` | 是 | 必須等於 `SelectedMealType`。 |
| `Description` | `string` | 是 | 抽中卡牌的完整描述。 |

**驗證規則**:

- 若所選餐別沒有任何有效卡牌，不產生 `DrawResult`。
- 若卡牌庫處於 blocking recovery 狀態，不產生 `DrawResult`。
- 抽卡只從所選餐別的現有有效卡牌集合中等機率選取。

## Entity: CardLibraryLoadResult

代表服務層載入卡牌庫後的狀態。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Status` | enum | 是 | `Ready`、`CreatedFromSeed`、`MissingCreated`、`BlockedCorruptFile`、`BlockedUnreadableFile`。 |
| `Document` | `CardLibraryDocument?` | 視狀態 | `Ready` 類狀態必須有文件；blocking 狀態不得讓 UI 執行卡牌操作。 |
| `UserMessage` | `string` | 是 | 使用者可見繁體中文訊息，不包含檔案內敏感內容或 stack trace。 |
| `DiagnosticMessage` | `string?` | 否 | 給日誌使用的診斷摘要，不記錄秘密值。 |

**狀態規則**:

- `MissingCreated`: 檔案不存在且已成功以預設資料建立。
- `BlockedCorruptFile`: 檔案存在但 JSON 無法解析或 schema/資料驗證失敗。
- `BlockedUnreadableFile`: 檔案存在但無法讀取。
- blocking 狀態下，列表、搜尋、建立、編輯、刪除與抽卡頁面都必須顯示復原錯誤並阻止狀態變更。

## 關係

```text
CardLibraryDocument 1 ── * MealCard
SearchCriteria 0..1 ── filters ── * MealCard
DrawResult 1 ── references ── 1 MealCard
DrawOperationState 1 ── may reveal ── 0..1 DrawResult
CardLibraryLoadResult 1 ── contains ── 0..1 CardLibraryDocument
```

## 持久化與原子性

- 讀取時完整載入 `CardLibraryDocument`，再執行資料驗證。
- 寫入時先建立完整的新文件內容，不直接就地修改原檔。
- 寫入流程必須完整成功才替換目標檔；任一步驟失敗不得留下部分更新狀態。
- 刪除為永久移除；本階段不提供回收桶或還原機制。

## 初始種子資料需求

預設卡牌必須至少包含：

- 早餐 3 張以上
- 午餐 3 張以上
- 晚餐 3 張以上

每張預設卡牌都必須符合 `MealCard` 驗證規則，且不得在重複判斷 key 上互相重複。
