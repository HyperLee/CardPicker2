# 研究: 餐點收藏與手動排除抽卡

## Decision: 保留 Razor Pages 表單介面，不新增 API、SPA 或資料庫

**決策**: 007 沿用 ASP.NET Core Razor Pages、Razor form POST、query string、hidden field、Bootstrap 5、jQuery Validation、ASP.NET Core Localization middleware、System.Text.Json、Serilog、xUnit/Moq、WebApplicationFactory 與既有 browser automation。收藏/排除切換、卡牌庫偏好篩選與結果區偏好操作都透過 server-rendered Razor Pages 與 Anti-Forgery protected forms 完成。

**理由**: 本功能的公開介面仍是使用者可見頁面、表單欄位、query string、狀態 badge、validation message、成功/失敗訊息與 HTML 結果。Razor Pages 適合 page-centered form workflow；PageModel 可以維持薄協調，核心偏好狀態、候選池排除與 JSON 寫入放入 service。新增 API 或 SPA 會擴大公開契約，增加 anti-forgery/localization/state replay 同步成本，且違反 FR-025 不新增外部系統資料介面的要求。

**Alternatives considered**:

- 新增 Minimal API 或 MVC JSON endpoint 管理偏好：會擴大公開契約與測試矩陣，且不符合目前 Razor Pages UI 契約。
- 改用資料庫保存偏好：超出單機 JSON 產品邊界，也增加 migration、transaction 與部署成本。
- 以 localStorage 保存收藏/排除：server-side draw 無法信任 client state，也無法保證跨重啟 JSON 資料一致性。

**參考來源**:

- Microsoft Learn, [Razor Pages in ASP.NET Core](https://learn.microsoft.com/aspnet/core/razor-pages/?view=aspnetcore-10.0)

## Decision: 保留 .NET 10 / C# 14，使用最新 10.0.x patch 但不採 preview

**決策**: `TargetFramework` 維持 `net10.0`，程式語言與專案慣例維持 C# 14、Nullable Reference Types 與 implicit usings。CI/部署 SHOULD 使用目前可用的最新 .NET 10 patch；不採用 ASP.NET Core 11 preview API。

**理由**: 憲章與專案已鎖定 .NET 10 / C# 14。官方支援政策顯示 .NET 10 是 active LTS，且 .NET 10 目前支援期到 2028-11-14。007 是服務層資料規則與 Razor UI 表單擴充，不需要平台替換。

**Alternatives considered**:

- 升級到 preview 版本：穩定性與支援風險高，且沒有本功能必需能力。
- 降回 .NET 8/9：會與憲章、repo、既有程式碼與測試專案衝突。

**參考來源**:

- .NET, [Official .NET support policy](https://dotnet.microsoft.com/platform/support/policy)

## Decision: 將 `cards.json` 升級為 schema v5，偏好狀態綁定 card object

**決策**: `CardLibraryDocument.CurrentSchemaVersion` 升級為 5。root 保留 `schemaVersion`、`cards` 與 `drawHistory`。每張 `MealCard` 新增 `preferences` object，包含 `isFavorite` 與 `isExcludedFromDraw`。讀取 v1/v2/v3/v4 時在記憶體中補齊 `CardPreferenceState.Default`，代表未收藏且未排除；下一次成功寫入以 v5 原子保存。

**理由**: 收藏與排除是綁定不可變 card ID 的長期偏好，和 card lifecycle 一起保存可避免跨檔案一致性問題。偏好狀態會影響未來候選池，因此比 006 optional `rotationSnapshot` 更適合用 schema v5 清楚標示資料形狀。舊資料缺少 preference 欄位不得視為 corrupted，符合 DI-009。

**Alternatives considered**:

- 維持 schema v4 並默默加入 optional 欄位：技術可行，但版本語意不清楚，後續資料驗證與支援回溯較難審查。
- 另建 `card-preferences.json`: 會讓 card mutation 與 preference mutation 跨檔案，寫入失敗時較難保證完整成功或完整失敗。
- 將 preferences 存入 cookie/localStorage: 無法讓 server-side draw 正確排除，也不能滿足跨應用程式重新啟動的一致持久化要求。

## Decision: 排除抽卡先於 metadata 篩選與近期輪替生效

**決策**: 抽卡候選池順序固定為：

```text
validate draw operation
-> replay existing operation if successful history exists
-> load active cards
-> remove cards where Preferences.IsExcludedFromDraw == true
-> build base pool by draw mode and meal type
-> apply 005 metadata filters
-> apply 006 recent-success rotation exclusion
-> if final pool non-empty, uniform randomize within final pool
-> append DrawHistoryRecord + RotationSnapshot atomically
```

**理由**: 007 DI-003 要求排除抽卡先於 006 近期輪替防重複生效。先移除手動排除卡牌可保證該卡不會出現在 normal/random base pool、metadata filtered pool、rotation pre/post pool、fallback 或結果中。收藏不參與此流程，只影響 UI 標示與卡牌庫篩選。

**Alternatives considered**:

- 先套用 metadata/rotation，再排除：最終結果可能仍正確，但 rotation snapshot 與 empty reason 容易誤把手動排除造成的空集合歸因給近期輪替。
- 在 randomizer 中檢查排除狀態：會混合 membership 與 randomness，公平性測試較難。
- 將排除視為 delete：違反規格，因為排除卡牌仍需預設出現在卡牌庫並可取消排除。

## Decision: 收藏不影響候選池、排序、統計或輪替

**決策**: `IsFavorite` 只用於卡牌庫/詳情/結果區狀態顯示、卡牌庫偏好篩選與使用者整理。抽卡候選池、randomizer、recent rotation exclusion、draw history append、statistics denominator 與 duplicate detection 均不讀取 `IsFavorite`。

**理由**: 規格 FR-010、FR-011 與 SC-002/SC-003 要求候選池內仍等機率，且收藏不得成為權重、排序或機率暗示。讓收藏完全離開 draw membership 可讓公平性測試保持簡單可審查。

**Alternatives considered**:

- 收藏卡牌加權或優先排序：明確超出本功能，且違反等機率候選池。
- 新增「只抽收藏」模式：規格 FR-026 禁止第一版新增收藏專屬抽卡模式。
- 將收藏納入 recent rotation：會改變既有輪替語意，也不符合收藏只協助辨識與篩選的範圍。

## Decision: 偏好更新使用 target-state mutation，而不是 server-side toggle

**決策**: 新增 `CardPreferenceUpdateInputModel` 或等效輸入，包含 `CardId` 以及目標最終狀態，例如 `targetIsFavorite` 或 `targetIsExcludedFromDraw`。服務層方法如 `SetPreferenceAsync(Guid cardId, CardPreferenceUpdateInputModel input)` 在單一 read-modify-write critical section 中保存目標狀態。重複提交同一 target state 必須得到相同最終狀態，不反覆翻轉。

**理由**: 規格釐清事項要求表單提交目標最終狀態。若使用「toggle 目前狀態」語意，快速連點、瀏覽器重送或 retry 會導致狀態反覆反轉，違反 DI-008。target-state mutation 也更容易測試 idempotency。

**Alternatives considered**:

- POST 只提交 `action=toggleFavorite`: 實作簡單但重複提交不安全。
- 只用 JavaScript disable button 防連點：可改善 UX，但 server-side data integrity 仍不可靠。
- 將偏好變更併入 edit card content form：會讓「整理偏好」過度依賴完整卡牌編輯流程，也增加局部更新風險。

## Decision: 卡牌庫預設顯示排除卡牌，並新增偏好篩選條件

**決策**: `/Cards` 一般列表仍以 active cards 為範圍，包含被排除抽卡的 active cards。新增 `CardPreferenceCriteria`，至少包含：

- `FavoriteFilter`: `All`、`FavoritesOnly`、`NotFavoritesOnly`
- `DrawEligibilityFilter`: `All`、`DrawableOnly`、`ExcludedOnly`

偏好篩選與既有 keyword、meal type、metadata filters 採交集規則。

**理由**: 規格要求排除卡牌預設仍可搜尋、查看、編輯、取消排除或刪除，避免使用者找不到已排除卡牌。收藏與排除都是整理狀態，作為卡牌庫篩選條件能符合 P2。

**Alternatives considered**:

- 預設隱藏排除卡牌：使用者可能找不到並無法取消排除，違反 FR-012。
- 只提供「收藏」篩選，不提供可抽/已排除：不滿足 FR-014。
- 讓 deleted cards 也出現在偏好篩選中：會混淆 active preference 與歷史統計 retention；deleted card 仍只由統計表呈現。

## Decision: 結果區偏好操作不修改已揭示結果、歷史、統計或輪替快照

**決策**: 首頁結果區可對目前 `resultCardId` 提交 target-state 收藏或排除操作。服務只更新該 card 的 preference state；目前畫面繼續顯示同一 card ID。成功抽卡歷史、總成功抽卡次數、單卡抽中次數、歷史機率與該次 rotation snapshot 不被修改。

**理由**: 偏好整理常發生在結果揭示後，但已揭示結果是已成立的成功抽卡事實。若排除剛抽中的卡牌後重算結果或 history，會破壞 004/006 idempotency 與統計一致性。

**Alternatives considered**:

- 結果區排除後自動重新抽卡：會造成使用者未明確啟動的新成功 draw，且統計與 operation id 難以解釋。
- 將本次歷史標記為已排除不計入統計：違反 004 統計口徑與 007 DI-006。
- 排除後清除結果：降低可理解性，且不符合 P3 要求保留已揭示結果。

## Decision: runtime 文案用 resource keys，服務回傳 stable status key

**決策**: 新增收藏/排除 labels、badges、filters、success/error、empty-after-preference 與 result action resource keys 到 `SharedResource.zh-TW.resx` / `SharedResource.en-US.resx`。服務層回傳 stable status key 與必要安全 arguments；PageModel/view 依 current culture 呈現文字。

**理由**: 003 已核准 runtime 雙語 UI，007 新增的所有可見文字都必須雙語完整。服務回傳 key 比直接回傳中文或英文更容易測試，也避免業務規則耦合 current culture。

**Alternatives considered**:

- 服務直接回傳 localized string：測試與 fallback 較脆弱。
- 只補繁中：違反 bilingual governance 與 007 FR-023。

**參考來源**:

- Microsoft Learn, [Globalization and localization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)

## Decision: 不新增序列化或效能平台；必要時才考慮 System.Text.Json source generation

**決策**: 先沿用目前 `JsonSerializerOptions` 與 `JsonStringEnumConverter`。若 150 張卡牌/1,000 筆 history fixture 的 p95 或記憶體預算失敗，再評估 `JsonSerializerContext` source generation 作為性能補強。

**理由**: 007 的運算熱點是已載入 document 上的 bool filter 與 small-object update，不是 serialization 複雜度。過早導入 source generation 會增加重構面與測試變更，不是目前必要條件。

**Alternatives considered**:

- 立即改成 source-generation-only serialization：可能增加泛型/options 限制與重構成本。
- 改用 Newtonsoft.Json：沒有需求優勢，會新增依賴並偏離既有 System.Text.Json 實作。

## Decision: 結構化日誌只記錄安全欄位與 count，不記錄 payload

**決策**: 沿用 `ILogger<T>` 與 Serilog provider。新增事件包含 schema v5 migration、preference update、invalid preference target、excluded card removed from draw pool count、empty-after-preference、result preference action、write failure 與 blocked library。不得記錄完整 `cards.json`、完整餐點描述、tag list 原文、完整 metadata/preference payload、未清理輸入、系統提示或 stack trace 到 UI。

**理由**: 偏好狀態可能透露使用者主觀喜好；診斷需要 card ID、operation ID、safe status key 與 count 即可。避免 payload logging 符合安全與隱私邊界。

**Alternatives considered**:

- 開啟 HTTP body logging：風險大於價值，會暴露 card content 與偏好操作。
- 只用 console 字串：缺乏可查詢結構欄位，難以診斷 empty candidate pool 與 write failure。
