# 快速入門: 餐點抽卡網站2

## 先決條件

- .NET 10 SDK
- 可執行本機 ASP.NET Core Razor Pages 專案的瀏覽器，建議 Chrome、Firefox、Safari 或 Edge
- repository root: `/Users/qiuzili/CardPicker2`

## 還原與建置

```bash
dotnet restore CardPicker2.sln
dotnet build CardPicker2.sln
```

預期結果：

- restore 成功。
- build 成功且沒有新增警告。

## 執行網站

```bash
dotnet run --project CardPicker2/CardPicker2.csproj
```

開啟終端機輸出的本機網址，例如 `https://localhost:5001` 或 `http://localhost:5000`。

## 首次啟動資料檢查

首次啟動時，若 `CardPicker2/data/cards.json` 不存在，系統必須建立預設卡牌庫。

驗證方式：

1. 啟動網站。
2. 進入卡牌列表。
3. 確認早餐、午餐、晚餐各至少有 3 張卡牌。
4. 關閉網站後重新啟動。
5. 確認同一份卡牌庫仍存在。

若要手動重跑缺檔情境，只能刪除單一檔案：

```bash
rm "CardPicker2/data/cards.json"
```

不要使用 bulk deletion 指令。

## 核心流程驗證

### 抽卡

1. 進入首頁。
2. 不選餐別直接投幣或開始抽卡。
3. 預期顯示「請先選擇早餐、午餐或晚餐。」
4. 選擇早餐。
5. 完成投幣。
6. 拉桿或按下開始抽卡。
7. 預期看到老虎機式轉動狀態，結束後揭示一張早餐卡牌。
8. 結果必須顯示餐點名稱、餐別與完整描述。

### 搜尋與瀏覽

1. 進入 `/Cards`。
2. 不輸入條件，預期顯示全部卡牌名稱與餐別。
3. 輸入名稱關鍵字，預期以大小寫不敏感部分比對。
4. 同時選擇餐別，預期只顯示同時符合名稱與餐別的卡牌。
5. 輸入不存在的關鍵字，預期顯示「查無符合條件的餐點卡牌。」

### 新增、編輯、刪除

1. 進入 `/Cards/Create`。
2. 不填必填欄位送出，預期顯示欄位錯誤且不新增資料。
3. 填入有效餐點名稱、餐別與描述送出。
4. 預期卡牌可在列表、搜尋與抽卡流程中使用。
5. 編輯該卡牌的名稱、餐別或描述。
6. 預期更新後內容立即反映在列表、詳情、搜尋與抽卡流程。
7. 嘗試新增或編輯成與另一張卡牌餐點名稱、餐別、描述完全相同的內容。
8. 預期儲存被拒絕，並顯示重複原因。
9. 刪除卡牌前必須看到確認操作。
10. 確認刪除後，該卡牌不得再出現在列表、搜尋、詳情或抽卡結果。

## 腐敗 JSON 復原情境

1. 停止網站。
2. 將 `CardPicker2/data/cards.json` 改成無法解析的 JSON，例如只留下 `{`。
3. 啟動網站。
4. 預期系統保留原檔，不覆寫成預設資料。
5. 預期首頁、列表與管理頁顯示阻斷復原錯誤，並停用新增、編輯、刪除與抽卡操作。
6. 修復 JSON 後重新啟動，預期卡牌庫恢復可用。

## Reduced Motion 驗證

在瀏覽器或作業系統啟用減少動態效果後：

1. 進入首頁。
2. 選擇餐別、投幣並開始抽卡。
3. 預期不播放連續轉動動畫。
4. 預期仍顯示短暫靜態揭示狀態與有效抽卡結果。

## 自動化測試

實作階段需建立下列測試專案：

```bash
dotnet new xunit -o tests/CardPicker2.UnitTests
dotnet new xunit -o tests/CardPicker2.IntegrationTests
dotnet sln CardPicker2.sln add tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj
dotnet sln CardPicker2.sln add tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj
dotnet add tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj reference CardPicker2/CardPicker2.csproj
dotnet add tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj reference CardPicker2/CardPicker2.csproj
dotnet add tests/CardPicker2.UnitTests/CardPicker2.UnitTests.csproj package Moq
dotnet add tests/CardPicker2.IntegrationTests/CardPicker2.IntegrationTests.csproj package Microsoft.AspNetCore.Mvc.Testing
```

測試執行：

```bash
dotnet test CardPicker2.sln
```

測試至少應涵蓋：

- 預設種子資料每個餐別至少 3 張。
- 必填欄位與非法餐別被拒絕。
- 重複卡牌判斷會 trim 並忽略大小寫。
- 編輯成重複卡牌會失敗且保留原內容。
- JSON 檔案缺失時會建立預設資料。
- JSON 腐敗時保留原檔並阻斷操作。
- 搜尋支援名稱關鍵字、餐別與組合條件。
- 抽卡只從所選餐別現有卡牌等機率選出。
- 抽卡進行中避免重複提交。
- Anti-Forgery 與正式環境安全標頭存在。

## 效能與品質驗收

建議在實作完成後執行：

```bash
dotnet build CardPicker2.sln
dotnet test CardPicker2.sln
dotnet run --project CardPicker2/CardPicker2.csproj
```

手動量測：

- 首頁 FCP < 1.5 秒。
- 首頁 LCP < 2.5 秒。
- 搜尋互動回應 < 1 秒。
- Page handler/API p95 < 200ms。
- 90% 首次使用者應能在 30 秒內完成一次有效抽卡。
- 90% 首次使用者應能在 20 秒內透過瀏覽或搜尋找到目標卡牌。

品質檢查：

- 使用者可見文字皆為繁體中文。
- 桌面與行動寬度無文字、按鈕、卡牌或老虎機視覺元素重疊。
- 公開服務與模型具備 XML 文件註解；需要示例的 API 包含 `<example>` 或 `<code>`。
- 日誌不包含秘密值、連線字串或 JSON 檔案完整內容。
