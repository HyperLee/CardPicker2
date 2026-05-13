# 快速入門: 抽卡模式與機率統計

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

實作前先新增並執行抽卡模式與統計相關失敗測試：

```bash
dotnet test CardPicker2.sln --filter DrawMode
dotnet test CardPicker2.sln --filter DrawHistory
dotnet test CardPicker2.sln --filter DrawStatistics
dotnet test CardPicker2.sln --filter DrawIdempotency
dotnet test CardPicker2.sln --filter DeletedCardStatistics
```

預期在尚未實作時，新增測試會失敗，至少覆蓋：

- Normal mode 未選餐別會被拒絕，且不新增 history。
- Normal mode 只從選定餐別 active cards 抽出結果。
- Random mode 不需餐別，且從全部 active cards 抽出結果。
- Random mode 忽略畫面殘留 meal type。
- 候選池含 N 張 cards 時，每張標稱機率為 1/N。
- empty pool、blocked library、validation failure、missing coin 與 write failure 不新增 history。
- 首次成功抽卡剛好新增一筆 `DrawHistoryRecord`。
- 同一 `DrawOperationId` 快速連點、重新整理或重送時 replay 原 result，history 不增加。
- schema v1/v2 讀取後可在記憶體映射為 v3；unsupported/corrupted JSON 保留並 block。
- 成功歷史跨應用程式重啟保留。
- 統計總成功次數、卡牌抽中次數與歷史機率依公式計算。
- 無成功歷史時顯示空狀態，不顯示誤導性每卡 0%。
- active 但未抽中的卡牌在已有歷史時顯示 0 次與 0%。
- 曾抽中後刪除的卡牌保留統計列並標示 deleted，且不再進候選池。
- 卡牌改名或翻譯更新後，history 仍歸屬同一 card ID。
- 語系切換、動畫、顯示排序與 reduced motion 不改變結果或統計。
- production HSTS/CSP 與 Anti-Forgery 保護仍通過。

## 執行網站

```bash
dotnet run --project CardPicker2/CardPicker2.csproj
```

開啟終端機輸出的本機網址，例如 `https://localhost:5001` 或 `http://localhost:5000`。

## Normal Mode 手動驗證

1. 進入首頁 `/`。
2. 選擇「正常模式」。
3. 不選餐別，完成 coin/confirmation 後嘗試啟動抽卡。
4. 預期系統拒絕抽卡，顯示目前語系 validation message，總成功抽取次數不變。
5. 選擇午餐並完成 coin/confirmation。
6. 啟動抽卡。
7. 預期成功結果只來自午餐 active cards。
8. 預期總成功抽取次數增加 1，該 card 的抽中次數增加 1。

## Random Mode 手動驗證

1. 在首頁選擇「隨機模式」。
2. 不選餐別，完成 coin/confirmation。
3. 啟動抽卡。
4. 預期系統可從早餐、午餐與晚餐全部 active cards 中抽出一張。
5. 若畫面仍顯示之前的餐別，預期該餐別不限制 Random mode 候選池。
6. 預期結果顯示抽卡模式、抽中卡牌餐別、名稱與描述。

## 重複提交驗證

1. 完成一次成功抽卡後記錄 `DrawOperationId`、result card ID、總成功抽取次數與該卡抽中次數。
2. 使用瀏覽器重新整理造成 form resubmission，或以測試重送同一 POST payload。
3. 預期畫面重顯同一 result card ID。
4. 預期總成功抽取次數與該卡抽中次數不再次增加。
5. 預期日誌記錄 replay，而不是新的 draw success。

## 統計表驗證

有成功歷史時：

1. 進入首頁。
2. 預期顯示總成功抽取次數。
3. 預期統計表包含所有 active cards。
4. 對每列檢查 `歷史機率 = 該卡抽中次數 / 總成功抽卡次數`。
5. 尚未抽中的 active cards 應顯示 0 次與 0%。

無成功歷史時：

1. 使用測試隔離資料檔或乾淨 v3 document 啟動網站。
2. 進入首頁。
3. 預期顯示可理解空狀態。
4. 預期不顯示每張卡牌 0% 作為歷史分布。

## 刪除後歷史保留驗證

1. 讓某張 active card 至少成功抽中一次。
2. 前往該卡牌刪除確認頁。
3. 送出刪除。
4. 預期該卡不再出現在未來 Normal/Random candidate pool。
5. 進入首頁統計表。
6. 預期該卡仍出現在統計列，狀態顯示為已刪除。
7. 預期總成功抽取次數與該卡歷史機率不因刪除而改變。

## 改名與翻譯更新驗證

1. 選擇一張已有成功歷史的 active card。
2. 編輯其名稱、描述或英文翻譯。
3. 回到首頁統計表。
4. 預期統計列使用更新後可見文字。
5. 預期抽中次數與歷史機率仍歸屬同一 card ID。
6. 切換語系後重複檢查，預期只改變顯示文字，不改變統計。

## 缺檔與 corrupted 檔案驗證

缺檔情境只能刪除單一檔案，不得使用 bulk deletion 指令：

```bash
rm "CardPicker2/data/cards.json"
```

預期下次載入會建立 seed data，每個餐別至少 3 張 active cards，且 `drawHistory` 為空。

Corrupted 情境應使用測試隔離資料夾：

1. 建立不可解析 JSON 或 unsupported schema version。
2. 執行 load/search/draw/create/edit/delete/statistics 測試。
3. 預期 operations 被 block，原檔保留。
4. 預期 recovery message 依目前語系呈現。
5. 預期日誌不包含完整 corrupted JSON 內容。

## Reduced Motion 與 RWD 驗證

對 `zh-TW` 與 `en-US`，在下列 viewport 檢查首頁：

- Mobile: 390x844
- Tablet: 768x1024
- Desktop: 1366x768

檢查項目：

- 模式選擇、餐別選擇、coin/start、slot visual、結果卡與統計表不重疊、不水平溢出。
- `document.documentElement.scrollWidth == document.documentElement.clientWidth`。
- `prefers-reduced-motion: reduce` 時不播放連續旋轉，仍揭示有效靜態結果。
- 鍵盤可操作模式切換、餐別、coin、start 與卡牌管理流程。
- 目前 mode、deleted status 與 validation state 不只依賴顏色。

## 安全與觀察性驗證

```bash
dotnet test CardPicker2.sln --filter SecurityHeaders
dotnet test CardPicker2.sln --filter AntiForgery
dotnet test CardPicker2.sln --filter Logging
```

預期：

- Production 環境保留 HTTPS redirection、HSTS 與 CSP。
- `POST /?handler=Draw` 需要 Anti-Forgery。
- draw success、repeat replay、validation failure、blocked state、write failure 與 delete retention 有結構化日誌。
- 日誌與 UI 不包含秘密值、完整 JSON、stack trace 或系統提示。

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
- 所有新增或變更的公開 API 都有 XML 文件註解，且每個公開 API 註解都包含 `<example>` 與 `<code>`。
- runtime UI 在 `zh-TW` 與 `en-US` 下不混用未核准語系或未翻譯 key。
