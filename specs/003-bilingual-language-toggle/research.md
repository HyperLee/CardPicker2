# 研究: 雙語語系切換

## Decision: 使用 ASP.NET Core localization middleware 與 culture cookie

**決策**: 在 `Program.cs` 註冊 `AddLocalization(options => options.ResourcesPath = "Resources")`、Razor Pages view/DataAnnotations localization 與 `RequestLocalizationOptions`。支援 culture 白名單固定為 `zh-TW` 與 `en-US`，預設為 `zh-TW`。語系偏好使用 ASP.NET Core culture cookie `.AspNetCore.Culture`，由 `CookieRequestCultureProvider.MakeCookieValue` 產生，格式由 framework 處理。

**理由**: ASP.NET Core 官方文件將 localization 拆成三個任務：讓內容可本地化、提供支援語系的 resources、實作每個 request 的 culture selection。Request localization middleware 會在每個 request 檢查 provider 並設定 request culture；cookie provider 是 production app 常用的使用者語系偏好保存方式。這符合規格要求「伺服器端可讀取的語系偏好 cookie」，也能讓 Razor Pages、PageModels、DataAnnotations 與服務層訊息使用同一個 current culture。

**Alternatives considered**:

- `localStorage`: 可立即由前端讀寫，但伺服器端 Razor render 與 DataAnnotations 無法在 request 開始時可靠讀取，不符合 FR-007。
- `Accept-Language` header: 可推測瀏覽器偏好，但規格要求無有效偏好時預設繁體中文；瀏覽器 header 不是使用者在本站的明確選擇。
- Query string culture: 方便測試，但會污染 URL、容易遺失或分享偏好，不適合作為主要偏好保存。

**來源**: Microsoft Learn, [Globalization and localization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)

## Decision: 僅讓 cookie provider 決定語系，無 cookie 時回到 zh-TW

**決策**: `RequestLocalizationOptions` 只保留 `CookieRequestCultureProvider` 作為 request culture provider；不使用 `AcceptLanguageHeaderRequestCultureProvider` 作為 fallback。無 cookie、cookie 格式不合法、culture 不在白名單內或 provider 無法解析時，使用 `DefaultRequestCulture = zh-TW`。

**理由**: 規格 FR-003、FR-008 與成功標準 SC-007 要求無偏好或偏好無效時 100% 預設繁體中文。若保留 framework 預設 provider 順序，瀏覽器 `Accept-Language` 可能讓第一次造訪自動進入英文，違反產品規則。

**Alternatives considered**:

- 保留 query string + cookie + Accept-Language 預設順序: 便於開發，但不符合預設 zh-TW。
- cookie 優先、Accept-Language 次之: 仍可能在第一次造訪或 cookie 遺失時進入英文。
- 自訂 provider 讀取自訂 cookie: 可行但不必要，ASP.NET Core 內建 cookie provider 已能提供標準格式。

## Decision: 使用 shared resource key 管理 UI 與訊息文字

**決策**: 建立 `Resources/SharedResource.zh-TW.resx` 與 `Resources/SharedResource.en-US.resx`，以穩定 key 管理導覽、按鈕、欄位標籤、狀態、成功、失敗、驗證、復原與 fallback prompt。Razor views 注入 `IStringLocalizer<SharedResource>` 或 shared HTML localizer；PageModels 與服務結果以 message key + arguments 傳遞，最後由 PageModel 或 view 依 current culture 呈現。

**理由**: 使用穩定 key 比以繁中原文作為 resource key 更容易審查「英文缺漏」與避免文案異動造成查找失效。Microsoft 文件指出 `IStringLocalizer` 以 ResourceManager 在 runtime 依 culture 查找字串，DataAnnotations 也可導向 shared resource。讓服務回傳 key 而非直接回傳中文，可降低業務規則與 UI 語言耦合。

**Alternatives considered**:

- `IViewLocalizer` per Razor file: 適合 view 專屬文字，但共用訊息、DataAnnotations 與服務結果會分散。
- 硬編碼兩語系 dictionary: 不符合 ASP.NET Core localization 慣例，DataAnnotations 整合成本較高。
- 服務直接注入 localizer 並回傳 localized string: 實作較快，但會讓服務輸出依 current thread culture 改變，測試與錯誤碼追蹤較不穩定。

**來源**: Microsoft Learn, [Globalization and localization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)

## Decision: 卡牌 JSON 升級為 schema v2 localizations，讀取 v1 但只寫 v2

**決策**: `CardLibraryDocument.CurrentSchemaVersion` 升級為 2。v2 每張卡牌以 `localizations` 儲存 `zh-TW` 與 `en-US` 的 `name`/`description`。讀取 schema v1 時保留 `Id`、`MealType`、`Name`、`Description`，映射為 `zh-TW` localization，`en-US` 設為缺漏；直到下一次成功寫入才原子保存為 v2。缺檔時建立完整雙語 v2 seed data。unsupported schema、corrupted JSON 與驗證失敗仍 block card operations 並保留原檔。

**理由**: 規格同時要求既有英文缺漏卡牌可 fallback、不覆蓋 corrupted user data、seed data 兩語系完整，以及新增/編輯後卡牌必須具備兩語系內容。讀取 v1 可避免把現有合法使用者資料視為損毀；只寫 v2 能逐步收斂到雙語資料模型。

**Alternatives considered**:

- 直接覆寫現有 v1 為雙語 seed: 會遺失使用者卡牌，違反資料完整性。
- v1 永久保留 `name`/`description` 並額外加英文欄位: 長期 schema 會混雜，重複偵測與 projection 複雜度增加。
- 阻擋所有 v1 檔案直到手動修復: 不符合英文缺漏 fallback 要求，且降低升級可用性。

## Decision: 搜尋與顯示使用目前語系可見投影

**決策**: 新增 `MealCardLocalizationService` 產生 `LocalizedMealCardView`。每張卡牌在目前語系下有 `DisplayName`、`DisplayDescription`、`IsFallback` 與 `MissingTranslationCultures`。英文語系若缺少英文內容，投影使用繁中內容作為 visible fallback，並提供補齊英文翻譯的 action link。搜尋關鍵字只比對目前語系投影的 `DisplayName`。

**理由**: FR-012 與 FR-018 明確要求搜尋依目前語系的可見餐點名稱，且英文缺漏時搜尋 fallback 名稱。把投影集中在 service 可避免 PageModel 各自判斷 fallback，並讓 draw、details、list 與 form prompt 使用同一規則。

**Alternatives considered**:

- 搜尋同時比對兩語系名稱: 容易讓使用者在英文模式輸入中文或在繁中模式輸入英文得到非預期結果，違反目前語系搜尋規則。
- 只在 view fallback，不在 service fallback: 搜尋、duplicate detection 與 draw 結果會與畫面顯示不一致。

## Decision: Duplicate detection 比對兩語系可見 name+description

**決策**: `DuplicateCardDetector` 對同餐別的每張卡牌建立兩組 normalized pair：`zh-TW` 使用繁中內容，`en-US` 使用英文內容；英文缺漏時使用英文模式可見 fallback pair。新增/編輯時若任一語系 pair 的 `Name.Trim()` 與 `Description.Trim()` 以 `OrdinalIgnoreCase` 完全相同，且餐別相同、ID 不同，即拒絕。

**理由**: FR-016 與 DI-005 要求只要繁中或英文任一語系形成相同正規化名稱與描述組合就視為重複。把 fallback pair 納入可防止英文模式顯示兩張看起來完全相同的卡牌。

**Alternatives considered**:

- 只檢查繁中欄位: 英文模式可出現 duplicate。
- 只檢查完整雙語卡牌的英文欄位: 既有英文缺漏卡牌在英文 fallback 下可能重複。
- 檢查 name-only duplicate: 會拒絕同名不同描述的合法卡牌，違反既有資料規則。

## Decision: 語系切換使用 state-changing POST 與 progressive enhancement 保留頁面狀態

**決策**: shared layout 的語系切換為 Anti-Forgery POST `POST /Language?handler=Set`，欄位包含 `culture` 與本機 `returnUrl`。基礎行為設定 cookie 後 redirect 回原頁並保留 query string。為滿足未送出輸入、validation message 與已揭示 draw result 不被清除，`site.js` 會在切換前保存目前頁面的表單欄位與可見狀態到 session-scoped transient state，重新 render 後還原並以目前語系重跑 client validation；draw result 使用 result card ID 與 meal type 作為可重新 render 的狀態，不重新抽卡。

**理由**: 設定 culture cookie 是會改變瀏覽器狀態的操作，必須使用 POST 與 Anti-Forgery。Razor Pages server render 需要下一次 request 才能完整套用新的 culture；progressive enhancement 用於補足規格對「不清除目前操作狀態」的要求。

**Alternatives considered**:

- 語系切換 GET link: 容易被預取或第三方觸發，且缺少 Anti-Forgery。
- 純 client-side dictionary 切換: 伺服器驗證訊息與 Razor render 無法自然跟隨 current culture。
- POST 後單純 redirect: 對搜尋 query 可行，但會清除未送出表單與 POST-returned validation state。
