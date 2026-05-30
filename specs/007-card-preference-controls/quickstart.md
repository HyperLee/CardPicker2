# 快速入門: 餐點收藏與手動排除抽卡

## 先決條件

- .NET 10 SDK；CI/部署 SHOULD 使用最新 10.0.x patch
- 可執行本機 ASP.NET Core Razor Pages 專案的瀏覽器，建議 Chrome、Firefox、Safari 或 Edge
- repository root: `/Users/qiuzili/CardPicker2`
- JSON 資料檔: `CardPicker2/data/cards.json`
- 若執行 browser automation 測試，需安裝測試專案使用的 browser runtime

## 還原與建置

```bash
dotnet restore CardPicker2.sln
dotnet build CardPicker2.sln
```

預期結果：

- restore 成功。
- build 成功且沒有新增警告。

## 測試優先工作流

實作前先新增並執行偏好狀態相關失敗測試：

```bash
dotnet test CardPicker2.sln --filter CardPreference
dotnet test CardPicker2.sln --filter PreferenceMutation
dotnet test CardPicker2.sln --filter PreferenceFilteredDraw
dotnet test CardPicker2.sln --filter PreferenceFilteredSearch
dotnet test CardPicker2.sln --filter PreferenceResultAction
```

預期在尚未實作時，新增測試會失敗，至少覆蓋：

- schema v4 `cards.json` 可載入為 v5 in-memory document，且舊卡 preferences 缺漏不 blocked。
- 新增或由舊資料升級而來的卡牌預設未收藏且未排除抽卡。
- 偏好狀態跨重新載入頁面與應用程式重新啟動保留。
- 收藏與排除 target-state mutation 重複提交不反覆反轉。
- 偏好更新對不存在、已刪除或 blocked library card 失敗，且原資料不變。
- 偏好更新寫入失敗不得留下部分更新狀態。
- 收藏不改變候選池、randomizer、draw history、statistics 或 rotation snapshot。
- 排除抽卡先於 normal/random base pool、metadata filters 與 rotation cooldown 生效。
- 被排除卡牌不會出現在 normal mode、random mode、metadata filtered draw 或 rotation pre/post pool。
- 排除所有符合條件的候選卡時，不新增 history、不改變 statistics，並顯示取消排除、調整條件或新增卡牌等提示。
- 取消排除後，card 可在符合 mode/meal/metadata/rotation 規則時回到候選池。
- `/Cards` 預設仍顯示已排除 active cards，且以 badge 標示。
- `/Cards` favorite filter、drawable/excluded filter 與 keyword、meal type、metadata filters 採交集。
- 詳情頁顯示並可更新收藏與排除狀態。
- 首頁 result action 可收藏或排除剛抽中的 card，並保留 result card ID、history、statistics 與 snapshot。
- `zh-TW` 與 `en-US` 收藏/排除 labels、badges、filters、success/error、empty states 都完整。
- production HSTS/CSP 與 Anti-Forgery 保護仍通過。

## 執行網站

```bash
dotnet run --project CardPicker2/CardPicker2.csproj
```

開啟終端機輸出的本機網址，例如 `https://localhost:5001` 或 `http://localhost:5000`。

## schema v5 與舊資料驗證

1. 使用測試隔離資料檔準備 schema v4 document，包含 `cards`、`decisionMetadata`、`drawHistory` 與 optional `rotationSnapshot`，但沒有 `preferences`。
2. 載入網站或執行 integration test。
3. 預期資料可載入，不被視為 corrupted。
4. 預期每張 card 顯示未收藏且未排除抽卡。
5. 對其中一張 card 收藏或排除後成功寫入。
6. 預期 document 以 `schemaVersion = 5` 保存，且只新增偏好狀態，不改變 card ID、metadata、history 或 snapshot。

## 卡牌庫收藏與排除驗證

1. 前往 `/Cards`。
2. 對一張 active card 提交「收藏」target state。
3. 預期列表顯示目前語系的收藏 badge。
4. 套用收藏篩選。
5. 預期只顯示收藏且同時符合既有 keyword、meal type 與 metadata filters 的 active cards。
6. 對同一 card 提交「排除抽卡」target state。
7. 預期該 card 仍出現在預設 `/Cards` 列表，並清楚標示已排除。
8. 套用「已排除」篩選。
9. 預期只顯示被排除抽卡的 active cards。
10. 套用「可抽」篩選。
11. 預期不顯示被排除抽卡的 cards。

## 詳情頁偏好操作驗證

1. 前往被收藏或被排除卡牌的 details page。
2. 預期顯示目前收藏與排除狀態，狀態不只依賴顏色。
3. 提交取消收藏或取消排除 target state。
4. 預期狀態更新並跨重新整理保留。
5. 重複提交同一 target state。
6. 預期最終狀態不變，且不產生反向切換。

## 排除抽卡候選池驗證

1. 準備午餐 active cards 至少 3 張。
2. 將其中 1 張標記為排除抽卡。
3. 選擇正常模式與午餐，關閉或降低 rotation 設定以避免測試干擾。
4. 完成 coin/confirmation 後啟動抽卡多次或使用可控 randomizer integration/unit test。
5. 預期被排除 card 永遠不出現在候選池或結果中。
6. 切換到隨機模式並套用 metadata filters。
7. 預期被排除 card 仍不出現在 filtered pool 或結果中。
8. 取消排除後再次抽卡。
9. 預期該 card 只在符合 mode/meal/metadata/rotation 規則時回到等機率候選池，不保證立即抽中。

## 排除造成空候選池驗證

1. 準備一組資料，使 005/006 前置條件下有符合條件的 active cards。
2. 將所有符合條件的 cards 都標記為排除抽卡。
3. 啟動抽卡。
4. 預期不顯示成功結果。
5. 預期顯示目前語系的可修正提示，至少包含取消部分排除、調整篩選條件或新增卡牌。
6. 預期 `drawHistory` 不新增紀錄，總成功抽取次數與卡牌抽中次數不變。
7. 取消其中一張 card 的排除後再次抽卡。
8. 若 rotation/metadata 條件允許，預期可完成公平抽卡。

## 首頁結果區偏好操作驗證

1. 完成一次成功抽卡後，記錄 `DrawOperationId`、result card ID、rotation snapshot、總成功抽取次數與該 card 抽中次數。
2. 在結果區提交收藏該 card。
3. 預期畫面仍顯示同一 result card ID，並標示已收藏。
4. 預期 `drawHistory` 不新增紀錄，statistics 不變，rotation snapshot 不變。
5. 在結果區提交排除該 card。
6. 預期畫面仍顯示同一 result card ID，並標示已排除。
7. 使用瀏覽器重新整理或重送同一 preference POST。
8. 預期 preference 最終狀態等於 target state，不反轉，不新增 history。
9. 再次啟動新的抽卡。
10. 預期剛排除的 card 不進入新候選池。

## 收藏公平性驗證

1. 準備同一候選池內一張收藏 card 與一張未收藏 card。
2. 啟動 normal/random/metadata/rotation draw。
3. 使用 unit test 驗證候選池 membership 包含兩張 card 且 `NominalProbability = 1/N`。
4. 預期收藏狀態不改變候選池排序、randomizer range、history append 或 statistics。

## 語系、主題與狀態保留驗證

對 `zh-TW` 與 `en-US` 都執行：

1. 在 `/Cards` 套用 favorite filter、draw eligibility filter、metadata filters 與 keyword。
2. 切換語系。
3. 預期 query/filter state 保留，labels、badges、messages 依目前語系呈現。
4. 完成一次成功抽卡後，在結果區收藏或排除。
5. 切換語系與主題。
6. 預期 result card ID、history、statistics、rotation snapshot 與 preference state 不變，只改變 display text。
7. 在 create/edit 表單輸入但不送出時切換語系或主題。
8. 預期未送出欄位盡可能保留，且偏好狀態不被未送出表單覆蓋。

## Reduced Motion 與 RWD 驗證

對 `zh-TW` 與 `en-US`，在下列 viewport 檢查首頁、卡牌庫、details 與 create/edit：

- Mobile: 390x844
- Tablet: 768x1024
- Desktop: 1366x768

檢查項目：

- 收藏/排除控制、狀態 badge、卡牌庫偏好篩選、metadata filters、結果區操作、slot visual、空候選池提示與統計表不重疊、不水平溢出。
- `document.documentElement.scrollWidth == document.documentElement.clientWidth`。
- `prefers-reduced-motion: reduce` 時不播放連續旋轉，仍揭示有效靜態結果或可理解空候選池提示。
- 鍵盤可操作模式、餐別、metadata filters、防重複 toggle、偏好篩選、收藏/排除 buttons、coin、start 與 card management。
- 收藏、排除、可抽、validation state 與 empty reason 不只依賴顏色。

## 效能、coverage 與可用性 smoke 驗證

```bash
dotnet test CardPicker2.sln --filter "CardPreferencePerformance|WebVitals"
dotnet test CardPicker2.sln --collect:"XPlat Code Coverage"
```

預期：

- 以至少 150 張 active cards 與 1,000 筆 successful draw history 的本機 JSON fixture 驗證首頁 GET、preference-aware draw POST、preference update POST、statistics projection 與 `/Cards` filtered search p95 < 200ms。
- 使用者觸發偏好更新或抽卡後，主要內容在 1 秒內更新為結果或可理解提示。
- browser automation 或手動紀錄顯示 FCP < 1.5 秒、LCP < 2.5 秒。
- coverage report 中本功能涉及的 critical business logic，包含 `CardPicker2/Models/` 與 `CardPicker2/Services/` 的 preference/filter/draw/search/persistence 路徑，覆蓋率達 80% 以上；若未達標，必須在 `plan.md` 記錄例外、風險與補救計畫。

## 安全與觀察性驗證

```bash
dotnet test CardPicker2.sln --filter SecurityHeaders
dotnet test CardPicker2.sln --filter AntiForgery
dotnet test CardPicker2.sln --filter Logging
```

預期：

- Production 環境保留 HTTPS redirection、HSTS 與 CSP。
- `POST /?handler=Draw`、首頁 result preference forms、`/Cards` preference forms、create/edit/delete、language/theme forms 需要 Anti-Forgery。
- schema v5 load/migration、preference update、invalid target state、empty after preference exclusion、draw success、replay、write failure 與 blocked state 有結構化日誌。
- 日誌與 UI 不包含秘密值、完整 JSON、完整描述、tag list 原文、stack trace、系統提示或未清理輸入。

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
