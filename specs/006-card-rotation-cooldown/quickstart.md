# 快速入門: 餐點輪替防重複抽卡

## 先決條件

- .NET 10 SDK，已確認本機可用版本為 `10.0.100`
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

實作前先新增並執行輪替防重複相關失敗測試：

```bash
dotnet test CardPicker2.sln --filter RotationCooldown
dotnet test CardPicker2.sln --filter RotationSnapshot
dotnet test CardPicker2.sln --filter DrawIdempotency
dotnet test CardPicker2.sln --filter FilteredDraw
dotnet test CardPicker2.sln --filter DrawStatistics
```

預期在尚未實作時，新增測試會失敗，至少覆蓋：

- 首頁預設啟用「避免最近重複」且 N=3。
- 新首頁 GET 或應用程式重新啟動後，防重複設定回到預設啟用與 N=3；先前本次抽卡設定不作為持久偏好保存。
- N 有效範圍固定為 0..10。
- 無效 N 值拒絕抽卡，不新增成功 history，不改變 statistics。
- N=0 或關閉防重複時，候選池與 005 metadata filtered draw 規則一致。
- Normal mode 先依餐別建立 base pool，再套用 metadata filters，再套用近期排除。
- Random mode 忽略餐別，從全部 active cards 套用 metadata filters，再套用近期排除。
- 最近 N 筆成功 history 依成功時間新到舊排序，時間相同時持久化順序較後者較新。
- 最近 N 筆中同一 card ID 多次出現時只排除一次。
- 最近 N 筆中 deleted card 或不在本次候選池的 card 不會排除其他 active cards。
- 近期排除後候選池包含 M 張時，每張標稱機率為 `1/M`。
- 005 pool 本來為空時，顯示既有空卡池或無符合條件訊息。
- 005 pool 非空但 rotation 後為空時，顯示降低 N、關閉防重複或調整條件的提示。
- rotation empty 不新增 `DrawHistoryRecord`，總成功抽卡次數與單卡抽中次數不變。
- 成功抽卡保存 `RotationSnapshot`，包含是否啟用、N、輪替前候選池數、排除數與輪替後候選池數。
- 同一 `DrawOperationId` 重送時 replay 原 card ID 與原 snapshot，不重算候選池。
- 缺少 `RotationSnapshot` 的既有成功 history 不 blocked、不回填、不影響統計與最近 N 次排除。
- `zh-TW` 與 `en-US` 防重複控制、結果摘要、空候選池提示與 validation message 都完整。
- production HSTS/CSP 與 Anti-Forgery 保護仍通過。

## 執行網站

```bash
dotnet run --project CardPicker2/CardPicker2.csproj
```

開啟終端機輸出的本機網址，例如 `https://localhost:5001` 或 `http://localhost:5000`。

## 預設防重複成功抽卡驗證

1. 準備午餐 active cards 至少 5 張，並讓其中 2 張出現在最近 3 筆成功 history 中。
2. 進入首頁 `/`。
3. 選擇「正常模式」與午餐。
4. 保持「避免最近重複」啟用與 N=3。
5. 完成 coin/confirmation 後啟動抽卡。
6. 預期成功結果只來自未出現在最近 3 筆成功 history 的午餐候選卡牌。
7. 預期結果摘要顯示輪替前候選池 5 張、排除 2 張、輪替後候選池 3 張或依 fixture 對應數字，且只顯示排除數量，不顯示被排除卡牌名稱。
8. 預期總成功抽取次數增加 1，抽中 card 的統計增加 1。

## 關閉防重複與 N=0 驗證

1. 使用同一組有近期 history 的資料。
2. 在首頁關閉「避免最近重複」後啟動抽卡。
3. 預期候選池與既有 005 metadata filtered draw 規則一致，不額外排除近期 card。
4. 將「避免最近重複」重新啟用但 N 設為 0。
5. 預期候選池仍與 005 規則一致。
6. 成功結果可保存 snapshot，且 snapshot 顯示未套用或 N=0 的摘要。

## 防重複設定不持久化驗證

1. 在首頁關閉「避免最近重複」並將 N 改為 7。
2. 完成一次成功或失敗抽卡後，重新以不帶表單狀態的新 GET 開啟首頁。
3. 預期防重複控制回到預設啟用，N 回到 3。
4. 重新啟動應用程式後再次開啟首頁。
5. 預期防重複控制仍回到預設啟用，N 仍為 3。
6. 預期 `cards.json` 只保存成功抽卡歷史中的 `rotationSnapshot`，不得保存跨頁面或跨重新啟動的防重複偏好。

## 隨機模式與 metadata filters 驗證

1. 在首頁選擇「隨機模式」。
2. 套用一組 metadata filters，例如低價位、蔬食、不辣。
3. 保持防重複啟用與 N=3。
4. 啟動抽卡。
5. 預期系統不要求餐別，先從早餐、午餐、晚餐全部 active cards 套用 metadata filters。
6. 預期只排除同時存在於 filtered pool 與最近 N 筆 history 中的 card IDs。
7. 預期結果顯示抽中卡牌實際餐別、metadata 摘要與輪替摘要。

## 防重複後空候選池驗證

1. 準備一組資料，使 005 base/metadata pool 有 3 張 active cards。
2. 讓這 3 張都出現在最近 3 筆成功 history 中。
3. 以防重複啟用、N=3 啟動抽卡。
4. 預期不顯示成功結果。
5. 預期顯示目前語系的防重複造成空候選池提示，包含降低 N、關閉防重複或調整條件等下一步。
6. 預期 `drawHistory` 不新增紀錄，總成功抽取次數與卡牌抽中次數不變。
7. 將 N 從 3 改為 1 後再次抽卡。
8. 若輪替後 pool 非空，預期可完成公平抽卡。

## 原始候選池為空驗證

1. 套用一組確定沒有卡牌符合的餐別或 metadata 條件。
2. 保持防重複啟用。
3. 啟動抽卡。
4. 預期顯示既有空卡池或無符合條件訊息。
5. 預期不得誤導為近期防重複造成。
6. 預期不新增 history 或 statistics。

## 重複提交與 snapshot replay 驗證

1. 完成一次成功抽卡後記錄 `DrawOperationId`、result card ID、rotation snapshot counts、總成功抽取次數與該卡抽中次數。
2. 使用瀏覽器重新整理造成 form resubmission，或以測試重送同一 POST payload。
3. 預期畫面重顯同一 result card ID。
4. 預期輪替摘要使用原 snapshot，不因最新 history 或目前 N 值重算。
5. 預期總成功抽取次數與該卡抽中次數不再次增加。
6. 預期日誌記錄 replay，而不是新的 draw success。

## 舊 history 缺少 snapshot 驗證

1. 使用測試隔離資料檔準備 schema v4 document，含 `drawHistory` 但舊紀錄沒有 `rotationSnapshot`。
2. 載入網站或執行 integration test。
3. 預期資料不被視為 corrupted。
4. 預期舊 history 仍納入總成功抽卡次數、單卡抽中次數與最近 N 次排除。
5. 重顯舊結果時，預期顯示目前語系的「此筆舊紀錄未保存輪替摘要」或等效狀態。
6. 預期系統不得為舊 history 寫入推測 snapshot。

## deleted card 與 card edit 驗證

1. 讓某張近期抽中的 card 被刪除並 retained as deleted。
2. 啟動新抽卡。
3. 預期 deleted card 不進入 005 base/metadata pool。
4. 若該 deleted card 出現在最近 N 筆 history 中，只作為歷史事實，不會排除其他 active cards。
5. 編輯一張近期抽中的 active card 名稱、描述、翻譯或 metadata。
6. 再次抽卡時，預期近期排除仍依相同 card ID 生效，不受顯示文字變更影響。

## 語系、主題與狀態保留驗證

對 `zh-TW` 與 `en-US` 都執行：

1. 在首頁設定 metadata filters、防重複啟用狀態與 N 值。
2. 切換語系。
3. 預期尚未提交的防重複設定與 filters 盡可能保留。
4. 完成一次成功抽卡後切換語系。
5. 預期 result card ID、history、statistics 與 rotation snapshot 不變，只改變 labels/options/summary display text。
6. 切換主題。
7. 預期防重複設定、operation ID、候選池語意、結果與 statistics 不變。
8. 預期語系/主題切換使用的 transient state 不會成為跨重新啟動偏好；新首頁 GET 仍回到預設啟用與 N=3。

## Reduced Motion 與 RWD 驗證

對 `zh-TW` 與 `en-US`，在下列 viewport 檢查首頁：

- Mobile: 390x844
- Tablet: 768x1024
- Desktop: 1366x768

檢查項目：

- 防重複控制、N input、metadata filters、slot visual、結果摘要、空候選池提示與統計表不重疊、不水平溢出。
- `document.documentElement.scrollWidth == document.documentElement.clientWidth`。
- `prefers-reduced-motion: reduce` 時不播放連續旋轉，仍揭示有效靜態結果或空候選池提示。
- 鍵盤可操作模式、餐別、metadata filters、防重複 toggle、N input、coin、start 與 card management。
- 目前防重複狀態、validation state 與 empty reason 不只依賴顏色。

## 效能、coverage 與可用性 smoke 驗證

```bash
dotnet test CardPicker2.sln --filter "RotationCooldownPerformance|WebVitals"
dotnet test CardPicker2.sln --collect:"XPlat Code Coverage"
```

預期：

- 以至少 150 張 active cards 與 1,000 筆成功 draw history 的本機 JSON fixture 驗證首頁 GET、metadata + rotation filtered draw POST、statistics projection 與 `/Cards` filtered search p95 < 200ms。
- 使用者觸發防重複抽卡後，主要內容在 1 秒內更新為結果或可理解提示。
- browser automation 或手動紀錄顯示 FCP < 1.5 秒、LCP < 2.5 秒。
- coverage report 中本功能涉及的 critical business logic，包含 `CardPicker2/Models/` 與 `CardPicker2/Services/` 的 rotation/filter/draw/history/persistence 路徑，覆蓋率達 80% 以上；若未達標，必須在 `plan.md` 記錄例外、風險與補救計畫。

## 安全與觀察性驗證

```bash
dotnet test CardPicker2.sln --filter SecurityHeaders
dotnet test CardPicker2.sln --filter AntiForgery
dotnet test CardPicker2.sln --filter Logging
```

預期：

- Production 環境保留 HTTPS redirection、HSTS 與 CSP。
- `POST /?handler=Draw`、create/edit/delete、language/theme forms 需要 Anti-Forgery。
- invalid N、rotation applied、empty after rotation、draw success、replay、write failure 與 blocked state 有結構化日誌。
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
