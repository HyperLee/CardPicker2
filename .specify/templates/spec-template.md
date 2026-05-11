# 功能規格: [FEATURE NAME]

**功能分支**: `[###-feature-name]`
**建立日期**: [DATE]
**狀態**: Draft
**輸入**: User description: "$ARGUMENTS"

## 使用者情境與測試 *(mandatory)*

<!--
  IMPORTANT: 使用者故事 MUST 依業務價值排序。
  每個故事 MUST 可獨立測試、獨立展示，且單獨完成時仍能交付有價值的 MVP 增量。

  使用 P1、P2、P3 標示優先級，P1 最高。
  所有使用者面向內容 MUST 使用繁體中文 zh-TW。
-->

### 使用者故事 1 - [Brief Title] (Priority: P1)

[以使用者語言描述此旅程]

**優先級理由**: [說明此故事的價值與為何排序在此]

**獨立測試**: [描述如何單獨驗證此故事，例如可透過特定操作得到特定價值]

**驗收情境**:

1. **Given** [初始狀態]，**When** [動作]，**Then** [預期結果]
2. **Given** [初始狀態]，**When** [動作]，**Then** [預期結果]

---

### 使用者故事 2 - [Brief Title] (Priority: P2)

[以使用者語言描述此旅程]

**優先級理由**: [說明此故事的價值與為何排序在此]

**獨立測試**: [描述如何單獨驗證此故事]

**驗收情境**:

1. **Given** [初始狀態]，**When** [動作]，**Then** [預期結果]

---

### 使用者故事 3 - [Brief Title] (Priority: P3)

[以使用者語言描述此旅程]

**優先級理由**: [說明此故事的價值與為何排序在此]

**獨立測試**: [描述如何單獨驗證此故事]

**驗收情境**:

1. **Given** [初始狀態]，**When** [動作]，**Then** [預期結果]

---

[依需要增加更多使用者故事，每個故事都 MUST 有優先級與獨立測試方式]

### 邊界情況

<!--
  ACTION REQUIRED: 填入與本功能相關的邊界情況。
  涉及卡牌選取時 MUST 覆蓋數量、範圍、空集合、重複選取與無效輸入。
-->

- 當 [boundary condition] 發生時，系統如何回應？
- 當 [error scenario] 發生時，使用者看到什麼可操作訊息？
- 如果輸入數量超出可用卡牌範圍，系統 MUST 如何阻止無效狀態？
- 如果操作失敗，系統 MUST 如何避免留下中間狀態？

## 需求 *(mandatory)*

<!--
  ACTION REQUIRED: 需求 MUST 具體、可測試，並避免實作細節。
-->

### 功能需求

- **FR-001**: System MUST [specific capability]
- **FR-002**: System MUST validate [specific input or state]
- **FR-003**: Users MUST be able to [key interaction]
- **FR-004**: System MUST preserve [data integrity rule]
- **FR-005**: System MUST log [security, validation, or key user event]

*不清楚的需求 MUST 標示並在 clarify 或 research 階段解決：*

- **FR-006**: System MUST [NEEDS CLARIFICATION: 未指定的決策]

### 資料完整性需求 *(include if feature involves cards, selection, or persisted data)*

- **DI-001**: System MUST reject [invalid range, count, duplicate, or impossible state]
- **DI-002**: System MUST keep [state transition] atomic and consistent
- **DI-003**: System MUST make [result] reproducible when a seed or deterministic mode is used

### 非功能需求

- **NFR-001**: User-facing text and documentation MUST use Traditional Chinese zh-TW
- **NFR-002**: Primary pages MUST meet documented performance budgets or define a justified alternative
- **NFR-003**: Forms that change state MUST use input validation and Anti-Forgery protection
- **NFR-004**: Key events and failures MUST be observable through structured logs
- **NFR-005**: UI changes MUST remain responsive and target WCAG 2.1 AA accessibility

### 關鍵實體 *(include if feature involves data)*

- **[Entity 1]**: [代表什麼、主要屬性，不包含實作細節]
- **[Entity 2]**: [代表什麼、與其他實體的關係]

## 成功標準 *(mandatory)*

<!--
  ACTION REQUIRED: 成功標準 MUST 可量測、技術無關，並能由使用者或業務結果驗證。
-->

### 可量測結果

- **SC-001**: [例如：使用者能在 2 分鐘內完成主要流程]
- **SC-002**: [例如：無效輸入 100% 被阻止並顯示可操作錯誤訊息]
- **SC-003**: [例如：90% 使用者首次嘗試即可完成主要任務]
- **SC-004**: [例如：主要頁面在目標環境達成指定效能預算]

## 假設

<!--
  ACTION REQUIRED: 填入在需求未指定時採用的合理預設。
-->

- [目標使用者假設]
- [範圍邊界假設]
- [資料或環境假設]
- [既有系統或服務相依性]
