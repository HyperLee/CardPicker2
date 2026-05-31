# Security Review: CardPicker2（繁體中文報告）

## Scope

- 掃描模式：針對 `/Users/qiuzili/CardPicker2`、commit `96d89d9` 執行 repository-wide Codex Security scan。
- 執行期範圍：`CardPicker2/` 下的 ASP.NET Core Razor Pages 應用程式，包含 PageModel、Razor template、service、model、static asset、設定檔、seed data，以及 package/version 宣告。
- 支援性範圍：Spec Kit 與開發用 artifact、產生出的 `bin/` 與 `obj/` 輸出、測試證據皆已檢視其安全相關性，但不視為已部署的 runtime attack surface。
- 產生的掃描脈絡：本掃描產生 per-scan threat model：`ReportFolder/96d89d9_20260531184843/artifacts/01_context/threat_model.md`，後續階段以該檔作為威脅模型的依據。
- 驗證 artifact：動態驗證 harness 輸出位於 `ReportFolder/96d89d9_20260531184843/artifacts/03_coverage/validation_harness_output.json`。
- 最終輸出：本 Markdown 報告位於 `ReportFolder/96d89d9_20260531184843/report.md`；HTML 報告位於 `ReportFolder/96d89d9_20260531184843/report.html`。

### Scan Summary

| Field | Value |
|---|---|
| Reportable findings | 3 |
| Severity mix | medium: 3 |
| Confidence mix | high: 3 |
| Coverage | `artifacts/02_discovery/work_ledger.jsonl` 中 134/134 個 deep-review row 已關閉；`artifacts/03_coverage/repository_coverage_ledger.md` 中 13 個 repository surface row 已關閉 |
| Validation mode | 靜態原始碼檢視、subagent full-file receipts、NuGet vulnerable package 查詢、advisory/version 檢查，以及針對原始應用程式碼的 bounded validation harness |
| Deferred coverage | 無 |

## Threat Model

### 概觀

CardPicker2 是單使用者、本機執行的 ASP.NET Core Razor Pages 網頁應用程式，用於管理餐點卡牌，並以 slot-machine 風格抽出餐點。主要 runtime project 是 `CardPicker2/`；測試、Spec Kit 文件與開發筆記支援產品開發，但預設不是部署後的應用程式 attack surface。

此應用程式暴露 server-rendered Razor Pages、form posts、query-string filters、static assets、localization resources，以及本機 JSON persistence file：`CardPicker2/data/cards.json`。核心狀態包含餐點卡牌內容、卡牌 metadata、preference state、draw history、draw statistics、language preference，以及暫時性的 UI status messages。

### 資產與信任邊界

- `cards.json` 的完整性，包括 card identity、meal type、metadata、preference state、draw history、deleted-card retention 與 schema version。
- 抽卡結果的正確性與公平性，包括 selected meal type、filter criteria、cooldown handling 與 idempotent draw operation IDs。
- 有效資料下的卡牌操作可用性，以及 persisted data corrupted 或 unsupported 時的安全 blocking behavior。
- state-changing forms、localized UI 與 static assets 的 browser security controls。
- 本機檔案內容、logs、system prompts、connection strings、secrets、stack traces 與 corrupted JSON contents 的機密性。
- Browser-to-server inputs 都是不可信任輸入：route values、query strings、form fields、anti-forgery tokens、cookies 與 headers 都必須在 server-side 驗證。
- 本機 JSON 檔案由 operator 控制，但仍具安全相關性，因為 malformed 或遭竄改的資料可能影響所有後續卡牌操作。

### 既有控制

- Razor Pages model binding 加上 service-layer validation 來執行 domain rules。
- ASP.NET Core anti-forgery 機制保護 state-changing Razor form posts。
- Razor 預設 HTML encoding。
- Language change 使用 `LocalRedirect` 搭配 return URL normalization。
- Supported-culture allowlist 與 unsupported culture cookie fallback。
- Production 環境在 `Program.cs` 中設定 HSTS、HTTPS redirection 與 CSP。
- JSON document validation、schema checks、corrupted-file blocking，以及 same-process mutation serialization。

### 嚴重度校準

在此本機單使用者模型下，Critical 或 High finding 仍需要嚴重邊界突破，例如 remote code execution、對 trusted config 有影響的任意檔案寫入、具真實 browser impact 的 stored XSS，或重大敏感資料外洩。Medium findings 包含會持久化 invalid draw/card state 的 server-side validation gaps、會破壞 recovery behavior 的 corrupted JSON handling，或具受限前置條件的 local file-persistence hazards。Low findings 則是 exploitability 有限的 hardening gaps。

## Findings

| # | Title | Severity | Confidence | Category |
|---|---|---|---|---|
| 1 | [Draw POST 未處理模型繫結錯誤即持久化抽卡結果](#1-draw-post-未處理模型繫結錯誤即持久化抽卡結果) | medium | high | Server-side input validation bypass |
| 2 | [Current-schema malformed JSON 可能在 corrupt-file recovery 前丟出例外](#2-current-schema-malformed-json-可能在-corrupt-file-recovery-前丟出例外) | medium | high | Malformed local data denial of service |
| 3 | [固定 card-library 暫存路徑會跟隨預先建立的 symlink](#3-固定-card-library-暫存路徑會跟隨預先建立的-symlink) | medium | high | Insecure temporary file handling / symlink-following write |

### Confidence Scale

| Label | Meaning |
|---|---|
| high | 直接的 source、configuration 或 runtime evidence 支援 finding，且 reachability 或 exploitability 沒有重大未解 blocker。 |
| medium | Source evidence 支援 plausible issue，但 runtime behavior、deployment configuration、role reachability、type constraints 或 exploit reliability 仍需額外證明。 |
| low | 證據薄弱或不完整；僅在刻意將 follow-up candidates 保留於最終報告時使用。 |

### [1] Draw POST 未處理模型繫結錯誤即持久化抽卡結果

| Field | Value |
|---|---|
| Severity | medium |
| Confidence | high |
| Confidence rationale | Validation harness 以原始 `IndexModel.OnPostDrawAsync` 執行，於 `ModelState` 中放入 `DrawMode` model-binding error，並觀察到成功持久化的 draw history record。 |
| Category | Server-side input validation bypass |
| CWE | CWE-20: Improper Input Validation |
| Affected lines | `CardPicker2/Pages/Index.cshtml.cs:137`, `CardPicker2/Pages/Index.cshtml.cs:151`, `CardPicker2/Pages/Index.cshtml.cs:162`, `CardPicker2/Services/CardLibraryService.cs:421` |

#### Summary

`OnPostDrawAsync` 會載入 card library，只檢查 `RecentDrawCount` 專屬的 binding errors，接著用已繫結的 properties 建立 `DrawOperation`。其他 model-binding failures，例如無效的 `DrawMode` 字串導致 typed property 留在 default value，不會阻止抽卡。Service 之後收到看似有效的 typed defaults，並可能 append 成功的 draw record。

#### Validation

方法：直接以 PageModel validation harness 驗證原始應用程式碼。Harness 加入一個 `DrawMode` `ModelState` error，設定足以走向成功路徑的 typed bound properties，呼叫 `OnPostDrawAsync`，再檢查 persisted `cards.json`。

觀察到的證據：`artifacts/03_coverage/validation_harness_output.json` 中有 `modelStateDrawModeErrors=1`、`resultSucceeded=True`、`historyRecordFound=True`、`historyCount=1`、`persistedDrawMode=Normal` 與 `persistedMealType=Breakfast`。

剩餘不確定性：對本機程式碼路徑沒有重大不確定性。完整 HTTP integration request 可進一步證明 ASP.NET Core 在此 app 中對 invalid enum binding 的精確行為，但 vulnerable handler behavior 已在相同 `ModelState` 條件下直接重現。

#### Dataflow

Crafted draw form field -> Razor Page model binding 記錄 error 並留下可用的 typed properties -> `IndexModel.OnPostDrawAsync` 於 `Index.cshtml.cs:137` 繼續執行 -> line 151 的 `CreateRotationSettings()` 只 gate recent draw count -> line 162 建立 `DrawOperation` -> `CardLibraryService.DrawAsync` 於 `CardLibraryService.cs:421` append draw history。

#### Reachability

具有效 anti-forgery token 的 same-user browser request，可直接送出 malformed draw fields 觸發此路徑，而不需透過正常 UI 控制項。Cross-site attacker 若無法繞過 anti-forgery，無法取得此路徑。結果是 draw history 與 statistics 中持久化 invalid-request semantics，不是 account compromise 或敏感資料外洩。

#### Severity

Medium。此路徑是實際產品 workflow，副作用是 persistent application-state integrity impact，符合 threat model 中 server-side validation gaps 的 medium 類別。嚴重度低於 high，因為 exploitation 需要 local/same-user draw surface 與有效 token，且不跨越 multi-user authorization boundary。若有 anti-forgery 可被繞過，或 invalid persisted draws 可造成更廣泛 file corruption 的證據，嚴重度會提高；若能證明 ASP.NET Core 對任何 posted draw field 都不會產生此 `ModelState`/default-value 組合，嚴重度會降低。

#### Remediation

在建立 `DrawOperation` 前，只要 `ModelState` 含有任何 binding error 就拒絕 draw submission，同時保留既有欄位專屬錯誤訊息。新增 integration test：以有效 anti-forgery token POST 無效的 `DrawMode` 或 metadata enum 字串，並 assert 沒有 append draw history。

### [2] Current-schema malformed JSON 可能在 corrupt-file recovery 前丟出例外

| Field | Value |
|---|---|
| Severity | medium |
| Confidence | high |
| Confidence rationale | Validation harness 寫入 schema v5 `cards.json`，其中 `decisionMetadata.dietaryPreferences=null`，並在 service 回傳 blocked corrupt-file result 前重現 `ArgumentNullException`。 |
| Category | Malformed local data denial of service |
| CWE | CWE-20: Improper Input Validation; CWE-248: Uncaught Exception |
| Affected lines | `CardPicker2/Services/CardLibraryService.cs:83`, `CardPicker2/Services/CardLibraryService.cs:94`, `CardPicker2/Services/CardLibraryService.cs:107`, `CardPicker2/Services/CardLibraryService.cs:1068`, `CardPicker2/Models/MealCardDecisionMetadata.cs:56`, `CardPicker2/Services/MealCardMetadataValidator.cs:47` |

#### Summary

Repository 預期 corrupted 或 invalid `cards.json` 應被保留，並轉換為 blocking recovery state。對 current-schema documents，`DeserializeDocument` 會呼叫 `NormalizeCurrentDocument`，而該方法在 `ValidateDocument` 能拒絕 malformed child state 前，就先 normalize 每張卡牌。Parseable JSON 中的 null child collections 因此可能在 normalization 或 metadata validation 時丟出例外，而 `LoadAsync` 在 deserialization 周圍只 catch `JsonException`。

#### Validation

方法：直接以 service validation harness 驗證 `CardLibraryService.LoadAsync`，輸入是一個 schema v5 JSON file，內含一張 active card，且 `decisionMetadata.dietaryPreferences=null`。

觀察到的證據：`artifacts/03_coverage/validation_harness_output.json` 中有 `exceptionType=System.ArgumentNullException` 與 `exceptionMessagePrefix=Value cannot be null. (Parameter 'source')`。輸入檔保存在 `artifacts/03_coverage/null-metadata-case/cards.json`。

剩餘不確定性：對重現的 null metadata collection 沒有重大不確定性。相似的 null localized strings 與 localization dictionaries 應由同一修正與 regression tests 涵蓋。

#### Dataflow

Local `cards.json` bytes -> `CardLibraryService.LoadAsync` 於 line 94 讀取檔案 -> `DeserializeDocument` 處理 parseable current-schema JSON -> `NormalizeCurrentDocument` 在 line 1068 執行 `document.Cards.Select(card => card.Normalize())` -> `MealCardDecisionMetadata.Normalize()` 於 line 56 enumerate `DietaryPreferences` -> unhandled exception escape，而不是回傳 `CardLibraryLoadResult.BlockedCorrupt`。

#### Reachability

本機使用者或程序竄改 `cards.json` 後，任何呼叫 `LoadAsync` 的 page 或 operation 都可能觸發 crash。此 app 是 single-user/local，因此預設不是 remote unauthenticated denial of service。不過這仍在 scope 內，因為 threat model 將 local JSON tampering 與 recovery-state behavior 視為 security-relevant availability and integrity concerns。

#### Severity

Medium。影響是 persistent local availability loss，並繞過設計中的 corrupt-file recovery behavior。低於 high，因為 attacker 必須能影響本機 data file，而且重現效果是 request failure，不是 code execution、secret disclosure 或 cross-user compromise。若 browser-facing app path 可在沒有 local filesystem access 下寫入此 malformed state，嚴重度會提高；若有廣泛 exception wrapper 可可靠地將所有 malformed current-schema shapes 轉成 blocked recovery，嚴重度會降低。

#### Remediation

在 normalization 前先 validate 或 sanitize nullable child state。Null collections 與 localized strings 應被視為 validation failures，或在任何 dereference 前 normalize 成 empty values。擴充 corrupted-file handling tests，加入 schema v5 的 null `localizations`、null localized `name`/`description`、null `decisionMetadata.tags`、null `decisionMetadata.dietaryPreferences` cases，並 assert `BlockedCorrupt` 與原始檔 preservation。

### [3] 固定 card-library 暫存路徑會跟隨預先建立的 symlink

| Field | Value |
|---|---|
| Severity | medium |
| Confidence | high |
| Confidence rationale | Validation harness 預先將 `cards.json.tmp` 建成 symlink，並觀察到 `CreateAsync` 以 serialized card-library JSON 覆寫 symlink target。 |
| Category | Insecure temporary file handling / symlink-following write |
| CWE | CWE-377: Insecure Temporary File; CWE-59: Link Following |
| Affected lines | `CardPicker2/Services/CardLibraryService.cs:941`, `CardPicker2/Services/CardLibraryService.cs:944`, `CardPicker2/Services/CardLibraryService.cs:956` |

#### Summary

`WriteDocumentAsync` 使用可預測的 temporary filename：`filePath + ".tmp"`，接著以 `FileMode.Create` 開啟。如果 local attacker 或其他 process 能先把該 path 建成 symbolic link，write 會在 service 將 temp path move 到 `cards.json` 前跟隨該 link。`CardLibraryFileCoordinator` 的 same-process serialization 無法防護 process 外部預先建立的 filesystem object。

#### Validation

方法：直接以 service validation harness 驗證，在呼叫 `CardLibraryService.CreateAsync` 前，先將 `cards.json.tmp` symlink 到同 directory 中可寫的 sibling file。

觀察到的證據：`artifacts/03_coverage/validation_harness_output.json` 中有 `mutationStatus=Succeeded`、`victimChanged=True`、`victimLengthBefore=23`、`victimLengthAfter=19114` 與 `victimStartsWithJsonObject=True`。Validation case files 位於 `artifacts/03_coverage/temp-symlink-case/`。

剩餘不確定性：本機程式碼行為已重現。Deployment-specific impact 取決於 card-library directory 周邊的 filesystem permissions，以及較低權限 actor 是否可在 app 以較高權限寫入時建立 temp-path symlink。

#### Dataflow

Local filesystem object at `cards.json.tmp` -> `WriteDocumentAsync` 於 `CardLibraryService.cs:941` 計算 deterministic temp path -> line 944 的 `new FileStream(tempPath, FileMode.Create, ...)` 跟隨預先建立的 symlink -> JSON serialization 寫入 symlink target -> line 956 的 `File.Move(tempPath, filePath, overwrite: true)` promote 該 filesystem object。

#### Reachability

Web UI 不暴露任意 path selection。Realistic attacker 是 local user 或其他 process，需具備 card-library directory 的 write access，但不一定能直接寫入被選定的 symlink target。任何後續 app write，例如 draw history、create、edit、delete 或 preference update，都可觸發 overwrite。在預設 single-user local setup 中，這通常退化為 same-user file access；但若 data directory 可由低權限 actor 寫入，而 app 以較高權限執行，這會變成更強的 boundary issue。

#### Severity

Medium。此 primitive 是受限 local filesystem 前置條件下的 app-account arbitrary file overwrite。未達 high，因為掃描未找到 browser-controlled path 可建立 symlink，也未證明可覆寫 executable/startup/config files，且預期部署是 local single-user。若有 production data directory 可被低權限寫入，或可覆寫 trusted executable/config paths 的證據，嚴重度會提高；若能證明該 directory 永遠只對 app account private，嚴重度會降低。

#### Remediation

在同一 directory 中使用 random temporary filename，並以 exclusive creation 開啟；若 path 已存在則 fail。優先採用不跟隨 pre-existing links 的 safe replace pattern，並只清理 process 自己建立的 exact temp file。新增 filesystem regression test：預先將 `cards.json.tmp` 建成 symlink，並 assert write 失敗且 target 未被修改。

## Reviewed Surfaces

| Surface | Risk Area | Outcome | Notes |
|---|---|---|---|
| Draw POST model binding and persistence | Server-side input validation | Reported | 成為 finding 1。 |
| Card-library JSON load, normalization, and validation | Malformed local data / recovery state | Reported | 成為 finding 2。 |
| Card-library write path | Filesystem temp file handling | Reported | 成為 finding 3。 |
| Razor forms for draw、preferences、create、edit、delete、language | CSRF | No issue found | State-changing Razor forms 使用 ASP.NET Core anti-forgery tag helpers；既有測試涵蓋 draw/security flows 的 missing-token rejection。 |
| Razor templates 與 localized/card content rendering | Stored/reflected XSS | No issue found | Templates 使用一般 Razor encoding；reviewed runtime pages 或 `site.js` 未發現 `Html.Raw` 或 raw DOM sink。 |
| Language switcher 與 returnUrl handling | Open redirect / cookie tampering | No issue found | `GetSafeReturnUrl` 會拒絕 absolute/scheme-relative values，handler 使用 `LocalRedirect`；culture 有 allowlist，cookie 僅儲存 UI preference。 |
| `Program.cs` production middleware | Security headers / CSP / HSTS | No issue found | Production branch 啟用 HSTS、HTTPS redirection、nonce-based CSP、`form-action 'self'` 與 `frame-ancestors 'none'`。 |
| Card management service 與 PageModels | Create/edit/delete validation and recovery-state bypass | No issue found | PageModels 委派給 service validation；service 會阻擋 invalid inputs、duplicates、deleted cards 與 unavailable libraries。 |
| Draw pool、filters、cooldown、statistics、replay | Draw integrity / replay / randomness | No issue found | Pool builder 依 mode 與 metadata 篩選 active/drawable cards；replay 是 idempotent；在此 local app 中 `Random.Shared` 不是 security boundary。 |
| Client JavaScript `site.js` | DOM injection / client-side trust | No issue found | 未發現 `innerHTML`、`eval`、`fetch` 或 token persistence；`CSS.escape` 保護 restored selector names；server 仍是 authoritative。 |
| NuGet 與 bundled client dependencies | Known vulnerable dependencies | No issue found | `dotnet list ... --vulnerable --include-transitive` 未回報 vulnerable NuGet packages；Bootstrap 5.3.3 與 jQuery Validation 1.21.0 不在已檢查 advisory ranges 內。詳見 `artifacts/01_context/seed_research.md`。 |
| Spec Kit scripts、workflows、generated `bin/`/`obj` outputs | Developer/generated artifacts | Not applicable | 不屬於此 local app scan 的 deployed runtime surfaces；未提升任何 privileged automation path。 |
| appsettings、launchSettings、resources、seed data | Secrets / sensitive disclosure | No issue found | Reviewed config/resource/data surfaces 未發現 secrets、connection strings、keys、完整 corrupt JSON、stack traces 或 system prompts。 |
