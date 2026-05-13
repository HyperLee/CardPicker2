# 快速入門: 餐點條件篩選抽卡

## 先決條件

- .NET 10 SDK，已確認本機可用版本為 `10.0.100`
- 可執行本機 ASP.NET Core Razor Pages 專案的瀏覽器，建議 Chrome、Firefox、Safari 或 Edge
- repository root: `/Users/qiuzili/CardPicker2`
- JSON 資料檔: `CardPicker2/data/cards.json`

## 還原與建置

```bash
dotnet restore CardPicker2.sln
dotnet build CardPicker2.sln
```

預期結果：

- restore 成功。
- build 成功且沒有新增警告。

## 測試優先工作流

實作前先新增並執行餐點 metadata 與條件篩選相關失敗測試：

```bash
dotnet test CardPicker2.sln --filter Metadata
dotnet test CardPicker2.sln --filter CardFilter
dotnet test CardPicker2.sln --filter FilteredDraw
dotnet test CardPicker2.sln --filter FilteredSearch
dotnet test CardPicker2.sln --filter SchemaV4
```

預期在尚未實作時，新增測試會失敗，至少覆蓋：

- schema v3 `cards.json` 可載入為 v4 in-memory document，且舊卡 `DecisionMetadata` 缺漏不 blocked。
- unsupported schema、corrupted JSON 與 invalid metadata enum 仍 preserve 原檔並 block。
- tag trim、移除空白、case-insensitive 去重並保留第一次顯示文字。
- create/edit metadata 與雙語 card content 同一原子寫入。
- metadata 更新不改變 card ID、draw history、statistics 或 deleted state。
- duplicate detection 不包含 metadata。
- 未套用 metadata filters 時，缺 metadata 的 active cards 仍可被 Normal/Random 抽出。
- 套用 price/time/diet/spice/tag filter 時，缺對應欄位的 cards 不符合。
- Normal mode 先依餐別建立 base pool，再套用 filters。
- Random mode 忽略 meal type 並從全部 active cards 套用 filters。
- 多個 dietary preferences 與 tags 採全部符合。
- max spice level 採小於等於規則。
- filtered pool 為空時不新增 `DrawHistoryRecord`，統計不變。
- filtered pool 包含 N 張卡時，每張標稱機率為 `1/N`。
- `/Cards` keyword、meal type 與 metadata filters 採交集。
- `zh-TW` 與 `en-US` metadata labels、options、empty states、validation messages 都完整。
- production HSTS/CSP 與 Anti-Forgery 保護仍通過。

## 執行網站

```bash
dotnet run --project CardPicker2/CardPicker2.csproj
```

開啟終端機輸出的本機網址，例如 `https://localhost:5001` 或 `http://localhost:5000`。

## 首頁 filtered draw 手動驗證

1. 進入首頁 `/`。
2. 選擇「正常模式」與午餐。
3. 選擇低價位、快速、不辣，並選擇至少一個 tag。
4. 完成 coin/confirmation 後啟動抽卡。
5. 預期成功結果只來自午餐 active cards，且同時符合所有篩選條件。
6. 預期結果顯示抽中卡牌的 metadata 摘要。
7. 預期總成功抽取次數增加 1，且統計表只因成功 draw 更新。

## 隨機模式 filtered draw 驗證

1. 在首頁選擇「隨機模式」。
2. 不選餐別，套用「蔬食」與最高辣度「小辣」。
3. 啟動抽卡。
4. 預期候選池來自早餐、午餐與晚餐全部 active cards，再套用蔬食與辣度條件。
5. 預期畫面殘留 meal type 不會限制 Random mode。
6. 預期結果顯示抽中卡牌實際餐別與 metadata。

## 空候選池驗證

1. 套用一組確定沒有卡牌符合的條件。
2. 完成 coin/confirmation 後啟動抽卡。
3. 預期顯示目前語系的空候選池訊息。
4. 預期不顯示成功結果。
5. 預期 `drawHistory` 不新增紀錄，總成功抽取次數與卡牌抽中次數不變。

## 卡牌庫篩選驗證

1. 前往 `/Cards`。
2. 輸入 keyword，選擇 meal type，再套用 price/time/diet/spice/tags。
3. 預期結果只顯示同時符合所有條件的 active cards。
4. 預期目前已套用條件清楚顯示。
5. 點選清除條件。
6. 預期 keyword、meal type 與 metadata filters 都清空，且仍保留目前語系與主題。

## 卡牌 metadata 維護驗證

1. 前往 `/Cards/Create`。
2. 填完整繁中與英文 name/description、meal type，並填入 metadata。
3. 送出後重新開啟 card details。
4. 預期 metadata 摘要完整顯示。
5. 回到 `/Cards` 以剛才 metadata 篩選，預期可找到該卡。
6. 編輯該卡，只更新 metadata。
7. 預期 card ID 不變，既有 draw history 與 statistics 不切分。
8. 嘗試送出 invalid metadata enum 或空白/重複 tags。
9. 預期 validation message 使用目前語系，且原資料不被局部修改。

## 舊資料與 schema v4 驗證

1. 使用測試隔離檔案準備 schema v3 document，包含 `cards` 與 `drawHistory` 但沒有 `decisionMetadata`。
2. 啟動測試伺服器或執行相關 integration test。
3. 預期資料可載入，不被視為 corrupted。
4. 未套用 metadata filters 時，舊卡仍可瀏覽、搜尋與抽卡。
5. 套用 metadata filters 時，舊卡因缺欄位不符合。
6. 任一次成功 create/edit/delete/draw 寫入後，預期 document 以 `schemaVersion = 4` 保存。

## 語系、主題與狀態保留驗證

對 `zh-TW` 與 `en-US` 都執行：

1. 在首頁套用多個 metadata filters。
2. 切換語系。
3. 預期 filter state 與已揭示 result card ID 保留，只改變 labels、options、metadata summary 語言。
4. 切換主題。
5. 預期 filters、result card ID、operation ID、候選池語意、draw history 與 statistics 不改變。
6. 在 `/Cards` 使用 query filters 後切換語系與主題。
7. 預期 query/filter state 保留，結果依目前語系投影。
8. 在 create/edit 表單輸入 metadata 但不送出時切換語系或主題。
9. 預期未送出欄位盡可能保留，validation message 使用目前語系。

## Reduced Motion 與 RWD 驗證

對 `zh-TW` 與 `en-US`，在下列 viewport 檢查首頁、卡牌庫、details 與 create/edit：

- Mobile: 390x844
- Tablet: 768x1024
- Desktop: 1366x768

檢查項目：

- filter panel、tag chips、metadata badges、卡牌列表、結果摘要與表單不重疊、不水平溢出。
- `document.documentElement.scrollWidth == document.documentElement.clientWidth`。
- `prefers-reduced-motion: reduce` 時不播放連續旋轉，仍揭示有效靜態結果。
- 鍵盤可操作模式、餐別、metadata filters、coin、start、search、clear filters 與 card management。
- 目前 filter、辣度、deleted status 與 validation state 不只依賴顏色。

## 效能、coverage 與可用性 smoke 驗證

```bash
dotnet test CardPicker2.sln --filter "MetadataFilterPerformance|WebVitals"
dotnet test CardPicker2.sln --collect:"XPlat Code Coverage"
```

預期：

- 以至少 150 張 active cards 與 1,000 筆 draw history 的本機 JSON fixture 驗證首頁 GET、filtered draw POST、`/Cards` filtered search 與 metadata projection p95 < 200ms。
- 主要內容在使用者觸發篩選或抽卡後 1 秒內更新。
- browser automation 或手動紀錄顯示 FCP < 1.5 秒、LCP < 2.5 秒。
- coverage report 中本功能涉及的 critical business logic，包含 `CardPicker2/Models/` 與 `CardPicker2/Services/` 的 metadata/filter/draw/search/persistence 路徑，覆蓋率達 80% 以上；若未達標，必須在 `plan.md` 記錄例外、風險與補救計畫。
- 首頁可用性 smoke checklist 至少 10 次預設情境操作中有 9 次以上能在 30 秒內套用至少一個條件並完成有效抽卡或看到可理解的空候選池提示。
- 卡牌庫可用性 smoke checklist 至少 10 次預設情境操作中有 9 次以上能在 30 秒內使用 metadata filters 找到符合條件的卡牌或看到可理解的無結果提示。

## 安全與觀察性驗證

```bash
dotnet test CardPicker2.sln --filter SecurityHeaders
dotnet test CardPicker2.sln --filter AntiForgery
dotnet test CardPicker2.sln --filter Logging
```

預期：

- Production 環境保留 HTTPS redirection、HSTS 與 CSP。
- `POST /?handler=Draw`、create/edit/delete、language/theme forms 需要 Anti-Forgery。
- schema migration、metadata validation failure、empty filtered pool、filtered draw success、write failure 與 blocked state 有結構化日誌。
- 日誌與 UI 不包含秘密值、完整 JSON、stack trace、系統提示或未清理輸入。

## 完整驗收

實作完成後執行：

```bash
dotnet build CardPicker2.sln
dotnet test CardPicker2.sln
```

必要時再啟動網站進行手動或 browser automation 驗證：

```bash
dotnet run --project CardPicker2/CardPicker2.csproj
```

品質檢查：

- 規格、計畫、研究、資料模型、快速入門與任務文件皆為繁體中文。
- behavior changes 的測試先於實作存在。
- 無新增 build warning。
- 所有新增或變更的 public C# model/service API 都有 XML 文件註解，且每個註解都包含 `<example>` 與 `<code>`。
- runtime UI 在 `zh-TW` 與 `en-US` 下不混用未核准語系或未翻譯 key。
