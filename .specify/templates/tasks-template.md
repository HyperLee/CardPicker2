---

description: "Task list template for feature implementation"
---

# 任務清單: [FEATURE NAME]

**輸入**: `/specs/[###-feature-name]/` 的設計文件
**前置文件**: plan.md（必要）、spec.md（使用者故事必要）、research.md、data-model.md、contracts/

**測試**: 依 CardPicker2 憲章，任何行為、資料規則、驗證邏輯或使用者流程變更
MUST 先建立失敗測試。若測試被豁免，tasks.md MUST 引用 plan.md 的豁免理由。

**組織方式**: 任務依使用者故事分組，確保每個故事可獨立實作與測試。

## 格式: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行，必須是不同檔案且無相依衝突
- **[Story]**: 任務所屬使用者故事，例如 US1、US2、US3
- Description MUST 包含精確檔案路徑
- 使用者面向文件與任務描述 MUST 使用繁體中文 zh-TW

## 路徑慣例

- **Web app**: `CardPicker2/` 為 Razor Pages 應用程式
- **頁面**: `CardPicker2/Pages/`
- **模型**: `CardPicker2/Models/`
- **服務**: `CardPicker2/Services/`
- **靜態資源**: `CardPicker2/wwwroot/`
- **測試**: `tests/CardPicker2.UnitTests/` 與 `tests/CardPicker2.IntegrationTests/`

<!--
  ============================================================================
  IMPORTANT: 下列任務是示例，/speckit-tasks MUST 以 spec.md、plan.md、
  data-model.md 與 contracts/ 的真實內容取代。

  產出的 tasks.md MUST:
  - 依 P1、P2、P3 使用者故事分組
  - 在每個故事的實作前列出測試任務
  - 包含輸入驗證、資料完整性、安全、可觀察性與文件工作
  - 不得保留示例任務
  ============================================================================
-->

## Phase 1: Setup，共用基礎

**目的**: 建立功能所需的專案與測試基礎。

- [ ] T001 確認或建立功能所需目錄結構
- [ ] T002 確認測試專案與 WebApplicationFactory 設定
- [ ] T003 [P] 設定或更新 linting、formatting 與測試執行指令

---

## Phase 2: Foundational，阻塞性前置工作

**目的**: 完成所有使用者故事共用且會阻塞後續工作的基礎。

**CRITICAL**: 此階段完成前不得開始任何使用者故事實作。

- [ ] T004 建立共用模型或輸入 DTO
- [ ] T005 [P] 建立共用驗證規則與錯誤訊息資源
- [ ] T006 [P] 設定結構化日誌與錯誤處理
- [ ] T007 建立資料完整性不變量與狀態轉換輔助元件
- [ ] T008 設定安全基礎，包含 Anti-Forgery、秘密管理與 CSP 需求

**Checkpoint**: 基礎完成後，使用者故事可依優先級或團隊容量平行展開。

---

## Phase 3: User Story 1 - [Title] (Priority: P1) MVP

**Goal**: [描述此故事交付的使用者價值]

**Independent Test**: [描述如何獨立驗證此故事]

### Tests for User Story 1，必須先失敗

- [ ] T009 [P] [US1] 新增單元測試於 tests/CardPicker2.UnitTests/[path]
- [ ] T010 [P] [US1] 新增整合測試於 tests/CardPicker2.IntegrationTests/[path]
- [ ] T011 [US1] 執行新增測試並確認在實作前失敗

### Implementation for User Story 1

- [ ] T012 [P] [US1] 建立或更新模型於 CardPicker2/Models/[file].cs
- [ ] T013 [P] [US1] 建立或更新服務於 CardPicker2/Services/[file].cs
- [ ] T014 [US1] 更新 Razor Page 或 PageModel 於 CardPicker2/Pages/[file]
- [ ] T015 [US1] 加入輸入驗證、資料完整性檢查與繁中錯誤訊息
- [ ] T016 [US1] 加入安全控制與 Anti-Forgery 保護
- [ ] T017 [US1] 加入關鍵事件與失敗路徑的結構化日誌
- [ ] T018 [US1] 執行 US1 測試並確認通過

**Checkpoint**: User Story 1 MUST 可獨立展示、測試與交付。

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [描述此故事交付的使用者價值]

**Independent Test**: [描述如何獨立驗證此故事]

### Tests for User Story 2，必須先失敗

- [ ] T019 [P] [US2] 新增單元測試於 tests/CardPicker2.UnitTests/[path]
- [ ] T020 [P] [US2] 新增整合測試於 tests/CardPicker2.IntegrationTests/[path]
- [ ] T021 [US2] 執行新增測試並確認在實作前失敗

### Implementation for User Story 2

- [ ] T022 [P] [US2] 建立或更新模型於 CardPicker2/Models/[file].cs
- [ ] T023 [US2] 建立或更新服務於 CardPicker2/Services/[file].cs
- [ ] T024 [US2] 更新 Razor Page 或 PageModel 於 CardPicker2/Pages/[file]
- [ ] T025 [US2] 補齊驗證、安全、日誌與資料完整性處理
- [ ] T026 [US2] 執行 US2 測試並確認 US1 仍通過

**Checkpoint**: User Story 1 與 User Story 2 MUST 仍可各自獨立驗證。

---

## Phase 5: User Story 3 - [Title] (Priority: P3)

**Goal**: [描述此故事交付的使用者價值]

**Independent Test**: [描述如何獨立驗證此故事]

### Tests for User Story 3，必須先失敗

- [ ] T027 [P] [US3] 新增單元測試於 tests/CardPicker2.UnitTests/[path]
- [ ] T028 [P] [US3] 新增整合測試於 tests/CardPicker2.IntegrationTests/[path]
- [ ] T029 [US3] 執行新增測試並確認在實作前失敗

### Implementation for User Story 3

- [ ] T030 [P] [US3] 建立或更新模型於 CardPicker2/Models/[file].cs
- [ ] T031 [US3] 建立或更新服務於 CardPicker2/Services/[file].cs
- [ ] T032 [US3] 更新 Razor Page 或 PageModel 於 CardPicker2/Pages/[file]
- [ ] T033 [US3] 補齊驗證、安全、日誌與資料完整性處理
- [ ] T034 [US3] 執行 US3 測試並確認 US1、US2 仍通過

**Checkpoint**: 所有已選使用者故事 MUST 獨立可用且整體不回歸。

---

[依需要加入更多使用者故事階段，維持相同結構]

---

## Phase N: Polish 與跨切面工作

**目的**: 完成跨故事品質要求與交付檢查。

- [ ] TXXX [P] 更新 zh-TW 文件於 specs/[###-feature-name]/ 或 docs/
- [ ] TXXX 執行程式碼清理與重構
- [ ] TXXX 驗證效能預算或記錄替代量測結果
- [ ] TXXX [P] 補充關鍵業務邏輯測試覆蓋
- [ ] TXXX 執行安全檢查與秘密掃描
- [ ] TXXX 驗證 quickstart.md
- [ ] TXXX 確認憲章檢查的例外均已關閉或有追蹤項目

---

## 相依性與執行順序

### Phase Dependencies

- **Setup (Phase 1)**: 無相依性，可立即開始
- **Foundational (Phase 2)**: 依賴 Setup，會阻塞所有使用者故事
- **User Stories (Phase 3+)**: 依賴 Foundational；可依優先級或容量平行
- **Polish (Final Phase)**: 依賴所有選定使用者故事完成

### User Story Dependencies

- **User Story 1 (P1)**: Foundational 完成後可開始，不依賴其他故事
- **User Story 2 (P2)**: Foundational 完成後可開始，若整合 US1 仍 MUST 可獨立測試
- **User Story 3 (P3)**: Foundational 完成後可開始，若整合 US1/US2 仍 MUST 可獨立測試

### Within Each User Story

- 測試 MUST 先寫並先失敗
- 模型先於服務
- 服務先於頁面或端點
- 核心實作先於整合
- 每個故事完成後 MUST 獨立驗證

### Parallel Opportunities

- 標記 [P] 的 Setup 任務可平行
- 標記 [P] 的 Foundational 任務可平行
- Foundational 完成後，不同使用者故事可由不同人平行處理
- 同一故事內不同檔案的測試或模型任務可平行

---

## 實作策略

### MVP First，只完成 User Story 1

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational
3. 先撰寫並確認 US1 測試失敗
4. 完成 US1 實作
5. 停下來驗證 US1 可獨立交付

### Incremental Delivery

1. 完成 Setup 與 Foundational
2. 完成 US1，測試並展示 MVP
3. 完成 US2，確認 US1 不回歸
4. 完成 US3，確認既有故事不回歸
5. 每個故事都以可展示、可測試的增量交付

### Parallel Team Strategy

1. 團隊共同完成 Setup 與 Foundational
2. Foundational 完成後分工處理不同使用者故事
3. 每個故事在整合前 MUST 通過自己的測試與憲章檢查

---

## Notes

- [P] 任務必須是不同檔案且無相依衝突
- [Story] 標籤用於追蹤任務與使用者故事
- 每個使用者故事 MUST 可獨立完成與測試
- 實作前 MUST 確認新增測試失敗
- 每個 checkpoint 都 MUST 驗證故事仍可獨立運作
- 避免模糊任務、同檔案衝突與破壞故事獨立性的跨故事相依
