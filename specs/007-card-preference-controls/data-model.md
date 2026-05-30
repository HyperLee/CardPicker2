# 資料模型: 餐點收藏與手動排除抽卡

## 概觀

本功能在 006 完成後的 schema v4 卡牌、metadata filters、成功抽卡歷史、輪替快照與統計模型上新增長期卡牌偏好狀態。持久化來源仍是單一 JSON 檔 `CardPicker2/data/cards.json`。schema v5 root 保留 `schemaVersion`、`cards` 與 `drawHistory`；每張 active 或 retained deleted `MealCard` 可包含 `preferences`。缺少 `preferences` 的舊資料不得被視為 corrupted，必須以未收藏且未排除抽卡的安全預設值載入。

偏好狀態只影響目前與未來的 card browsing/search/draw candidate pool。它不改變既有成功抽卡歷史、統計公式、rotation snapshot、card ID、餐別、雙語內容、metadata 或刪除狀態。模型名稱可使用英文識別字；使用者可見欄位、提示、摘要、空狀態與 validation message 必須依目前 runtime 語系呈現。

## Entity: CardPreferenceState

代表一張卡牌的長期使用者整理狀態。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `IsFavorite` | bool | 是 | 預設 `false`；只用於顯示、卡牌庫篩選與結果區操作，不影響抽卡機率。 |
| `IsExcludedFromDraw` | bool | 是 | 預設 `false`；`true` 時 active card 不得進入未來任何抽卡候選池。 |

**JSON 範例**:

```json
{
  "preferences": {
    "isFavorite": true,
    "isExcludedFromDraw": false
  }
}
```

**不變條件**:

- 缺少 `preferences`、`isFavorite` 或 `isExcludedFromDraw` 時，讀取為 `false`。
- `IsFavorite` 不得參與 duplicate detection、candidate pool weighting、rotation exclusion、history append 或 statistics。
- `IsExcludedFromDraw` 只阻止未來候選池；不得刪除 card，不得修改既有 history 或 statistics。
- deleted card 若保留 preferences，preferences 不得讓它回到 active list 或 candidate pool。

## Entity: CardPreferenceUpdateInputModel

代表收藏/排除表單提交的 target-state mutation。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `CardId` | Guid | 是 | 目標 card ID；不可為 `Guid.Empty`。 |
| `TargetIsFavorite` | bool? | 否 | 有值時將收藏狀態設為該值。 |
| `TargetIsExcludedFromDraw` | bool? | 否 | 有值時將排除抽卡狀態設為該值。 |
| `ReturnUrl` | string? | 否 | Razor Pages redirect 用；必須驗證為 local URL，否則回安全頁面。 |
| `ResultOperationId` | Guid? | 否 | 首頁結果區可用於重顯同一次成功結果；不得觸發重新抽卡。 |
| `ResultCardId` | Guid? | 否 | 首頁結果區可用於重顯同一 card；不得作為新抽卡來源。 |

**驗證規則**:

- `CardId` 無效、card 不存在、card 已 deleted 或資料封鎖時，拒絕操作並顯示目前語系訊息。
- `TargetIsFavorite` 與 `TargetIsExcludedFromDraw` 至少一個必須有值。
- 重複提交相同 target state 必須得到相同最終狀態，不得反轉。
- 更新必須完整成功或完整失敗；寫入失敗時原 preferences 不變。
- 表單必須包含 Anti-Forgery token。

## Entity: PreferenceMutationResult

代表一次偏好狀態更新的結果。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Succeeded` | bool | 是 | 寫入成功且資料可重新載入時為 true。 |
| `Status` | enum/string | 是 | `Succeeded`、`NotFound`、`Deleted`、`Blocked`、`ValidationFailed`、`WriteFailed`。 |
| `CardId` | Guid? | 否 | 成功或已知目標時提供。 |
| `Preferences` | CardPreferenceState? | 否 | 成功後最新狀態。 |
| `MessageKey` | string | 是 | stable localized message key。 |

**規則**:

- 成功更新不得建立 `DrawHistoryRecord`。
- 成功更新不得改變 `RotationSnapshot` 或 `DrawStatisticsSummary`。
- 失敗訊息不得包含完整 JSON、stack trace、系統提示、秘密值或未清理輸入。

## Entity: FavoriteFilter

代表卡牌庫依收藏狀態篩選的選項。

| 值 | 規則 |
|----|------|
| `All` | 不依收藏狀態過濾。 |
| `FavoritesOnly` | 只顯示 `Preferences.IsFavorite == true` 的 active cards。 |
| `NotFavoritesOnly` | 只顯示 `Preferences.IsFavorite == false` 的 active cards。 |

**驗證規則**:

- query/form 中 unsupported 值必須拒絕或忽略為 `All`，並以目前語系顯示可修正訊息。
- 收藏篩選不得影響 draw candidate pool。

## Entity: DrawEligibilityFilter

代表卡牌庫依可抽狀態篩選的選項。

| 值 | 規則 |
|----|------|
| `All` | 顯示所有 active cards，包含排除抽卡卡牌。 |
| `DrawableOnly` | 只顯示 active 且 `IsExcludedFromDraw == false` 的 cards。 |
| `ExcludedOnly` | 只顯示 active 且 `IsExcludedFromDraw == true` 的 cards。 |

**驗證規則**:

- 卡牌庫預設使用 `All`，讓已排除卡牌可被找到並取消排除。
- `ExcludedOnly` 不得顯示 deleted cards；deleted card 仍由統計表的歷史列處理。

## Entity: CardPreferenceCriteria

代表卡牌庫與局部查詢用的偏好篩選條件。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `FavoriteFilter` | FavoriteFilter | 是 | 預設 `All`。 |
| `DrawEligibilityFilter` | DrawEligibilityFilter | 是 | 預設 `All`。 |

**匹配規則**:

```text
Card matches preference criteria when:
  card.Status == Active
  and FavoriteFilter matches card.Preferences.IsFavorite
  and DrawEligibilityFilter matches card.Preferences.IsExcludedFromDraw
```

**不變條件**:

- Preference criteria 只影響卡牌庫/查詢顯示，不影響抽卡結果。
- 抽卡候選池永遠使用 `IsExcludedFromDraw == false`，不讀取 `FavoriteFilter`。

## Entity: SearchCriteria

延伸既有卡牌庫搜尋條件。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Keyword` | string? | 否 | Trim 後空白視為 null；比對目前語系 visible card name。 |
| `MealType` | MealType? | 否 | 空值代表全部餐別。 |
| `Filters` | CardFilterCriteria | 是 | 005 metadata 條件集合。 |
| `Preferences` | CardPreferenceCriteria | 是 | 收藏與可抽狀態篩選。 |
| `CurrentLanguage` | SupportedLanguage | 是 | 搜尋 visible name 與顯示結果語系。 |

**規則**:

- Keyword、meal type、metadata filters 與 preference filters 採交集。
- 搜尋結果只包含 active cards。
- 排除抽卡 cards 預設仍包含在結果中，除非使用者選擇 `DrawableOnly`。
- deleted cards 不出現在一般卡牌庫結果。

## Entity: MealCard

延續既有 schema v4 雙語、metadata 與生命週期模型，新增 preferences。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Id` | Guid | 是 | 不可變。 |
| `MealType` | MealType | 是 | `Breakfast`、`Lunch`、`Dinner`。 |
| `Localizations` | map | 是 | 維持 003/004 雙語規則。 |
| `Status` | CardStatus | 是 | `Active` 或 `Deleted`。 |
| `DeletedAtUtc` | DateTimeOffset? | 否 | 維持 004 規則。 |
| `DecisionMetadata` | MealCardDecisionMetadata? | 否 | 維持 005 規則。 |
| `Preferences` | CardPreferenceState | 是 | 缺漏時預設未收藏且未排除。 |

**衍生屬性**:

```text
IsActive = Status == Active
IsDrawable = Status == Active && Preferences.IsExcludedFromDraw == false
IsPreferenceEditable = Status == Active
```

**候選池規則**:

- 只有 `IsDrawable == true` 且資料驗證通過的 cards 可進入 normal/random base pool。
- 被排除抽卡的 active card 仍可在卡牌庫顯示、搜尋、查看詳情、編輯、取消排除與刪除。
- 收藏狀態不得影響 `IsDrawable`。

## Entity: CardLibraryDocument

代表 `CardPicker2/data/cards.json` 的根文件。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `SchemaVersion` | int | 是 | v5 值為 `5`。 |
| `Cards` | IReadOnlyList<MealCard> | 是 | 可空；seed 建立時每餐別至少 3 張 active drawable cards。 |
| `DrawHistory` | IReadOnlyList<DrawHistoryRecord> | 是 | 只包含成功抽卡歷史；可含 006 optional `RotationSnapshot`。 |

**schema v5 JSON 範例**:

```json
{
  "schemaVersion": 5,
  "cards": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "mealType": "Lunch",
      "status": "Active",
      "deletedAtUtc": null,
      "localizations": {
        "zh-TW": {
          "name": "菇菇蔬食便當",
          "description": "杏鮑菇、青花菜與豆干搭配糙米飯。"
        },
        "en-US": {
          "name": "Mushroom Vegetable Bento",
          "description": "King oyster mushrooms, broccoli, tofu, and brown rice."
        }
      },
      "decisionMetadata": {
        "tags": [ "蔬食", "便當" ],
        "priceRange": "Medium",
        "preparationTimeRange": "Quick",
        "dietaryPreferences": [ "Vegetarian", "TakeoutFriendly" ],
        "spiceLevel": "None"
      },
      "preferences": {
        "isFavorite": true,
        "isExcludedFromDraw": false
      }
    }
  ],
  "drawHistory": []
}
```

**migration 規則**:

```text
schema v1 -> 既有單語轉 zh-TW localization；status = Active；drawHistory = []; decisionMetadata = null; preferences = default
schema v2 -> 保留 localizations；status = Active；drawHistory = []; decisionMetadata = null; preferences = default
schema v3 -> 保留 cards/status/drawHistory；decisionMetadata = null; rotationSnapshot = null; preferences = default
schema v4 -> 保留 cards/status/decisionMetadata/drawHistory/rotationSnapshot；preferences = default when missing
schema v5 -> 原樣讀取、正規化並完整驗證
unsupported schema -> block operations, preserve original file
corrupted/unreadable JSON -> block operations, preserve original file
```

**文件驗證規則**:

- 維持 006 的 card、metadata、status、history ID、operation ID、history card reference、rotation snapshot 驗證。
- `Preferences` 可缺漏於舊 schema；寫入 v5 時必須存在或由 serializer 保存預設值。
- preferences bool 值無法解析、JSON 型別錯誤或 document 結構無效時，依既有 corrupted/block 規則處理。
- 寫入前驗證整份 document；失敗不得覆寫原檔。

## Entity: DrawOperation

延續既有抽卡操作輸入。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| 既有 operation id / mode / meal type / coin / language / filters / rotation | mixed | 是 | 維持 004/005/006 規則。 |

**偏好相關規則**:

- DrawOperation 不需要使用者提交 preference filters。
- 服務在建立 base pool 前移除 `IsExcludedFromDraw == true` 的 active cards。
- `DrawOperationId` 已有成功 history 時，直接 replay 原 result/snapshot，不因該 card 後來被排除而改變原結果。

## Entity: DrawCandidatePool

延伸既有候選池語意，加入手動排除前置規則。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Cards` | IReadOnlyList<MealCard> | 是 | 只包含 active、未排除抽卡且符合 mode/meal/metadata 的 cards。 |
| `ExcludedByPreferenceCount` | int | 建議 | 從 active cards 中因 `IsExcludedFromDraw` 被移除的數量，用於日誌與空候選池原因。 |

**建構規則**:

```text
drawableCards = cards where card.Status == Active && !card.Preferences.IsExcludedFromDraw
basePool =
  Normal -> drawableCards where card.MealType == SelectedMealType
  Random -> drawableCards
filteredPool = basePool where MealCardFilterService.Matches(card, metadata filters)
rotationPool = DrawRotationCooldownService.Apply(filteredPool, drawHistory, settings)
```

**空候選池原因**:

- 若 active cards 存在但全部因 preference exclusion 被移除，回傳 `PreferenceCandidatePoolEmpty` 或等效 status key。
- 若 metadata filters 導致空集合，維持 005 empty metadata message。
- 若 rotation 導致空集合，維持 006 empty after cooldown message。

## Entity: DrawResult

延伸既有抽卡結果，讓結果區可呈現與操作目前 card preference。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| 既有 success / card / mode / operation / filters / rotation | mixed | 是 | 維持 004/005/006 規則。 |
| `SelectedCardPreferences` | CardPreferenceState? | 成功時建議 | 成功或 replay 時顯示目前 card preference state。 |
| `PreferenceMessageKey` | string? | 否 | 結果區偏好操作成功/失敗後的 message key。 |

**規則**:

- 成功 result 必須對應一筆 `DrawHistoryRecord` 或 replay 既有 history。
- 若 result card 後來被排除，重顯同一成功結果時仍可顯示該 card，並標示目前已排除；不得重新抽卡。
- 對 result card 執行偏好操作不得新增 history、不得改變 statistics、不得改變 rotation snapshot。

## 關係

```text
MealCard 1 ── has ── 1 CardPreferenceState
SearchCriteria 1 ── contains ── 1 CardPreferenceCriteria
CardPreferenceUpdateInputModel 1 ── targets ── 1 MealCard
DrawCandidatePool 1 ── derived from ── drawable MealCards + mode + metadata filters
RotationCandidatePool 1 ── derived from ── DrawCandidatePool + recent history
DrawResult 1 ── displays ── 0..1 CardPreferenceState for selected MealCard
DrawHistoryRecord * ── references ── 1 MealCard by immutable CardId
```

## 狀態轉換與不變條件

```text
SchemaV4Loaded
  -> v5 in-memory document with Preferences = Default
  -> next successful write persists schemaVersion = 5

PreferenceSubmitted
  -> validate anti-forgery + card ID + target state
  -> load document in exclusive coordinator
  -> reject blocked/missing/deleted card
  -> set target preference state
  -> validate whole document
  -> atomic write

DrawSubmitted
  -> validate operation/mode/coin/meal/filter/rotation settings
  -> if OperationId has persisted success: replay existing card + rotation snapshot
  -> remove manually excluded active cards
  -> build 005 base/metadata pool
  -> apply 006 rotation cooldown
  -> empty pool fails without history/statistics
  -> uniform random index over final pool
  -> append DrawHistoryRecord atomically
```

- 收藏狀態只影響整理與篩選，不得改變抽卡、公平性、統計或輪替。
- 排除抽卡狀態只影響未來候選池，不得刪除 card，也不得修改既有 history。
- target-state preference update 重複提交不得反覆反轉。
- 語系、主題、動畫、reduced motion、顯示排序與統計投影不得改變 preferences、candidate pool facts、card ID、history 或 snapshot。
- 使用者可見訊息、日誌與 HTML 不得包含秘密值、完整 JSON、stack trace、系統提示或未清理輸入。
