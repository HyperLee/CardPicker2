<!--
Sync Impact Report
==================
Version Change: 1.0.0 -> 2.0.0
Change Type: MAJOR - runtime UI language governance changed from zh-TW-only to approved bilingual support
Modified Principles:
- III. 使用者體驗一致性 - allows approved runtime UI locales while preserving consistency and accessibility
- 開發工作流程 / 文件語言要求 - separates zh-TW project documents from runtime UI localization
Added Sections: none
Removed Sections: none
Templates requiring updates:
- ✅ .specify/templates/plan-template.md - still aligned with constitution gates
- ✅ .specify/templates/spec-template.md - still aligned with zh-TW project documentation and measurable requirements
- ✅ .specify/templates/tasks-template.md - still aligned with TDD and quality gates
- ✅ .specify/templates/checklist-template.md - still aligned with evidence checks
- ✅ docs/readme-template.md - still aligned with zh-TW README/documentation requirement
- ✅ .specify/templates/commands/*.md - directory not present, no update required
Follow-up TODOs: none
-->

# CardPicker2 憲章

## 核心原則

### I. 程式碼品質至上 (NON-NEGOTIABLE)

所有正式程式碼 MUST 清晰、可維護，並符合專案的 `.editorconfig`。
跨檔案使用的服務、模型與公開 API MUST 使用明確命名、Nullable Reference
Types、檔案範圍命名空間、模式匹配與 `is null`/`is not null` 等 C# 慣例。
公開服務與模型的非平凡行為 MUST 具備 XML 文件註解；需要示例才能正確使用的
API MUST 提供 `<example>` 或 `<code>`。錯誤處理 MUST 覆蓋邊界情況並回傳
可診斷的失敗資訊。任何新增警告、格式化違規或未說明的複雜度 MUST 在合併前修正。

**理由**: 穩定的程式碼品質會降低維護成本，讓後續功能能在可理解、可審查、
可安全重構的基礎上前進。

### II. 測試優先開發 (NON-NEGOTIABLE)

任何會改變行為、資料規則、驗證邏輯或使用者流程的變更 MUST 先定義失敗測試，
再進行實作。卡牌選取演算法、隨機化邏輯、組合產生、狀態轉換與結果顯示
MUST 有單元測試；頁面渲染、表單處理、驗證與資料存取邊界 MUST 有整合測試。
任務清單 MUST 將測試工作放在對應實作之前。若某項變更無法先寫測試，計畫文件
MUST 記錄原因、風險與替代驗證方式。

**理由**: 測試優先能驗證需求理解，提供重構安全網，並讓關鍵業務行為成為
可執行的文件。

### III. 使用者體驗一致性

所有使用者面向畫面與訊息 MUST 使用一致的設計語言、Bootstrap 5 元件與
`site.css` 自訂樣式。產品執行時 UI MUST 預設使用繁體中文 `zh-TW`；
若功能規格明確核准其他 runtime 語系，所有導覽、按鈕、表單、驗證訊息、
狀態、復原訊息與使用者可見資料 MUST 依目前支援語系完整呈現，且不得混用
未核准語系或顯示未翻譯 key。畫面 MUST 在桌面與行動尺寸下保持可操作、
文字不可重疊或溢出，表單驗證 MUST 提供即時且可操作的錯誤回饋。功能設計
MUST 以優先級排序的使用者故事交付，每個故事 MUST 能獨立展示與驗證。
互動式卡牌操作 MUST 減少不必要輸入，並提供清楚的選取狀態與結果回饋。
無障礙設計 MUST 以 WCAG 2.1 AA 作為最低目標。

**理由**: 一致且可及的體驗會降低使用者學習成本，減少錯誤操作與支援成本。

### IV. 效能與延展性

主要頁面與核心互動 MUST 有明確效能預算。預設目標為首次內容繪製小於 1.5 秒、
最大內容繪製小於 2.5 秒；若功能特性使目標不適用，計畫文件 MUST 說明替代
量測方式。I/O 密集操作 MUST 使用 async/await。靜態資源 MUST 透過
MapStaticAssets、WithStaticAssets、壓縮、快取或 CDN 等機制合理最佳化。
資源生命週期 MUST 被管理，涉及外部資源的型別 MUST 正確釋放或註冊生命週期。

**理由**: 效能問題越晚處理成本越高；在設計階段設定預算能避免使用者體驗
被後續功能逐步侵蝕。

### V. 可觀察性與監控

系統 MUST 使用結構化且可關聯的日誌記錄重要事件。安全事件、卡牌操作、
驗證失敗、不可復原錯誤與背景流程 MUST 具有足夠診斷資訊，但 MUST NOT 記錄
密碼、金鑰、連線字串或其他敏感資料。日誌層級 MUST 準確使用
Trace、Debug、Information、Warning、Error 與 Critical。生產部署 SHOULD
整合 Application Insights、Serilog sink 或同等遙測工具；若暫不導入，
計畫文件 MUST 說明替代監控方式。

**理由**: 可觀察性是生產問題診斷與風險控制的基礎，能在使用者受到嚴重影響前
發現異常。

### VI. 安全優先

安全性 MUST 內建於每個功能。所有使用者輸入 MUST 透過 Data Annotations、
FluentValidation 或等效方式驗證。Razor 輸出 MUST 保持 HTML 編碼，除非計畫
明確證明內容已安全淨化。所有狀態變更表單 MUST 使用 Anti-Forgery Token。
金鑰、連線字串與秘密值 MUST 使用 Secret Manager、環境變數或部署平台的秘密
管理機制，MUST NOT 存入原始碼、規格或日誌。生產環境 MUST 強制 HTTPS 與 HSTS，
並設定適當的 Content Security Policy。

**理由**: 安全缺陷的修復成本與信任損害通常遠高於預防成本；預設安全能降低
整體風險。

### VII. 資料完整性 (NON-NEGOTIABLE)

CardPicker2 的核心資料與選牌結果 MUST 保持正確、一致且可驗證。卡牌總數、
選取數量、範圍、排除條件與任何使用者輸入 MUST 進行邊界值驗證。選牌數量
MUST 不得超過可用牌數，狀態轉換 MUST 不得產生重複選取、遺失牌、負數數量
或其他無效狀態。若功能支援種子亂數，相同輸入與種子 MUST 產生相同結果，
並保留足夠資訊供除錯。多步驟操作 MUST 以原子方式完成，失敗時 MUST 不留下
中間狀態。

**理由**: 使用者信任建立在結果正確性上；選牌結果一旦不可解釋或不一致，
產品可信度會直接受損。

## 技術標準

### 技術堆疊

- **Framework**: ASP.NET Core 10.0，目標框架為 `net10.0`
- **語言**: C# 14，啟用 Nullable Reference Types 與 Implicit Usings
- **前端**: Razor Pages、Bootstrap 5、jQuery、jQuery Validation
- **靜態資源**: MapStaticAssets 與 WithStaticAssets
- **日誌**: 內建 `ILogger` 為最低標準，Serilog 或同等工具可依部署需求導入
- **測試**: xUnit、Moq 或同等單元測試工具；整合測試使用 WebApplicationFactory

### 專案結構

- **入口點**: `CardPicker2/Program.cs` 設定服務、Middleware 與 Razor Pages
- **頁面**: Razor Pages 放在 `CardPicker2/Pages/`
- **模型**: 領域模型與輸入模型放在 `CardPicker2/Models/`
- **服務**: 業務邏輯與選牌流程放在 `CardPicker2/Services/`
- **靜態資源**: CSS、JavaScript 與第三方資源放在 `CardPicker2/wwwroot/`
- **測試**: 單元與整合測試放在 `tests/` 下的對應測試專案
- **設定**: 使用 `appsettings.json` 與環境特定設定檔管理非秘密設定

### 資源管理

- 自訂 CSS MUST 優先放在 `CardPicker2/wwwroot/css/site.css`
- 頁面專屬樣式 SHOULD 使用 Razor CSS isolation
- 自訂 JavaScript MUST 優先放在 `CardPicker2/wwwroot/js/site.js`
- 第三方前端函式庫 MUST 透過 `CardPicker2/wwwroot/lib/` 或明確記錄的供應方式管理

## 開發工作流程

### 文件語言要求

所有專案文件與開發交付文件 MUST 使用繁體中文 zh-TW，包括功能規格、實作計畫、
研究文件、資料模型、快速入門指南、任務清單與 README。產品執行時的使用者
可見 UI、驗證訊息、錯誤訊息、成功訊息與復原訊息 MUST 使用目前核准的 runtime
語系；未選擇語系、語系偏好無效或語系偏好不可用時 MUST 使用繁體中文 `zh-TW`。
程式碼識別字可使用英文；程式碼註解可使用英文或中文，但 MUST 以可維護性為準。
Git commit 訊息 SHOULD 使用英文，除非團隊針對該工作流另有明確規範。

### 功能開發流程

1. **規格定義**: 在 `specs/NNN-feature-name/spec.md` 以繁體中文定義使用者故事、
   驗收標準、邊界情況與成功指標。
2. **計畫制定**: 產生 `plan.md`、`research.md`、`data-model.md` 與
   `quickstart.md`，並在憲章檢查中逐項說明符合情況。
3. **憲章檢查**: 驗證設計符合程式碼品質、測試優先、UX、效能、可觀察性、
   安全與資料完整性要求。
4. **測試先行**: 對每個改變行為的故事先撰寫失敗測試。
5. **實作**: 依使用者故事優先級完成最小可交付增量。
6. **驗證**: 執行單元測試、整合測試與必要的手動驗證。
7. **審查**: Pull Request MUST 提供測試證據與憲章合規說明。

### 品質閘門

每個可合併變更 MUST 滿足以下條件：

- 所有相關自動化測試通過
- 關鍵業務邏輯測試覆蓋率達 80% 以上，或在計畫中記錄合理例外
- 無新增編譯警告、格式化違規或 linter 錯誤
- 公開服務與模型的文件註解符合本憲章要求
- 安全檢查無未處理的高風險問題
- 憲章檢查中的例外均有明確理由與補救計畫

## 治理規則

### 憲章優先級

本憲章優先於其他專案慣例、範本與個人偏好。當規格、計畫、任務或程式碼審查
與本憲章衝突時，MUST 以本憲章為準，除非修訂憲章並完成版本更新。

### 修訂程序

修訂本憲章 MUST 包含提案文件、修訂理由、影響範圍、替代方案與遷移計畫。
重大修訂 MUST 經 2/3 團隊成員同意；若目前只有單一維護者，該維護者 MUST 在
修訂提交中記錄決策理由。任何修訂 MUST 同步檢查 Spec Kit 範本、README 指引、
代理指引與其他執行文件。

### 版本控制規則

- **MAJOR**: 移除或重新定義核心原則，或導入不相容治理變更
- **MINOR**: 新增原則、區段或實質擴充既有指導
- **PATCH**: 釐清文字、修正錯字或進行不改變治理語意的細化

### 合規審查

所有 Pull Request、規格審查與計畫審查 MUST 驗證憲章合規性。每季 SHOULD
進行一次憲章遵循審計；若專案處於單人維護狀態，審計可記錄在維護者工作筆記
或追蹤議題中。任何複雜度增加、測試例外或安全例外 MUST 有明確業務價值、
風險說明與後續補救計畫。

**Version**: 2.0.0 | **Ratified**: 2026-05-11 | **Last Amended**: 2026-05-13
