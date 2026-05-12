# 快速入門: 雙語語系切換

## 先決條件

- .NET 10 SDK
- 可執行本機 ASP.NET Core Razor Pages 專案的瀏覽器，建議 Chrome、Firefox、Safari 或 Edge
- repository root: `/Users/qiuzili/CardPicker2`
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

實作前先新增並執行語系相關失敗測試：

```bash
dotnet test CardPicker2.sln --filter Language
dotnet test CardPicker2.sln --filter Localization
dotnet test CardPicker2.sln --filter Bilingual
```

預期在尚未實作時，新增測試會失敗，至少覆蓋：

- 無 culture cookie 時預設繁體中文。
- `en-US` cookie 會讓首頁、卡牌庫、詳情、建立、編輯、刪除確認、隱私權與錯誤頁呈現英文。
- unsupported cookie 值回到繁體中文。
- shared layout 每個主要頁面都有語系切換入口與目前語系狀態。
- `POST /Language?handler=Set` 需要 Anti-Forgery，且只接受 `zh-TW`/`en-US`。
- DataAnnotations、ModelState、服務結果與 recovery message 依目前語系呈現。
- schema v1 `cards.json` 可載入為繁中內容 + 英文缺漏 fallback。
- 缺檔建立 schema v2 雙語 seed data。
- corrupted/unsupported JSON 被 preserve 並 block operations。
- 新增/編輯缺少任一語系 name/description 會失敗。
- 任一語系 duplicate name+description 同餐別會失敗。
- 搜尋依目前語系 visible name 比對；英文缺漏時以 fallback name 比對。
- 抽卡結果 card ID 與 meal type 不因語系切換改變。
- 語系切換保留搜尋條件、未送出表單輸入、validation state、刪除確認與已揭示結果。
- production HSTS/CSP 仍通過。

## 執行網站

```bash
dotnet run --project CardPicker2/CardPicker2.csproj
```

開啟終端機輸出的本機網址，例如 `https://localhost:5001` 或 `http://localhost:5000`。

## 首次造訪與預設語系驗證

1. 清除本站 `.AspNetCore.Culture` cookie。
2. 進入首頁 `/`。
3. 預期 `<html lang="zh-Hant">`。
4. 預期導覽、按鈕、餐別、投幣/確認、拉桿、狀態文字與卡牌資訊皆為繁體中文。
5. 前往 `/Cards`、`/Privacy` 與 `/Error`。
6. 預期所有主要頁面仍為繁體中文，且 shared layout 顯示目前語系。

## 切換到英文驗證

1. 在任一主要頁面使用 shared layout 語系切換入口選擇英文。
2. 預期提交 `POST /Language?handler=Set`，包含 Anti-Forgery token。
3. 預期 response 設定 `.AspNetCore.Culture`，值代表 `c=en-US|uic=en-US`。
4. 預期回到原頁後 `<html lang="en">`。
5. 預期導覽、按鈕、功能名稱、餐別、表單 label、狀態、成功/失敗訊息與目前可見餐點資訊皆為英文。
6. 前往首頁、卡牌庫、詳情、建立、編輯、刪除確認、隱私權與錯誤頁。
7. 預期每個頁面都維持英文，且語系切換入口仍可見。

## 切回繁體中文驗證

1. 在英文模式選擇繁體中文。
2. 預期 `.AspNetCore.Culture` 改為 `c=zh-TW|uic=zh-TW`。
3. 預期回到原頁後 `<html lang="zh-Hant">`。
4. 預期所有可見 UI 與餐點內容回到繁體中文。

## 無效 cookie 驗證

1. 使用 DevTools 將 `.AspNetCore.Culture` 改為 unsupported 或 malformed value。
2. 重新整理首頁。
3. 預期網站不中斷，並回到繁體中文。
4. 預期不顯示 stack trace、cookie 原始值、系統提示或完整內部資料。

## 搜尋驗證

繁體中文模式：

1. 前往 `/Cards`。
2. 輸入繁體中文餐點名稱關鍵字。
3. 預期結果只依繁中 visible name 比對。
4. 切到英文後，預期搜尋條件保留，但結果依英文 visible name 重新比對。

英文模式：

1. 輸入英文餐點名稱關鍵字。
2. 預期結果只依英文 visible name 比對。
3. 若卡牌缺少英文內容，預期以繁中 fallback name 比對並顯示 `Needs English translation` 或等效英文提示。
4. 無結果時，預期無結果訊息為英文。

## 抽卡驗證

1. 在首頁選擇一個餐別並完成 coin/confirmation。
2. 觸發 draw。
3. 記錄揭示結果的 card ID 與 meal type。
4. 切換語系。
5. 預期顯示同一 card ID 與同一 meal type，只改變名稱、描述與餐別顯示語言。
6. 預期不重新觸發 randomizer，不出現另一張卡牌。
7. 重複於繁體中文與英文模式各完成一次。

## 卡牌新增與編輯驗證

1. 前往 `/Cards/Create`。
2. 嘗試只填繁中 name/description 並送出。
3. 預期拒絕儲存，validation message 使用目前語系，且 JSON 不變。
4. 填完整繁中與英文 name/description、meal type 後送出。
5. 預期新增成功，成功訊息使用目前語系。
6. 在兩種語系下搜尋、查看詳情與抽卡，預期同一張卡牌顯示對應語系內容。
7. 編輯該卡牌，嘗試改成同餐別另一張卡牌任一語系相同的 normalized name+description。
8. 預期 duplicate validation 失敗，原卡牌不變。

## 既有 schema v1 與英文缺漏驗證

1. 使用測試隔離檔案建立 schema v1 `cards.json`，內容包含 `name`、`description` 與 `mealType`。
2. 啟動測試伺服器或執行相關 integration test。
3. 預期檔案可載入，不被視為 corrupted。
4. 繁體中文模式顯示原始 `name`/`description`。
5. 英文模式顯示同一繁中內容作為 fallback，且出現補齊英文翻譯提示。
6. 編輯並補齊英文後送出。
7. 預期寫入 schema v2，保留原 card ID 與 meal type。

## 缺檔與 corrupted 檔案驗證

缺檔情境只能刪除單一檔案，不得使用 bulk deletion 指令：

```bash
rm "CardPicker2/data/cards.json"
```

預期下次載入會建立 schema v2 雙語 seed data，每個餐別至少 3 張。

Corrupted 情境應使用測試隔離資料夾：

1. 建立不可解析 JSON 或 unsupported schema version。
2. 執行 load/search/draw/create/edit/delete 測試。
3. 預期 operations 被 block，原檔保留。
4. 預期 recovery message 依目前語系呈現。
5. 預期日誌不包含完整 corrupted JSON 內容。

## 狀態保留驗證

對繁中與英文互切都執行：

1. 在 `/Cards` 輸入搜尋條件後切換語系。
2. 預期 keyword 與 meal type query 保留。
3. 在 `/Cards/Create` 輸入但不送出欄位後切換語系。
4. 預期欄位值不清除，label 與 validation message 使用新語系。
5. 在 edit page 造成 validation error 後切換語系。
6. 預期錯誤狀態仍可見且可操作。
7. 在 delete confirmation page 切換語系。
8. 預期仍確認同一 card ID。
9. 在首頁已揭示 draw result 後切換語系。
10. 預期同一 card ID 的 result 留在畫面上。

## 可及性與 responsive 驗證

對 `zh-TW` 與 `en-US`，在下列 viewport 檢查首頁與主要頁面：

- Mobile: 390x844
- Tablet: 768x1024
- Desktop: 1366x768

檢查項目：

- 無文字、按鈕、卡牌、表單、badge、語系切換或老虎機元素重疊。
- `document.documentElement.scrollWidth == document.documentElement.clientWidth`。
- 語系切換、餐別選擇、投幣、拉桿、搜尋、建立、編輯、刪除確認可用鍵盤操作。
- 目前語系與 fallback 狀態不只依賴顏色。
- 焦點指示在亮色/暗黑主題與兩語系下都可見。
- Automated axe 或等效可及性 smoke 檢查沒有重大違規；若工具無法完整量測對比，驗收紀錄需包含人工 contrast/focus 檢查。

## 自動化測試

實作完成後執行：

```bash
dotnet test CardPicker2.sln
```

若 browser automation 測試需要先安裝瀏覽器 runtime，依測試套件指示安裝後再執行。測試不得依賴外部翻譯服務或外部網路。

## 效能與品質驗收

建議在實作完成後執行：

```bash
dotnet build CardPicker2.sln
dotnet test CardPicker2.sln
dotnet run --project CardPicker2/CardPicker2.csproj
```

手動或 browser automation 量測：

- 首頁 FCP < 1.5 秒。
- 首頁 LCP < 2.5 秒。
- 語系切換與頁面重新呈現 < 1 秒。
- Page handler/API p95 < 200ms。
- 單一請求記憶體 < 100MB。

品質檢查：

- 規格、計畫、研究、資料模型、快速入門與任務文件皆為繁體中文。
- runtime UI 在 `zh-TW` 與 `en-US` 下都沒有未翻譯 key、空白餐點名稱或空白描述。
- production 環境保留 HSTS 與 CSP。
- 所有 state-changing forms 有 Anti-Forgery。
- 日誌與 console 診斷不包含秘密值、連線字串、完整資料檔內容、stack trace、系統提示或未清理輸入。
