# 研究: 餐點條件篩選抽卡

## Decision: 既有 ASP.NET Core Razor Pages 技術棧足以支援本功能

**決策**: 保留 ASP.NET Core Razor Pages、Razor form POST、query string、Bootstrap 5、jQuery Validation、ASP.NET Core Localization middleware、System.Text.Json、Serilog、xUnit/Moq 與 WebApplicationFactory。005 不新增 MVC controller、Minimal API、SPA framework、資料庫或外部資料服務。

**理由**: 本功能的公開介面仍是使用者可見頁面、表單欄位、query string、validation message、狀態與 HTML 結果。Razor Pages 適合 page-centered form workflow；現有 app 已具備 Anti-Forgery、localization、schema migration、Serilog、MapStaticAssets 與測試專案。條件篩選可在服務層以本機記憶體集合處理，不需要 DB query engine 或 API surface。

**Alternatives considered**:

- 新增 Minimal API 提供篩選與抽卡 JSON endpoint：會擴大公開契約，違反 FR-019。
- 改用資料庫：超出單機 JSON 產品邊界，也增加 schema migration 與部署成本。
- 改成 SPA：不符合目前 Razor Pages 架構，並增加 server validation 與 localization state 同步風險。

**參考來源**:

- Microsoft Learn, [Razor Pages in ASP.NET Core](https://learn.microsoft.com/aspnet/core/razor-pages/?view=aspnetcore-10.0)
- .NET, [官方 .NET 支援原則](https://dotnet.microsoft.com/zh-tw/platform/support/policy)

## Decision: 將 `cards.json` 升級為 schema v4，metadata 放在 card object

**決策**: `CardLibraryDocument.CurrentSchemaVersion` 升級為 4。root 仍包含 `schemaVersion`、`cards` 與 `drawHistory`。每張 `MealCard` 新增 optional `decisionMetadata`，包含 `tags`、`priceRange`、`preparationTimeRange`、`dietaryPreferences`、`spiceLevel`。讀取 v1/v2/v3 時在記憶體補齊 `decisionMetadata = null` 或 empty metadata；下一次成功寫入以 v4 原子保存。

**理由**: metadata 描述的是卡牌本身，不是獨立資料集；放在 card object 可讓 create/edit 與 metadata 更新同一原子寫入，避免 cards 與 metadata 脫鉤。升級為 v4 可清楚區分「已支援 metadata schema」與 004 的 v3 drawHistory schema，同時讓舊卡缺 metadata 不被誤判為 corrupted。

**Alternatives considered**:

- 維持 schema v3 並默默加入 optional 欄位：技術可行，但版本語意不清楚，後續 validation 與支援回溯較難審查。
- 另建 `card-metadata.json`：跨檔案一致性會變成兩階段問題，寫入失敗時難以維持完整成功或完整失敗。
- 將 metadata 寫入 drawHistory：metadata 是目前卡牌篩選條件，不應變成成功抽卡歷史的一部分；history 仍只保存成功抽卡事實。

## Decision: metadata 缺漏代表「未知」，只在未套用該條件時通過

**決策**: 舊卡牌或使用者未填某項 metadata 時，該欄位以 null/empty 表示未知。未套用對應條件時，卡牌仍可瀏覽、搜尋、編輯、刪除與抽卡；一旦使用者套用該欄位條件，缺漏該欄位的卡牌不得視為符合。

**理由**: 規格 P3 要求舊卡不因缺 metadata 失效，但 FR-014 明確要求缺少某項決策資訊時，不可在使用者套用該條件時被視為符合。此規則避免把未知資料誤導成符合條件，也讓使用者可以逐步補齊卡牌資訊。

**Alternatives considered**:

- 缺漏 metadata 一律排除：會破壞未篩選抽卡與舊資料可用性。
- 缺漏 metadata 一律視為符合：會讓篩選結果不可信，特別是價格、時間與辣度限制。
- 導入「未知也包含」的進階選項：第一版 scope 複雜度過高，可由未來規格評估。

## Decision: 第一版固定精簡選項，不導入數值價格或時間

**決策**: 第一版使用分類 enum，而不是自由數值：

- `PriceRange`: `Low`、`Medium`、`High`
- `PreparationTimeRange`: `Quick`、`Standard`、`Long`
- `DietaryPreference`: `Vegetarian`、`Light`、`HeavyFlavor`、`TakeoutFriendly`
- `SpiceLevel`: `None`、`Mild`、`Medium`、`Hot`

**理由**: 使用者要求的是精簡決策欄位。分類值可降低表單負擔、避免數字單位/幣別/店家變動問題，也便於雙語顯示與資料驗證。價格區間只是使用者手動描述大致花費，並非價值分級、稀有度或加權依據。

**Alternatives considered**:

- 實際金額與分鐘數：會引入幣別、區間邊界、排序與輸入驗證複雜度，第一版不需要。
- 外部店家價格與等待時間：明確超出規格，且需外部 API 或爬取。
- 使用者自訂 enum 選項：會增加 localization、validation 與遷移複雜度；第一版以固定選項交付。

## Decision: 自訂標籤以 trim + case-insensitive 去重，篩選採 all-tags match

**決策**: 儲存前將每個 tag trim，移除空白 tag，並以 `OrdinalIgnoreCase` 去除同一卡牌內重複 tag。保存時保留第一次出現的顯示文字。搜尋/篩選時，使用者選取多個 tag 代表卡牌必須同時具有所有 tag。

**理由**: FR-015 要求避免大小寫或空白差異造成同一卡牌內重複標籤；邊界情況要求多個標籤預設同時符合全部標籤。保留第一次出現文字可避免任意改變使用者輸入的大小寫或語言。

**Alternatives considered**:

- tag OR matching：結果較寬，與規格預設不符。
- 強制轉小寫保存：會破壞使用者想顯示的英文大小寫。
- 全站 tag 字典：第一版不需要獨立管理介面，可先由現有卡牌集合推導可選 tag。

## Decision: 首頁抽卡與卡牌庫搜尋共用 `CardFilterCriteria`

**決策**: 新增 `CardFilterCriteria`，包含 meal type、price range、preparation time、dietary preferences、max spice level、selected tags 與 current language。首頁正常模式會在 criteria 中保留 meal type；隨機模式忽略 meal type。卡牌庫則可同時使用 keyword、meal type 與 metadata criteria。

**理由**: 首頁與卡牌庫必須使用一致的篩選規則，否則使用者在卡牌庫看到的符合條件卡牌，可能與抽卡候選池不同。共用模型和 service 可讓單元測試直接覆蓋交集規則、缺漏 metadata 規則、tag all-match 與 empty result。

**Alternatives considered**:

- PageModel 各自組條件：容易分歧並把業務規則放入 UI 協調層。
- 為首頁與卡牌庫建立兩套 criteria：重複欄位多，後續新增條件會同步困難。

## Decision: 篩選服務先建立候選池，再保持均勻 randomizer

**決策**: `DrawCandidatePoolBuilder` 或等效服務先依 draw mode 建立 base pool：Normal 用所選餐別 active cards，Random 用全部 active cards；再交由 `MealCardFilterService` 套用 metadata criteria。最後仍以 `MealCardRandomizer.NextIndex(pool.Count)` 從篩選後 pool 中均勻選 index。

**理由**: 005 的核心資料完整性要求是篩選只能縮小候選池，不可改變權重。把篩選和 randomizer 分離，測試可直接驗證 pool membership 與 `1/N` nominal probability，不讓 metadata、歷史統計、語系或 display order 進入權重。

**Alternatives considered**:

- 在 randomizer 中加入 metadata 條件：會混合 membership 與 randomness，公平性測試較難。
- 以 metadata 給偏好權重或排序：明確違反 FR-020 與 DI-001。
- 前端先篩再送卡牌 ID：會讓 client state 成為資料完整性來源，不可接受。

## Decision: UI 使用 Razor forms + query string 保留篩選狀態

**決策**: 首頁抽卡表單 POST 送出 `drawMode`、`mealType`、`coinInserted`、`drawOperationId` 與 metadata filter fields。卡牌庫使用 GET query string 保存 keyword、meal type 與 filter fields，並提供清除條件入口。語系/主題切換後必須盡可能保留 query/form 狀態；JS 只做 progressive enhancement，不決定結果。

**理由**: Razor Pages 表單和 query string 與既有 app 一致，可自然支援 Anti-Forgery、server validation、localization 與 browser back/forward。卡牌庫篩選是可分享/可回復狀態，適合 GET query；抽卡是 state-changing operation，必須 POST。

**Alternatives considered**:

- 將所有 filter state 放在 session/localStorage：server render、可分享 URL 與測試性較差。
- AJAX 動態篩選：第一版不需要；也會擴大公開介面或 client/server state 同步面。

## Decision: runtime 文案補齊 resource keys，不在服務中硬編碼 UI 語言

**決策**: 新增 metadata label、option、validation、empty candidate、filter summary 與 clear filters 的 `SharedResource.zh-TW.resx` / `SharedResource.en-US.resx` keys。服務層回傳 stable message key 或 status；PageModel/view 依 current culture 呈現文字。

**理由**: 003 已核准 runtime 雙語 UI，005 新增的所有可見文字都必須雙語完整，不得顯示未翻譯 key。服務回傳 key 可維持測試穩定性，避免業務規則直接耦合特定語系。

**Alternatives considered**:

- 服務直接回傳中文或英文字串：測試與 localization fallback 較脆弱，且容易漏翻。
- 只補繁中：違反既有 bilingual governance 與 005 FR-018。

**參考來源**:

- Microsoft Learn, [Globalization and localization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)
- Microsoft Learn, [Static files in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/map-static-files?view=aspnetcore-10.0)
