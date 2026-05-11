# 實作計畫: [FEATURE]

**分支**: `[###-feature-name]` | **日期**: [DATE] | **規格**: [link]
**輸入**: 來自 `/specs/[###-feature-name]/spec.md` 的功能規格

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## 摘要

[從功能規格萃取主要需求、使用者價值與技術方向。內容 MUST 使用繁體中文 zh-TW。]

## 技術背景

<!--
  ACTION REQUIRED: 以本專案的實際技術內容取代下列提示。
  未確定項目 MUST 標示 NEEDS CLARIFICATION，並在研究階段解決。
-->

**語言/版本**: ASP.NET Core 10.0 / C# 14 或 NEEDS CLARIFICATION
**主要相依性**: Razor Pages, Bootstrap 5, jQuery, jQuery Validation 或 NEEDS CLARIFICATION
**儲存方式**: [N/A、檔案、資料庫或 NEEDS CLARIFICATION]
**測試**: xUnit、Moq、WebApplicationFactory 或 NEEDS CLARIFICATION
**目標平台**: ASP.NET Core Web App 或 NEEDS CLARIFICATION
**專案類型**: Razor Pages web application
**效能目標**: FCP < 1.5 秒、LCP < 2.5 秒，或記錄替代量測方式
**限制條件**: zh-TW 文件、TDD、輸入驗證、資料完整性、安全與可觀察性
**規模/範圍**: [使用者數、頁面數、資料量、卡牌集合大小或 NEEDS CLARIFICATION]

## 憲章檢查

*GATE: Phase 0 research 前 MUST 通過；Phase 1 design 後 MUST 重新檢查。*

| Gate | 憲章要求 | 狀態 | 證據或例外理由 |
|------|----------|------|----------------|
| 文件語言 | 使用者面向文件 MUST 使用繁體中文 zh-TW | [PASS/FAIL] | [spec/plan/tasks evidence] |
| 程式碼品質 | 設計 MUST 符合 `.editorconfig`、C# 14 與可維護性要求 | [PASS/FAIL] | [files/patterns] |
| 測試優先 | 行為變更 MUST 先定義失敗測試 | [PASS/FAIL/WAIVED] | [test plan or waiver] |
| UX 一致性 | 使用 Bootstrap 5、`site.css`、RWD、可操作錯誤回饋與 WCAG 2.1 AA 目標 | [PASS/FAIL] | [UI approach] |
| 效能 | 主要頁面與互動 MUST 有效能預算與量測方式 | [PASS/FAIL] | [budget/measurement] |
| 可觀察性 | 關鍵事件、驗證失敗與錯誤 MUST 有結構化日誌 | [PASS/FAIL] | [logging plan] |
| 安全 | 輸入驗證、Anti-Forgery、秘密管理、HTTPS/HSTS/CSP MUST 被處理 | [PASS/FAIL] | [security controls] |
| 資料完整性 | 卡牌數量、範圍、狀態轉換與結果一致性 MUST 可驗證 | [PASS/FAIL] | [invariants/tests] |

**Complexity Review**: 任何 FAIL 或 WAIVED MUST 在「複雜度追蹤」中記錄理由、
替代方案與補救計畫。

## 專案結構

### 文件，本功能

```text
specs/[###-feature]/
├── plan.md              # 本檔案，由 /speckit-plan 產生
├── research.md          # Phase 0 輸出
├── data-model.md        # Phase 1 輸出
├── quickstart.md        # Phase 1 輸出
├── contracts/           # Phase 1 輸出，若適用
└── tasks.md             # Phase 2 輸出，由 /speckit-tasks 產生
```

### 原始碼，repository root

<!--
  ACTION REQUIRED: 依功能實際影響範圍調整下列樹狀結構。
  產出的 plan.md MUST 保留真實路徑，不得保留未使用的占位選項。
-->

```text
CardPicker2/
├── Program.cs
├── Pages/
│   ├── Shared/
│   └── [feature].cshtml
├── Models/
├── Services/
└── wwwroot/
    ├── css/
    ├── js/
    └── lib/

tests/
├── CardPicker2.UnitTests/
└── CardPicker2.IntegrationTests/
```

**結構決策**: [記錄本功能採用的實際目錄與理由]

## 複雜度追蹤

> **僅在憲章檢查有 FAIL 或 WAIVED 時填寫**

| 違反或例外 | 為何需要 | 被拒絕的較簡方案與原因 | 補救計畫 |
|------------|----------|------------------------|----------|
| [例如：延後整合測試] | [目前限制] | [為何不可先做] | [何時補上] |
