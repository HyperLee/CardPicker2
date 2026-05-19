# CardPicker2

CardPicker2 是一個單機單人的本機餐點抽卡網站。使用者可以用老虎機式流程在早餐、午餐、晚餐之間抽出餐點，也可以用隨機模式從全部有效卡牌中抽選，並透過價格、等待時間、飲食偏好、辣度與標籤等條件縮小候選池。

專案採用 ASP.NET Core Razor Pages 與單一本機 JSON 檔保存卡牌庫，適合在本機瀏覽器中管理常吃、想吃或需要排除的餐點選項。

## 功能

- 老虎機式餐點抽卡流程：餐別選擇、投幣確認、拉桿啟動、轉動狀態與結果揭示。
- 正常模式與隨機模式：正常模式依餐別抽卡，隨機模式從全部有效卡牌抽卡。
- 條件篩選抽卡：支援價格區間、準備或等待時間、飲食偏好、最高可接受辣度與標籤。
- 卡牌庫管理：瀏覽、搜尋、查看詳情、新增、編輯與刪除餐點卡牌。
- 卡牌決策資訊：每張卡牌可維護標籤、價格、時間、飲食偏好與辣度。
- 抽卡歷史與統計：顯示總成功抽取次數、單卡抽中次數、歷史機率與卡牌狀態。
- 雙語執行時介面：支援 `zh-TW` 與 `en-US`，繁體中文為預設語系。
- 主題模式：首頁可選亮色、暗黑或跟隨系統，並套用到全站。
- Reduced motion 支援：使用者偏好減少動態效果時，略過連續旋轉並顯示靜態揭示結果。

## 技術棧

- ASP.NET Core Razor Pages，目標框架 `net10.0`
- C#，啟用 Nullable Reference Types 與 Implicit Usings
- Bootstrap 5、jQuery、jQuery Validation
- ASP.NET Core Localization 與 `.resx` resources
- `System.Text.Json` 本機 JSON 持久化
- Serilog console/file logging
- xUnit、Moq、`Microsoft.AspNetCore.Mvc.Testing`
- Playwright 與 AxeCore browser/accessibility 驗證

## 快速開始

先確認已安裝 .NET 10 SDK，然後在 repository 根目錄執行：

```bash
dotnet restore CardPicker2.sln
dotnet build CardPicker2.sln
dotnet test CardPicker2.sln
dotnet run --project CardPicker2/CardPicker2.csproj
```

網站啟動後，開啟終端機輸出的本機網址，例如 `https://localhost:5001` 或 `http://localhost:5000`。

## 使用方式

首頁 `/` 是主要抽卡入口：

1. 選擇正常模式或隨機模式。
2. 正常模式需選擇早餐、午餐或晚餐；隨機模式不需要餐別。
3. 視需要套用價格、時間、飲食偏好、辣度或標籤條件。
4. 勾選投幣確認並啟動抽卡。
5. 查看抽出的餐點、決策資訊摘要與抽卡統計。

卡牌庫 `/Cards` 可用關鍵字、餐別與決策條件搜尋。新增與編輯卡牌時，可同時維護繁體中文與英文餐點內容，以及可選的決策資訊。

## 專案結構

```text
CardPicker2/
├── Program.cs                 # DI、Localization、Serilog、HSTS/CSP、Razor Pages
├── Models/                    # 卡牌、抽卡、統計、語系、metadata 與篩選模型
├── Services/                  # JSON 持久化、抽卡、公平候選池、搜尋、統計與驗證
├── Pages/                     # Razor Pages 首頁、卡牌庫、語系與 shared layout
├── Resources/                 # zh-TW / en-US runtime UI 文案
├── data/cards.json            # 本機卡牌庫資料檔
└── wwwroot/                   # CSS、JavaScript 與第三方前端資源

tests/
├── CardPicker2.UnitTests/
└── CardPicker2.IntegrationTests/

specs/
├── 001-casino-meal-picker/
├── 002-theme-mode-toggle/
├── 003-bilingual-language-toggle/
├── 004-draw-mode-statistics/
└── 005-card-metadata-filtered-draw/
```

## 資料與安全

卡牌庫儲存在 `CardPicker2/data/cards.json`。如果資料檔不存在，系統會以預設種子卡牌建立；如果資料檔無法讀取、JSON 損壞、schema 不支援或驗證失敗，系統會保留原檔並封鎖會改變資料的操作，避免覆蓋使用者資料。

寫入採同目錄暫存檔與原子替換流程。抽卡結果、歷史紀錄、卡牌更新與 metadata 更新都必須完整成功或完整失敗。

所有狀態變更表單使用 ASP.NET Core Anti-Forgery 保護。非開發環境保留 HTTPS redirection、HSTS 與 Content Security Policy。使用者可見錯誤與日誌不得包含秘密值、完整資料檔、stack trace 或內部提示內容。

## 開發流程

目前功能脈絡以 `specs/005-card-metadata-filtered-draw/` 為主要來源，並建立在前序功能之上：

- `001-casino-meal-picker`: 初始餐點抽卡、卡牌庫與本機 JSON。
- `002-theme-mode-toggle`: 全站亮色、暗黑與跟隨系統主題。
- `003-bilingual-language-toggle`: `zh-TW` / `en-US` runtime UI 與雙語餐點內容。
- `004-draw-mode-statistics`: 正常/隨機模式、成功抽卡歷史與統計。
- `005-card-metadata-filtered-draw`: 餐點決策資訊與條件篩選抽卡。

專案文件與交付文件使用繁體中文。程式碼識別字可使用英文；runtime UI 預設繁體中文，並支援英文顯示。
