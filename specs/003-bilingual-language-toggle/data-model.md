# 資料模型: 雙語語系切換

## 概觀

本功能新增兩類資料模型：

- 語系偏好與 UI 本地化狀態，來源為 ASP.NET Core request culture 與 culture cookie。
- 餐點卡牌雙語內容，來源為本機 JSON `CardPicker2/data/cards.json` 的 schema v2 localizations，並支援讀取既有 schema v1。

模型名稱可使用英文識別字；所有使用者可見內容必須能依 `zh-TW` 或 `en-US` 呈現。

## Entity: SupportedLanguage

代表系統允許的 runtime UI 語系。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `CultureName` | string | 是 | 僅允許 `zh-TW` 或 `en-US`。 |
| `DisplayName` | string | 是 | 依目前語系顯示，例如「繁體中文」/`Traditional Chinese`、`English`/「英文」。 |
| `HtmlLang` | string | 是 | `zh-Hant` 或 `en`，套用到 `<html lang>`. |
| `IsDefault` | bool | 是 | 只有 `zh-TW` 為 true。 |

**驗證規則**:

- cookie、表單欄位、query/debug 輸入與任何 client state 都必須以白名單驗證。
- 無效、空白、缺失或 unsupported culture 一律回到 `zh-TW`。
- 不得因瀏覽器 `Accept-Language` 自動改變預設語系。

## Entity: LanguagePreference

代表同一瀏覽器與裝置上最近一次有效語系選擇。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `CookieName` | string | 是 | 固定使用 ASP.NET Core culture cookie `.AspNetCore.Culture`。 |
| `CultureName` | `SupportedLanguage` | 是 | cookie 中的 culture 與 ui-culture 必須都映射到支援語系。 |
| `ExpiresUtc` | DateTimeOffset? | 否 | 建議保存 1 年；cookie 不可用時允許 null。 |
| `CanPersist` | bool | 否 | cookie 成功寫入時為 true，失敗時目前 request 仍可使用使用者選擇。 |

**cookie 值範例**:

```text
c=zh-TW|uic=zh-TW
c=en-US|uic=en-US
```

**驗證規則**:

- 只接受 ASP.NET Core `CookieRequestCultureProvider` 可解析的 culture cookie。
- culture 與 ui-culture 若不一致或任一值 unsupported，視為無效並回到 `zh-TW`。
- Cookie 不包含使用者識別、餐點資料、秘密值、stack trace 或完整內部狀態。

## Entity: LocalizedTextResource

代表 UI、驗證與系統訊息的 resource key。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Key` | string | 是 | 穩定英文識別字，例如 `Nav.Cards`、`Validation.MealNameRequired`。 |
| `ZhTwValue` | string | 是 | 繁體中文文字，不可空白。 |
| `EnUsValue` | string | 是 | 英文文字，不可空白。 |
| `AllowsHtml` | bool | 是 | 預設 false；若 true，必須在 contract 中說明安全理由。 |

**驗證規則**:

- 所有使用者可見導覽、按鈕、功能名稱、表單 label、狀態、成功、錯誤、確認、復原與 fallback prompt 都必須有兩語系 resource。
- 一般文字 resource 不得包含 HTML；需要連結或強調時以 Razor markup 組合 localized text。
- Resource value 不得包含秘密值、連線字串、系統提示或完整內部資料內容。

## Entity: MealCardLocalizedContent

代表同一張餐點卡牌在單一語系下的餐點內容。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `CultureName` | `SupportedLanguage` | 是 | `zh-TW` 或 `en-US`。 |
| `Name` | string | 是 | Trim 後不可空白。 |
| `Description` | string | 是 | Trim 後不可空白。 |

**驗證規則**:

- 新增與編輯後的卡牌必須同時具備 `zh-TW` 與 `en-US` 的非空 `Name`/`Description`。
- 從 schema v1 載入的既有卡牌可暫時缺少 `en-US`，但必須在英文語系顯示繁中 fallback 並提供補齊提示。
- 儲存 schema v2 時，缺少必要繁中內容必須 block；缺少英文內容只允許來自既有 v1 migration 且不得由新增/編輯產生。

## Entity: MealCard

代表一張可瀏覽、搜尋、編輯、刪除與抽卡的餐點卡牌。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `Id` | Guid | 是 | 系統產生且不可變；不可為 `Guid.Empty`。 |
| `MealType` | enum | 是 | 僅允許 `Breakfast`、`Lunch`、`Dinner`。 |
| `Localizations` | map | 是 | key 僅允許 `zh-TW`、`en-US`；value 為 `MealCardLocalizedContent`。 |
| `TranslationStatus` | enum/flags | 是 | `Complete` 或 `MissingEnglish`；不得隱藏 fallback 狀態。 |

**schema v2 JSON 範例**:

```json
{
  "schemaVersion": 2,
  "cards": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "mealType": "Breakfast",
      "localizations": {
        "zh-TW": {
          "name": "鮪魚蛋餅",
          "description": "附近早餐店的鮪魚蛋餅，加一杯無糖豆漿。"
        },
        "en-US": {
          "name": "Tuna Egg Pancake",
          "description": "A nearby breakfast-shop tuna egg pancake with unsweetened soy milk."
        }
      }
    }
  ]
}
```

**schema v1 migration 規則**:

```text
schemaVersion = 1 card.Name        -> Localizations["zh-TW"].Name
schemaVersion = 1 card.Description -> Localizations["zh-TW"].Description
schemaVersion = 1 card.MealType    -> MealType
schemaVersion = 1 card.Id          -> Id
en-US localization missing         -> TranslationStatus.MissingEnglish
```

**不變條件**:

- 語系切換不得建立新 `MealCard` 或改變 `Id`。
- 翻譯內容不得建立第二張抽卡用卡牌。
- 抽卡 pool 只看 `MealType` 與有效卡牌，不看目前語系。

## Entity: MealCardInputModel

代表 create/edit form 的使用者輸入。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `NameZhTw` | string | 是 | Trim 後不可空白。 |
| `DescriptionZhTw` | string | 是 | Trim 後不可空白。 |
| `NameEnUs` | string | 是 | Trim 後不可空白。 |
| `DescriptionEnUs` | string | 是 | Trim 後不可空白。 |
| `MealType` | MealType? | 是 | 必須是有效餐別。 |

**驗證規則**:

- DataAnnotations 與 `IValidatableObject` 錯誤訊息都使用 current culture。
- Normalize 時只 trim 文字，不改變大小寫與原始語言。
- 任一必要語系欄位缺漏時拒絕儲存，且不得局部寫入 JSON。

## Entity: LocalizedMealCardView

代表目前語系下可顯示與搜尋的一張卡牌投影。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `CardId` | Guid | 是 | 來源 `MealCard.Id`。 |
| `MealType` | MealType | 是 | 來源餐別。 |
| `MealTypeDisplayName` | string | 是 | 依目前語系顯示早餐/午餐/晚餐。 |
| `DisplayName` | string | 是 | 目前語系 name；英文缺漏時使用繁中 fallback。 |
| `DisplayDescription` | string | 是 | 目前語系 description；英文缺漏時使用繁中 fallback。 |
| `CultureName` | SupportedLanguage | 是 | 投影使用的目前語系。 |
| `IsFallback` | bool | 是 | 目前語系內容是否使用 fallback。 |
| `MissingTranslationCultures` | collection | 否 | 缺漏語系，例如 `en-US`。 |

**投影規則**:

```text
current = zh-TW -> 使用 zh-TW content
current = en-US and en-US complete -> 使用 en-US content
current = en-US and en-US missing -> 使用 zh-TW fallback, IsFallback = true
```

**搜尋規則**:

- 搜尋 keyword 只比對 `DisplayName`。
- 比對使用 `Contains(keyword, OrdinalIgnoreCase)` 並先 trim keyword。
- 英文語系 fallback 卡牌以 fallback `DisplayName` 比對。

## Entity: DuplicateCandidate

代表 duplicate detection 使用的正規化比對值。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `CardId` | Guid | 是 | 來源卡牌 ID。 |
| `MealType` | MealType | 是 | 必須相同才可能 duplicate。 |
| `CultureName` | SupportedLanguage | 是 | `zh-TW` 或 `en-US`。 |
| `NormalizedName` | string | 是 | `Name.Trim()`。 |
| `NormalizedDescription` | string | 是 | `Description.Trim()`。 |

**duplicate 規則**:

- 同餐別、不同 `CardId`，且任一語系的 `NormalizedName` 與 `NormalizedDescription` 以 `OrdinalIgnoreCase` 相同，即 duplicate。
- 英文缺漏卡牌的 `en-US` candidate 使用英文模式可見 fallback pair。
- 同名不同描述允許；不同餐別允許。

## Entity: DrawResult

代表一次抽卡結果。

| 欄位 | 型別 | 必填 | 規則 |
|------|------|------|------|
| `MealType` | MealType | 是 | 使用者選擇的餐別。 |
| `CardId` | Guid | 成功時是 | 抽出的卡牌 ID；語系切換後用於重新 render 同一結果。 |
| `LocalizedCard` | LocalizedMealCardView? | 成功時是 | 依目前語系投影。 |
| `StatusKey` | string | 是 | 成功或失敗訊息 key。 |

**狀態規則**:

- 抽卡只在 `POST /?handler=Draw` 成功驗證後發生。
- 語系切換、動畫 timing、display order 或 repeated click 不得重新抽卡。
- 若語系切換後用 `CardId` 重新 render，必須確認卡牌仍存在且餐別仍符合；若不存在，顯示目前語系的可復原訊息，不得抽另一張替代。

## 關係

```text
LanguagePreference 1 ── selects ── 1 SupportedLanguage
SupportedLanguage 1 ── resolves ── * LocalizedTextResource
MealCard 1 ── contains ── 1..2 MealCardLocalizedContent
MealCard 1 ── projects to ── 1 LocalizedMealCardView per request culture
MealCardInputModel 1 ── creates/updates ── 1 MealCard
DuplicateCandidate * ── derived from ── * MealCardLocalizedContent
DrawResult 1 ── references ── 0..1 MealCard by CardId
```

## 狀態轉換

```text
NoCookie/InvalidCookie
  -> SupportedLanguage(zh-TW)
  -> render zh-TW

UserPostsLanguage(en-US)
  -> validate anti-forgery + allowed culture
  -> write .AspNetCore.Culture
  -> redirect/render current page in en-US

SchemaV1CardLoaded
  -> Localizations["zh-TW"] populated
  -> TranslationStatus.MissingEnglish
  -> en-US display uses zh-TW fallback

CreateOrEditCardSubmitted
  -> validate zh-TW + en-US fields
  -> duplicate check across both languages
  -> write schema v2 atomically

DrawSubmitted
  -> validate meal type + coin state
  -> select card by meal type only
  -> render LocalizedMealCardView for current culture
  -> language switch re-renders same CardId
```

## 不變條件

- 無有效語系偏好時必須預設 `zh-TW`。
- 語系切換不得清除搜尋條件、未送出表單輸入、validation state、刪除確認狀態或已揭示抽卡結果。
- 語系切換不得改變 `CardId`、`MealType`、抽卡機率、duplicate detection 規則、已刪除狀態或 JSON atomic write 規則。
- 新增/編輯卡牌不得產生任一語系空白 name/description。
- Corrupted、unreadable 或 unsupported JSON 不得被 seed 或 migration 覆蓋。
- 使用者可見訊息不得包含秘密值、連線字串、完整 JSON 內容、stack trace 或系統提示。
