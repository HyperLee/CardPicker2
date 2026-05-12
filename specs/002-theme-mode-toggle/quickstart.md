# 快速入門: 網站主題模式切換

## 先決條件

- .NET 10 SDK
- 可執行本機 ASP.NET Core Razor Pages 專案的瀏覽器，建議 Chrome、Firefox、Safari 或 Edge
- 若執行 browser automation 測試，安裝 Microsoft Playwright for .NET 所需 Chromium、Firefox、WebKit 瀏覽器；Safari/Edge 若無法自動化，須以手動驗證紀錄補足
- repository root: `/Users/qiuzili/CardPicker2`

## Playwright Browser Automation 安裝

本功能的 integration tests 使用 `Microsoft.Playwright` 與 `Deque.AxeCore.Playwright`。若本機尚未下載 Playwright browser runtime，可在 repository root 執行：

```bash
/Users/qiuzili/.nuget/packages/microsoft.playwright/1.59.0/.playwright/node/darwin-arm64/node /Users/qiuzili/.nuget/packages/microsoft.playwright/1.59.0/.playwright/package/cli.js install chromium firefox webkit
```

自動化 browser matrix 覆蓋 Chromium、Firefox、WebKit engine。Safari 與 Edge 品牌差異無法由 Playwright for .NET 直接自動化時，需以手動驗證補足：開啟首頁、切換三種模式、前往 `/Cards` 與 `/Privacy`、確認 1 秒內套用一致主題且非首頁不顯示主題控制項。

## 還原與建置

```bash
dotnet restore CardPicker2.sln
dotnet build CardPicker2.sln
```

預期結果：

- restore 成功。
- build 成功且沒有新增警告。

## 測試優先工作流

實作前先建立失敗測試：

```bash
dotnet test CardPicker2.sln --filter ThemeMode
```

預期在尚未實作時，新增的主題測試會失敗，至少覆蓋：

- 首頁輸出「亮色模式」、「暗黑模式」、「跟隨系統」三個選項。
- `/Privacy`、`/Cards`、卡牌詳情與卡牌管理頁不輸出主題選擇控制項。
- Layout 在 stylesheet 前具有初始主題套用機制。
- production CSP/HSTS 仍存在。
- 無效 theme mode 會回到 `system`。
- localStorage 讀取、寫入與 storage event 失敗會安全 fallback，且 console 診斷只包含允許的非敏感事件名稱。
- 切換主題不清除抽卡結果、搜尋條件或未送出表單。

Browser automation 測試應覆蓋：

- localStorage 無值時預設 `data-theme-mode="system"`。
- localStorage 為 `dark` 時首次可見呈現前套用 `data-bs-theme="dark"`。
- 選擇 light/dark/system 後 1 秒內更新目前頁面。
- 首頁主題控制可用鍵盤與 mobile pointer/touch 完成選擇。
- system 模式跟隨 browser color scheme。
- system preference 或另一分頁變更後 2 秒內同步。
- 手機、平板與桌面寬度在三種模式下無水平溢出，焦點狀態可見，automated axe 或等效可及性 smoke 檢查無重大違規。
- Chromium、Firefox、WebKit 均執行核心主題行為測試；Safari/Edge 若無法自動化，須記錄手動驗證結果。

## 執行網站

```bash
dotnet run --project CardPicker2/CardPicker2.csproj
```

開啟終端機輸出的本機網址，例如 `https://localhost:5001` 或 `http://localhost:5000`。

## 首頁主題控制驗證

1. 進入首頁 `/`。
2. 確認可看到「網站主題」或等效群組。
3. 確認只有三個選項：「亮色模式」、「暗黑模式」、「跟隨系統」。
4. 使用滑鼠、觸控或鍵盤逐一切換三個選項。
5. 預期每次切換後，首頁 1 秒內套用一致有效主題。
6. 預期目前選取模式有清楚可見狀態。
7. 預期餐別選擇、投幣確認、拉桿按鈕與老虎機視覺區仍可操作。
8. 預期鍵盤焦點指示在亮色與暗黑有效主題下都清楚可見，且 mobile viewport 可用 pointer/touch 操作切換。

## 全站套用驗證

1. 在首頁選擇「暗黑模式」。
2. 前往 `/Cards`、任一卡牌詳情頁、`/Cards/Create`、`/Cards/Edit/{id}`、`/Privacy`。
3. 預期每個頁面皆套用暗黑外觀。
4. 預期首頁以外頁面不顯示主題選擇控制項。
5. 回到首頁選擇「亮色模式」並重複上述頁面。
6. 預期所有頁面皆套用亮色外觀。

## 回訪與 localStorage 驗證

在同一瀏覽器與裝置：

1. 在首頁選擇「暗黑模式」。
2. 關閉分頁或重新整理。
3. 重新開啟網站。
4. 預期首次可見呈現前即套用暗黑外觀，不先閃現亮色外觀。
5. 在 DevTools Application/Storage 檢查 `cardpicker.theme.mode` 值為 `dark`。

無效值驗證：

1. 在 DevTools 將 `cardpicker.theme.mode` 改為 `invalid`。
2. 重新整理網站。
3. 預期選取模式回到「跟隨系統」，頁面不中斷。

localStorage 例外驗證：

1. 使用 browser automation 模擬 `localStorage.getItem` 或 `setItem` 丟出例外。
2. 預期頁面不中斷，當前頁面使用安全預設或使用者剛選擇的外觀。
3. 若 console 有診斷，僅允許 `CardPickerThemePreferenceReadFailed` 或 `CardPickerThemePreferenceWriteFailed` 等事件名稱，不得包含原始偏好值、完整例外、stack trace、秘密值或系統提示。

## 跟隨系統驗證

1. 在首頁選擇「跟隨系統」。
2. 將瀏覽器或作業系統外觀偏好切到暗黑。
3. 預期網站 2 秒內改為暗黑有效主題。
4. 將外觀偏好切回亮色。
5. 預期網站 2 秒內改為亮色有效主題。
6. 改選「亮色模式」或「暗黑模式」。
7. 再次切換系統外觀偏好。
8. 預期網站維持使用者明確選擇的有效主題。

## 跨分頁同步驗證

1. 開啟兩個同站分頁。
2. 分頁 A 停在首頁，分頁 B 停在 `/Cards` 或 `/Privacy`。
3. 在分頁 A 選擇「暗黑模式」。
4. 預期分頁 B 在 2 秒內同步為暗黑外觀，且不顯示主題選擇控制項。
5. 在分頁 A 改選「亮色模式」。
6. 預期分頁 B 在 2 秒內同步為亮色外觀。
7. 若分頁 B 有搜尋條件、表單輸入或可見抽卡結果，預期同步後仍保留。
8. 若 storage event 處理失敗且 console 有診斷，僅允許 `CardPickerThemeSyncFailed` 等非敏感事件名稱。

## 資料完整性驗證

主題切換不得改變餐點資料：

1. 記錄 `CardPicker2/data/cards.json` 修改時間與內容摘要。
2. 在首頁與卡牌頁切換三種主題多次。
3. 執行搜尋、查看詳情，並在未送出表單時切換主題。
4. 預期 `cards.json` 未因主題切換而修改。
5. 預期搜尋條件、未送出欄位與 validation message 不被清除。

不要使用 bulk deletion 指令。若其他測試需要缺檔情境，只能刪除單一檔案：

```bash
rm "CardPicker2/data/cards.json"
```

## 可及性與 responsive 驗證

對 light、dark、system 三種選取模式，在下列 viewport 檢查首頁與主要頁面：

- Mobile: 390x844
- Tablet: 768x1024
- Desktop: 1366x768

檢查項目：

- 無文字、按鈕、卡牌、表單或老虎機元素重疊。
- `document.documentElement.scrollWidth == document.documentElement.clientWidth`。
- 導覽、主題控制、餐別選擇、投幣、拉桿、搜尋、建立、編輯、刪除確認都可用鍵盤操作。
- 主題控制在 mobile viewport 可用 pointer/touch 操作。
- 焦點指示在 light/dark 有效主題下都可見。
- 文字與互動元件對比符合 WCAG 2.2 AA。
- automated axe 或等效可及性 smoke 檢查沒有重大違規；若工具無法完整量測對比，驗收紀錄必須包含人工 contrast/focus 檢查結果。

## 自動化測試

實作完成後執行：

```bash
dotnet test CardPicker2.sln
```

若 browser automation 測試需要先安裝瀏覽器，依 Microsoft Playwright for .NET 套件指示安裝後再執行測試。測試不得依賴外部網路服務。

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
- 主題切換後 1 秒內套用一致有效主題。
- 同站已開啟分頁 2 秒內同步。
- 目標瀏覽器環境中，回訪 dark preference 時不出現可見 light-to-dark 閃爍。
- Chromium、Firefox、WebKit 核心主題行為測試通過；Safari/Edge 若無法自動化，手動驗證紀錄已補齊。

品質檢查：

- 使用者可見文字皆為繁體中文。
- production 環境保留 HSTS 與 CSP。
- CSP 明確允許必要 head bootstrap script，且不得移除 `default-src 'self'`。
- 日誌與 console 診斷不包含秘密值、連線字串、完整資料檔內容、stack trace、系統提示或未清理的 localStorage 值，且 localStorage/sync 失敗只輸出允許的非敏感事件名稱。
- 主題切換後，餐點抽卡、瀏覽、搜尋、建立、編輯與刪除流程仍可操作。

## 實作驗證紀錄（2026-05-12）

自動化驗證命令：

```bash
dotnet format CardPicker2.sln --verify-no-changes
dotnet build CardPicker2.sln
dotnet test CardPicker2.sln
```

主題 browser automation 覆蓋：

- Chromium、Firefox、WebKit：核心 light/dark 切換。
- Chromium：localStorage 保存與無效值 fallback、read/write/sync 安全 warning、system preference change、跨分頁 storage event 同步、抽卡/搜尋/建立表單狀態不變、390x844/768x1024/1366x768 responsive overflow、鍵盤焦點、mobile touch/pointer 與 axe serious/critical smoke。
- Axe `color-contrast` 對 CSS-variable-backed Bootstrap segmented controls 產生 false positive，因此 automated smoke 以 axe 排除該單一規則，並另外以 computed color/background contrast 驗證主題控制與導覽連結達 4.5:1 以上。

手動 fallback 待驗證紀錄格式：

```text
Browser: Safari 或 Edge
Date:
Tester:
Viewport:
Steps: 首頁切換 light/dark/system，前往 /Cards 與 /Privacy
Result: 通過/未通過
Notes: 對比、焦點、是否水平溢出、是否非首頁無主題控制項
```
