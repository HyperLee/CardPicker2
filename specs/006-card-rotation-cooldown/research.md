# 研究: 餐點輪替防重複抽卡

## Decision: 保留 ASP.NET Core Razor Pages 技術棧，不新增 API 或 SPA

**決策**: 006 沿用 ASP.NET Core Razor Pages、Razor form POST、query string/hidden field、Bootstrap 5、jQuery Validation、ASP.NET Core Localization middleware、System.Text.Json、Serilog、xUnit/Moq、WebApplicationFactory 與 browser automation。新增防重複控制與摘要仍是 server-rendered Razor UI，不新增 Minimal API、MVC controller JSON endpoint、SPA framework 或資料庫。

**理由**: 本功能的公開介面仍是首頁抽卡表單、狀態文字、結果摘要與使用者可見錯誤。Razor Pages 適合 page-centered form workflow；PageModel 可以維持薄協調，核心候選池與輪替規則放入 service。新增 API 或 SPA 會擴大公開契約，並增加 server validation、anti-forgery、localization 與 state replay 的同步成本。

**Alternatives considered**:

- 新增 JSON endpoint 回傳輪替候選池摘要：違反本功能不新增外部資料介面，也讓 client state 更容易被誤認為資料完整性來源。
- 改成 SPA 狀態管理：對單機 Razor Pages app 過度複雜，且重複提交與 replay 仍必須由 server-side history 保證。
- MVC controller：目前沒有 handler-level authorization 差異需求，Razor Pages 足夠。

**參考來源**:

- Microsoft Learn, [Razor Pages in ASP.NET Core](https://learn.microsoft.com/aspnet/core/razor-pages/?view=aspnetcore-10.0)
- Microsoft Learn, [Globalization and localization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)

## Decision: 保留 .NET 10 / C# 14，CI/部署留意最新 10.0.x patch

**決策**: `TargetFramework` 維持 `net10.0`，本機 SDK `10.0.100` 可用於本功能規劃與實作。CI/部署 SHOULD 使用目前可用的最新 .NET 10 patch，以符合官方支援政策的 patch currency 要求；不採用 ASP.NET Core 11 preview API。

**理由**: 憲章與專案已鎖定 .NET 10 / C# 14，且 Microsoft 支援政策顯示 .NET 10 是 active LTS。006 不需要平台升級，主要工作是服務層規則與 UI 表單擴充。

**Alternatives considered**:

- 升級到 preview 版本：不符合穩定性要求，也沒有必要功能差距。
- 降回 .NET 8/9：會與憲章、既有專案與 005 技術背景衝突。

**參考來源**:

- .NET, [Official .NET support policy](https://dotnet.microsoft.com/platform/support-policy)

## Decision: 輪替防重複放在 005 filtered candidate pool 之後

**決策**: 抽卡流程固定為：

```text
validate draw operation
-> replay existing operation if successful history exists
-> build 005 base candidate pool by draw mode and meal type
-> apply 005 metadata filters
-> apply 006 recent-success exclusion when enabled and N > 0
-> if post-rotation pool non-empty, uniform randomize within post-rotation pool
-> append DrawHistoryRecord + RotationSnapshot atomically
```

**理由**: 005 已定義正常模式、隨機模式與 metadata filters 如何建立公平候選池。006 只縮小已成立的候選池，不得改變候選池內權重。先篩 metadata 再排除近期卡牌，可以清楚區分「原始候選池本來為空」與「防重複排除後為空」。

**Alternatives considered**:

- 先排除最近卡牌再套用 metadata：會讓 empty reason 較難判斷，也可能讓使用者看不出是條件太嚴還是輪替太嚴。
- 把 cooldown 放進 randomizer：會混合 membership 與 randomness，公平性測試較難。
- 以歷史次數降低權重而非排除：明確違反等機率候選池與不新增權重規則。

## Decision: 新增 `DrawRotationCooldownService`，不把輪替規則放進 PageModel

**決策**: 新增 service 負責驗證後的輪替套用：輸入 005 候選池、成功抽卡歷史與 `RotationCooldownSettings`，輸出 `RotationCandidatePool`，包含輪替前候選池數、排除 card IDs、排除數、輪替後候選池與可持久化快照。PageModel 只負責 binding `avoidRecentRepeats` 與 `recentDrawCount`，並把 normalized operation 交給 `ICardLibraryService.DrawAsync`。

**理由**: 輪替規則需要單元測試覆蓋 history ordering、ID 去重、deleted card、random mode、metadata filters、empty reason 與 snapshot。放在 service 可重用於 unit/integration tests，並符合 PageModel 不承載核心業務規則的架構要求。

**Alternatives considered**:

- 延伸 `DrawCandidatePoolBuilder` 直接讀 history：會讓 005 base/metadata 候選池與 006 recent exclusion 耦合，測試邊界變差。
- 在 `CardLibraryService.DrawCoreAsync` 內直接 LINQ 寫完：實作快但不易獨立測試與審查。

## Decision: 沿用 schema v4，`DrawHistoryRecord.RotationSnapshot` 為 optional

**決策**: `CardLibraryDocument.CurrentSchemaVersion` 維持 4。`DrawHistoryRecord` 新增 optional `RotationSnapshot? RotationSnapshot`，JSON 欄位可命名為 `rotationSnapshot`。006 上線前的成功歷史缺少快照時仍合法；讀取時不得回填推測快照，寫入新成功歷史時才保存本次實際快照。

**理由**: 006 不新增 root collection，也不要求既有 history 具備新欄位。把快照設計成 optional 可讓 v4 文件向前相容，避免因缺少歷史摘要而封鎖使用者既有資料。輪替快照是「成功抽卡當下的最小摘要」，不是後續可由目前資料可靠重算的 aggregate。

**Alternatives considered**:

- 升級 schema v5 並回填舊 history：會迫使既有資料遷移，且舊 history 的輪替設定不存在，任何回填都是推測，違反規格。
- 不持久化快照，每次顯示重算：語系切換、重啟、card edit/delete 或 N 值變更後會顯示不同摘要，違反 replay 一致性。
- 將快照放到獨立檔案：跨檔案一致性會破壞抽卡結果與 history 同一原子寫入。

## Decision: 最近 N 次以成功歷史由新到舊排序，時間相同以持久化順序較後者較新

**決策**: 最近成功範圍只取 `DrawHistoryRecord` 中已持久化成功的紀錄。排序規則為 `SucceededAtUtc` 由新到舊；若時間相同，使用該紀錄在 `drawHistory` list 中的後方位置作為較新。取最近 N 筆後，以 `CardId` 建立 `HashSet<Guid>` 排除集合。

**理由**: JSON array 的順序就是持久化歷史順序，且同一 process 內 read-modify-write 已序列化。當時間精度或測試 fixture 造成相同 timestamp 時，用 array position 可以穩定重現「較後寫入者較新」。

**Alternatives considered**:

- 只依 timestamp 排序：相同時間時不穩定。
- 取最近 N 張唯一卡牌：規格已明確 N 代表最近 N 筆成功紀錄，不是最近 N 張唯一卡。
- 依 card name/metadata 判斷近期：會因改名、翻譯或 metadata 更新失效，違反不可變 ID 規則。

## Decision: 防重複後空候選池與原始空候選池使用不同狀態

**決策**: `DrawResult` 或等效結果需能區分：

- 005 base/metadata pool 為空：使用既有空卡池或無符合條件訊息。
- 005 pool 非空，但 006 rotation 後為空：使用 `Rotation.EmptyAfterCooldown` 類訊息，提示降低 N、關閉防重複或調整其他條件。

**理由**: 使用者需要知道下一步要調整 metadata/餐別，還是調整輪替防重複。自動放寬規則會破壞信任；只顯示一般無結果會讓使用者不知道 N 是原因。

**Alternatives considered**:

- 自動降低 N 或關閉防重複：違反規格與資料完整性。
- 顯示同一 empty pool message：無法滿足 P2 使用者故事。

## Decision: replay 優先使用已保存成功歷史與快照，不重新套用目前條件

**決策**: `CardLibraryService.DrawCoreAsync` 在 build candidate pool 前先查 `OperationId` 是否已有成功歷史。若存在，重顯原 card ID 與原 `RotationSnapshot`；若原 history 缺少快照，顯示「當時未保存輪替摘要」或等效狀態。replay 不讀取目前 N 值、不重建候選池、不新增 history。

**理由**: 同一次成功操作重送時，該次結果已成立。若 replay 重新套用目前 N 或最新 history，該次結果可能被自己排除，造成重顯失敗或錯誤摘要。

**Alternatives considered**:

- replay 時重算摘要：會因 history/card/filter 變更產生不一致。
- 只靠前端 disable button：不能處理 refresh、form resubmission、網路重試或 app restart。

## Decision: System.Text.Json source generation 是可選補強，不列為本功能必要替換

**決策**: 先沿用目前 `JsonSerializerOptions` 與 `JsonStringEnumConverter`。若 150 張卡牌/1,000 筆 history fixture 的 p95 或記憶體預算失敗，再新增 `JsonSerializerContext` source generation 作為性能補強。

**理由**: 006 的運算熱點不是 JSON 形狀複雜度，而是同一文件載入後的 in-memory filtering。N 上限為 10，排除集合很小。過早導入 source generation 會增加實作面與測試變更，但不是目前需求的必要條件。

**Alternatives considered**:

- 立即改成 source-generation-only serialization：可能增加泛型/options 限制與重構成本，不需要先做。
- 改用 Newtonsoft.Json：沒有需求優勢，會新增依賴並偏離既有 System.Text.Json 實作。

**參考來源**:

- Microsoft Learn, [How to use source generation in System.Text.Json](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation)

## Decision: Serilog 保留為結構化日誌，不記錄 payload

**決策**: 沿用 `ILogger<T>` 與 Serilog provider。新增事件包含 invalid N、rotation applied、empty-after-rotation、draw success with rotation counts、replay with/without snapshot、write failure。不得記錄完整 `cards.json`、完整描述、完整 tags payload、未清理輸入、系統提示或 stack trace 到 UI。

**理由**: 本功能需要可診斷候選池縮小原因，但 card content 與 JSON 文件是使用者資料。結構化欄位只保存安全的 count、mode、operation ID、card ID 與 status key。

**Alternatives considered**:

- 開啟 HTTP body logging：對本功能風險大於價值，可能洩漏餐點描述與表單內容。
- 只用 console 無結構欄位：難以診斷 empty-after-rotation 與 replay。

**參考來源**:

- Serilog, [Serilog.AspNetCore request logging](https://github.com/serilog/serilog-aspnetcore)
