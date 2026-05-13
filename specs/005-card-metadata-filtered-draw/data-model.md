# 資料模型: 餐點條件篩選抽卡

## 概觀

本功能在既有 schema v3 卡牌、抽卡歷史與統計模型上新增餐點決策資訊與篩選條件。持久化來源仍是單一 JSON 檔 `CardPicker2/data/cards.json`。schema v4 root 保留 `schemaVersion`、`cards` 與 `drawHistory`；`drawHistory` 不加入 metadata snapshot，因為 metadata 更新不得切分既有歷史。篩選條件只影響未來候選池與卡牌庫顯示，不影響既有成功歷史、統計分母或單卡抽中次數。

模型名稱可使用英文識別字；使用者可見的欄位名稱、選項、錯誤、空狀態與摘要必須依目前 runtime 語系呈現。

## Entity: PriceRange

代表使用者手動維護的餐點大致花費區間。

| 值 | 語意 | 規則 |
|----|------|------|
| `Low` | 低價位 | 只用於篩選與顯示，不代表卡牌價值或權重。 |
| `Medium` | 中價位 | 只用於篩選與顯示。 |
| `High` | 高價位 | 只用於篩選與顯示。 |

**驗證規則**:

- null 表示未知或未填。
- 非 enum 值不得持久化；create/edit 必須回傳 validation error。
- PriceRange 不得參與 duplicate detection、random weighting 或 statistics。

## Entity: PreparationTimeRange

代表使用者手動維護的準備、等待或取得時間區間。

| 值 | 語意 |
|----|------|
| `Quick` | 快速 |
| `Standard` | 一般 |
| `Long` | 較久 |

**驗證規則**:

- null 表示未知或未填。
- 非 enum 值不得持久化。
- 本欄位不要求自動計時、營業時間或外部店家資料。

## Entity: DietaryPreference

代表使用者手動標註的飲食偏好或餐點特性。每張卡牌可有多個值。

| 值 | 語意 |
|----|------|
| `Vegetarian` | 蔬食 |
| `Light` | 清淡 |
| `HeavyFlavor` | 重口味 |
| `TakeoutFriendly` | 適合外帶 |

**驗證規則**:

- 空集合表示未知或未填。
- 同一卡牌內不得有重複 enum 值。
- 篩選時，使用者選取多個 dietary preferences 代表卡牌必須同時包含全部選取值。

## Entity: SpiceLevel

代表使用者手動標註的辣度。

| 值 | 語意 | 順序 |
|----|------|------|
| `None` | 不辣 | 0 |
| `Mild` | 小辣 | 1 |
| `Medium` | 中辣 | 2 |
| `Hot` | 重辣 | 3 |

**篩選規則**:

- null 表示未知或未填。
- 首頁與卡牌庫的辣度篩選使用「最高可接受辣度」語意。例如選擇 `Mild` 時，`None` 與 `Mild` 符合，`Medium` 與 `Hot` 不符合。
- 缺少辣度的卡牌在套用辣度條件時不符合。

## Entity: MealCardDecisionMetadata

代表一張餐點卡牌的 optional 決策資訊。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Tags` | IReadOnlyList<string> | 否 | 自訂標籤；trim 後不可空白；同一卡牌內以 `OrdinalIgnoreCase` 去重；保留第一次出現的顯示文字。 |
| `PriceRange` | PriceRange? | 否 | `Low`、`Medium`、`High` 或 null。 |
| `PreparationTimeRange` | PreparationTimeRange? | 否 | `Quick`、`Standard`、`Long` 或 null。 |
| `DietaryPreferences` | IReadOnlySet<DietaryPreference> | 否 | 允許多選；不可重複；只接受支援 enum。 |
| `SpiceLevel` | SpiceLevel? | 否 | `None`、`Mild`、`Medium`、`Hot` 或 null。 |

**正規化規則**:

```text
Tags:
  trim each tag
  remove empty tags
  remove duplicates using OrdinalIgnoreCase
  preserve first non-empty display text

DietaryPreferences:
  remove duplicates
  sort by enum order for stable persistence/display
```

**不變條件**:

- 完全缺少 metadata 或所有欄位為空，不得視為 corrupted。
- 無效 enum、unsupported culture key 或 invalid tag collection 不得持久化。
- metadata 不參與 duplicate detection。
- metadata 更新不得改變 `MealCard.Id`、`CardStatus`、`DrawHistoryRecord` 或統計歸屬。

## Entity: CardFilterCriteria

代表首頁抽卡與卡牌庫搜尋共用的條件集合。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `MealType` | MealType? | 否 | 卡牌庫可選；首頁 Normal mode 必填；首頁 Random mode 忽略。 |
| `PriceRange` | PriceRange? | 否 | 有值時只符合相同 price range。 |
| `PreparationTimeRange` | PreparationTimeRange? | 否 | 有值時只符合相同 time range。 |
| `DietaryPreferences` | IReadOnlySet<DietaryPreference> | 否 | 多選時必須全部符合。 |
| `MaxSpiceLevel` | SpiceLevel? | 否 | 有值時符合 spice order <= selected level。 |
| `Tags` | IReadOnlyList<string> | 否 | 多個 tag 必須全部符合；比對 trim 後 `OrdinalIgnoreCase`。 |
| `CurrentLanguage` | SupportedLanguage | 是 | 只影響顯示摘要與 keyword 搜尋投影，不影響候選池身分。 |

**匹配規則**:

```text
Card matches criteria when:
  card.Status == Active
  and meal type matches when criteria.MealType has value
  and every non-empty metadata condition matches
  and missing metadata for a selected condition fails that condition
```

## Entity: SearchCriteria

延伸既有卡牌庫搜尋條件。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Keyword` | string? | 否 | Trim 後空白視為 null；比對目前語系 visible card name。 |
| `MealType` | MealType? | 否 | 空值代表全部餐別。 |
| `Filters` | CardFilterCriteria | 是 | metadata 條件集合。 |
| `CurrentLanguage` | SupportedLanguage | 是 | 搜尋 visible name 與顯示結果語系。 |

**規則**:

- Keyword 與 metadata filters 採交集規則。
- 搜尋結果只包含 active cards。
- deleted cards 不出現在一般卡牌庫結果。

## Entity: MealCardInputModel

延伸 create/edit form 的使用者輸入。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| 既有雙語 name/description | string | 是 | 維持 003 規則。 |
| `MealType` | MealType? | 是 | 維持既有規則。 |
| `TagsInput` | string? 或 collection | 否 | 支援逗號/換行分隔或多輸入；正規化後寫入 metadata tags。 |
| `PriceRange` | PriceRange? | 否 | 可留空。 |
| `PreparationTimeRange` | PreparationTimeRange? | 否 | 可留空。 |
| `DietaryPreferences` | collection | 否 | 多選。 |
| `SpiceLevel` | SpiceLevel? | 否 | 可留空。 |

**驗證規則**:

- metadata 欄位缺漏不得阻止卡牌新增或編輯，只要必要雙語欄位與餐別有效。
- metadata 欄位有值但 unsupported 時必須拒絕該次儲存。
- metadata 正規化與卡牌更新必須同一原子寫入；失敗時原卡牌不變。

## Entity: MealCard

延續既有 schema v3 雙語與生命週期模型，新增 metadata。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Id` | Guid | 是 | 不可變。 |
| `MealType` | MealType | 是 | `Breakfast`、`Lunch`、`Dinner`。 |
| `Localizations` | map | 是 | 維持 003/004 雙語規則。 |
| `Status` | CardStatus | 是 | `Active` 或 `Deleted`。 |
| `DeletedAtUtc` | DateTimeOffset? | 否 | 維持 004 規則。 |
| `DecisionMetadata` | MealCardDecisionMetadata? | 否 | null 或 empty 代表未知/未填。 |

**候選池規則**:

- 只有 active card 可進入候選池。
- 未套用 metadata filters 時，缺 metadata 的 active card 仍可抽出。
- 套用某項 metadata filter 時，缺該項 metadata 的 card 不符合。

## Entity: CardLibraryDocument

代表 `CardPicker2/data/cards.json` 的根文件。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `SchemaVersion` | int | 是 | v4 值為 `4`。 |
| `Cards` | IReadOnlyList<MealCard> | 是 | 可空；seed 建立時每餐別至少 3 張 active cards，建議含部分完整 metadata 以支援手動驗證。 |
| `DrawHistory` | IReadOnlyList<DrawHistoryRecord> | 是 | 只包含成功抽卡歷史；可空。 |

**schema v4 JSON 範例**:

```json
{
  "schemaVersion": 4,
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
      }
    }
  ],
  "drawHistory": []
}
```

**migration 規則**:

```text
schema v1 -> 既有單語轉 zh-TW localization；status = Active；drawHistory = []; decisionMetadata = null
schema v2 -> 保留 localizations；status = Active；drawHistory = []; decisionMetadata = null
schema v3 -> 保留 cards/status/drawHistory；decisionMetadata = null when missing
schema v4 -> 原樣讀取、正規化並完整驗證
unsupported schema -> block operations, preserve original file
corrupted/unreadable JSON -> block operations, preserve original file
```

**文件驗證規則**:

- 維持 v3 的 card ID、history ID、operation ID、status/deletedAt、history card reference 驗證。
- `DecisionMetadata` 可缺漏；若存在，所有 enum 與 collections 必須有效。
- tag 正規化後若變成空集合，可保存為 empty 或 null，但不可保存空白 tag。
- 寫入前驗證整份 document；失敗不得覆寫原檔。

## Entity: DrawOperation

延伸既有抽卡操作輸入。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| 既有 operation id / mode / meal type / coin / language | mixed | 是 | 維持 004 規則。 |
| `Filters` | CardFilterCriteria | 否 | 缺漏代表未套用 metadata filters。 |

**規則**:

- Normal mode 必須先驗證 meal type，再套用 filters。
- Random mode 忽略 meal type，但套用 metadata filters。
- 同一 `DrawOperationId` replay 原成功結果，不重新套用目前畫面上的新 filters。

## Entity: FilteredCandidatePool

代表一次抽卡在模式、餐別與 metadata filters 後可被等機率選取的卡牌集合。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Mode` | DrawMode | 是 | Normal 或 Random。 |
| `SelectedMealType` | MealType? | Normal 是；Random 否 | Random 忽略。 |
| `Filters` | CardFilterCriteria | 是 | 已正規化條件。 |
| `Cards` | IReadOnlyList<MealCard> | 是 | 只包含 active 且符合條件的 cards。 |
| `NominalProbability` | decimal? | Cards 非空時是 | 每張候選卡為 `1 / Cards.Count`。 |

**建構規則**:

```text
basePool =
  Normal -> activeCards where card.MealType == SelectedMealType
  Random -> all activeCards

filteredPool = basePool where MealCardFilterService.Matches(card, Filters)
```

## Entity: DrawResult

延伸既有抽卡結果。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| 既有 success / card / mode / operation / replay | mixed | 是 | 維持 004 規則。 |
| `AppliedFilters` | CardFilterCriteria | 否 | 成功或失敗時顯示此次提交條件。 |
| `LocalizedMetadataSummary` | FilterSummary | 成功時建議 | 顯示抽中卡牌的 metadata 摘要。 |

**規則**:

- 成功 result 必須對應一筆 `DrawHistoryRecord` 或 replay 既有 history。
- metadata summary 只輔助理解結果為何符合條件，不可暗示權重或推薦分數。
- 語系切換後可重新投影同一 card 的 metadata display text，不可重新抽卡。

## 關係

```text
MealCard 1 ── has optional ── 0..1 MealCardDecisionMetadata
SearchCriteria 1 ── contains ── 1 CardFilterCriteria
DrawOperation 1 ── contains ── 0..1 CardFilterCriteria
CardFilterCriteria 1 ── filters ── * MealCard
FilteredCandidatePool 1 ── derived from ── DrawOperation + Cards
DrawResult 1 ── references ── 0..1 MealCard and 0..1 FilterSummary
DrawHistoryRecord * ── references ── 1 MealCard by immutable CardId
```

## 狀態轉換與不變條件

```text
SchemaV3Loaded
  -> v4 in-memory document with DecisionMetadata = null
  -> next successful write persists schemaVersion = 4

CreateOrEditSubmitted
  -> validate required bilingual fields + meal type
  -> normalize metadata
  -> validate metadata enum/tag values
  -> duplicate check without metadata
  -> atomic write

DrawSubmitted
  -> validate operation id + mode + coin + meal type rules
  -> build base pool
  -> apply metadata filters
  -> empty pool fails without history/statistics change
  -> non-empty pool uses uniform random index
  -> append DrawHistoryRecord atomically
```

- 篩選條件只能縮小候選池，不得改變候選池內權重。
- metadata 缺漏不得讓舊卡失效；但套用該欄位條件時缺漏值不符合。
- metadata 更新不得改變卡牌 ID、draw history、statistics row identity 或 deleted state。
- deleted cards 不得出現在一般篩選結果或未來候選池。
- 語系、主題、動畫、reduced motion、顯示排序與歷史統計不得影響篩選後候選池內的等機率選取。
- 使用者可見訊息、日誌與 HTML 不得包含秘密值、完整 JSON、stack trace、系統提示或未清理輸入。
