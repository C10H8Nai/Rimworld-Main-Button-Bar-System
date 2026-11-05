# 第十三輪實作計畫：自訂主題（二）

> 給接手的 Claude（本地或雲端）：照任務順序做，每個任務完成就編譯（0 警告）、跑測試、連同 DLL 一起 commit。規矩見 `docs/HANDOFF.md` 第 3、4 節。

**目標**：完成自訂主題的後半，包括背景插圖、匯入 PNG、匯出與載入自訂主題、複製／貼上樣式，以及排序分享帶上自訂主題用到的圖。

**依據**
- 設計：`docs/superpowers/specs/2026-11-05-custom-theme-design.md`（第 3.4、3.5、3.6 節，第 4 節第 7、12、16、17、18 點）。
- 整體計畫：`docs/superpowers/plans/2026-11-05-roadmap.md`。
- 第十二輪的實際做法：`docs/testing/round-12.md`。

**前提**：#14（第十輪）、#15（第十一輪）、第十二輪的 PR 都已合併。從最新的 main 開 `claude/round-13-custom-theme-2`；如果還沒合併，就從第十二輪的分支往下開。

## 現況（第十二輪結束時）

| 東西 | 位置 |
|---|---|
| 自訂主題資料 | `Config/CustomTheme.cs`：`ButtonLook`（一層的欄位）、`CustomThemeData`（`all` 加上 `buttons`，`Layer`／`GetOrCreate`／`Reset`／`Prune`／各項解析）、`CustomThemes`（worker 快取、`AtlasRef`="主題defName\|欄位"、`AtlasTexture`）、`BarAppearanceWorker_Custom` |
| 繪製的接點 | `API/BarAppearanceDef.cs`：worker 的 `ChangesButtons`／`LabelAlignFor`／`LabelFontFor`／`IconLookFor`；`ButtonPainter.Draw`；背景插圖由 `ThemeAtlasSwap`（`Patches/MainButtonsRootPatches.cs`）呼叫 `theme.ButtonImage(def)`、`theme.DrawButtonImage(rect, image)` |
| 介面 | `UI/Dialog_MainButtonBar.Appearance.cs`：`DrawAppearance`、`DrawLookList`、`DrawLookDetail`、`DrawLookPreview`、`DrawLookAtlas`、`AtlasOptions` |
| 圖示的 PNG（參考做法） | `Config/CustomIcons.cs`（`file:` 前綴、`Icons/` 資料夾、`OpenFolder`、快取與「找不到」只記一次）、`UI/Dialog_IconPicker.cs` |
| 匯出、匯入（參考做法） | `UI/Dialog_MainButtonBar.cs` 的 `ExportOptions`／`ImportOptions`／`AskInfo`、`Config/ArrangementIO.cs`（`BundleIcons` 會把 PNG 圖示包進排序模組） |

## 任務

### 任務 1：圖的參照統一成「主題貼圖或匯入的 PNG」

- `CustomThemes` 新增 PNG 參照，格式 `file:<檔名>.png`，指向 `<存檔資料夾>/MainButtonBarSystem/Images/`。
  - `ModPaths` 加 `ImagesFolder`。
  - 讀取比照 `CustomIcons.LoadFile`：檔名驗證、快取、找不到只警告一次。
  - 重新載入時 `Destroy` 舊的貼圖，避免 `CustomIcons` 那種洩漏（roadmap 的待辦有列）。
- `CustomThemes.AtlasTexture`（可以改名成 `Texture`）同時接受 `主題defName|欄位` 和 `file:`。
- 主題貼圖的參照擴大到主題的背景插圖：`buttonImage`、`buttonImages` 的 texture。格式 `主題defName|image:<texture 路徑>`，解析時用 `theme.Tex(path)`。
- `ButtonLook` 的四個 atlas 欄位沿用字串，所以按鈕背景也能用匯入的 PNG（設計 3.6）。
- 測試：參照字串的解析放進 Core（例如 `Core/ImageRef.cs`：種類、主題、欄位或路徑），加單元測試。

### 任務 2：選圖視窗

- 新增 `UI/Dialog_LookImagePicker.cs`，比照 `Dialog_IconPicker`，分兩頁：
  - 「主題貼圖」：列出每款主題的四種按鈕背景和背景插圖，附縮圖。
  - 「匯入的 PNG」：`Images/` 裡的檔案，加上「開啟資料夾」「重新整理」。
- 選了就呼叫 callback 回傳參照字串。
- 第十二輪按鈕背景用的 `AtlasOptions`（FloatMenu）改成開這個視窗。
- 尺寸提示（設計 4.12）：按鈕背景超過 `behaviour.themeAtlasMaxSize` 只顯示提示，不擋。

### 任務 3：背景插圖

- `ButtonLook` 加欄位，每個都要有「沒改過」值：
  - `image`（參照字串）。
  - `imageFit`（`ImageFit` 的列舉加 Default）。
  - `imageAnchor`、`imageVerticalAnchor`（加 Default）。
  - `imageOpacity`（百分比，-1）。
  - `imageInset`（-1）。
- 補上 `ExposeData`、`IsEmpty`、`Clone`。
- `CustomThemeData` 加解析：這顆 → 所有按鈕 → 主題的 `ButtonImage(def)`。
- `BarAppearanceWorker_Custom` 覆寫 `ButtonImage(MainButtonDef)`：回傳合併後的 `BarButtonImage`。
  - 每顆按鈕快取一個物件，資料改了才重建，避免每幀配置。可以在 `CustomThemeData` 加一個版本號，每次編輯（`GetOrCreate` 之後的寫入、`Reset`、`Prune`）就加一。
  - 貼圖用任務 1 的參照解析，`DrawButtonImage` 若需要可以一起覆寫。
- 介面：`DrawLookDetail` 加「背景插圖」一節。
  - 選圖（任務 2）。
  - 填法：Cover／Fit／Stretch。
  - 錨點：水平和垂直各三格，比照圖示的位置選格。
  - 透明度、內縮：`IntSetting`，範圍新增在 `Tuning.xml` 的 `ranges`，例如 `lookImageOpacity`、`lookImageInset`。
  - 每一項都要有預設值按鈕。
- 預覽 `DrawLookPreview` 也畫插圖，順序是插圖 → 框 → 文字，和實際一致。

### 任務 4：複製樣式／貼上樣式

- 選一顆按鈕或「所有按鈕」時，在「重設這個樣式」旁邊加「複製樣式」「貼上樣式」。
- 剪貼簿用一個靜態的 `ButtonLook` 複本，不用系統剪貼簿。貼上時 `defName` 換成目標。
- 剪貼簿是空的時候，「貼上樣式」是灰的。

### 任務 5：匯出、載入自訂主題

- 檔案放在 `<存檔資料夾>/MainButtonBarSystem/Themes/<名稱>/`：
  - `theme.xml`：根節點 `MainButtonBarSystemTheme`，內容是 `meta`（名稱、作者、說明，沿用 `ArrangementMeta` 的欄位或另做小類別）、`baseTheme`（起點主題 defName）、`customTheme`（`CustomThemeData`）。
  - 用到的 `file:` PNG 複製到同一個資料夾，參照改成相對於這份主題。載入時再複製進 `Images/`，檔名衝突就自動改名，參照也跟著改。
- 外觀分頁的底部「匯出…」「匯入…」選單加項目（設計 3.5：和按鈕分頁一樣管整套）：
  - 匯出：「存成自訂主題」。用 `AskInfo` 填名稱、作者、說明，檔名重複就加 (2)。
  - 匯入：每份自訂主題一項，再加「開啟自訂主題資料夾」。
- 預設主題的清單：在內建和附屬模組主題之後，加一段「自訂」卡片，列出 `Themes/` 裡的每一份。
  - 點了就把它的內容複製進設定：`appearance = baseTheme`、`appearanceMode = Custom`、`customTheme = 複本`（設計 4.17：存檔不依賴那份檔案）。
  - 起點主題沒安裝時改用原版，並提示一次。
- 讀檔失敗、格式太新：照排序匯入的方式提示，不出紅字。

### 任務 6：分享排法時帶上自訂主題的圖

- 排序匯出本來就帶著 `customTheme`（Theme 部分）。
- `ArrangementIO.BundleIcons` 擴大成也打包自訂主題用到的 `file:` PNG，「做成排序模組」時一起帶走。
- 匯入報告（`UI/Dialog_Sharing.cs`）：對方缺圖時，列出缺少的自訂主題圖片。

### 任務 7：字串、文件、測試清單

- 新字串：英文、繁中先寫好，其餘 27 種語言交給翻譯子代理。規則照 HANDOFF 第 4 節：簡中用「」、日文用半形標點；「預設主題」是預先設計好的意思，不能和「預設值」混用。
- `docs/API.md`：自訂主題的資料格式（`appearanceMode`、`customTheme`）、圖的參照格式、自訂主題檔的結構。
- `docs/testing/round-13.md`：
  - 背景插圖各項。
  - 匯入 PNG（含檔名衝突、刪掉檔案後的提示）。
  - 匯出後換一台遊戲（或刪掉 `Images/`）載入。
  - 複製貼上樣式。
  - 做成排序模組時帶圖。
- 派一個獨立的審查子代理，只給它設計文件和 diff；修完問題再開 PR，PR 說明寫給本機的提示詞。

### 任務 0：第十二輪審查留下的（先做）

第十二輪的審查已修：圖示錨點的邊距、顏色與背景逐項往上找（規格 4.5）、只改字級時文字照遊戲的位置、匯入預設主題時保留自己的自訂外觀、自訂模式沒有資料時自動建立、Prune 不再每次重建索引。

還沒做的：
- **主題自己的 `workerClass` 在自訂模式下不生效**：`BarAppearanceWorker_Custom` 繼承的是預設 worker。附屬主題如果覆寫了 `DrawMoreButton`、`DrawScrollArrow`、`ButtonAtlas` 等，切到自訂模式就會失去效果，而預覽用的卻是真的 worker。做法：在裡面包一個 `inner = baseDef.Worker`，每個 virtual 都轉給它，按鈕相關的後援也走 `inner`。內建主題都沒有自己的 worker，不受影響。
- **預覽和實際列不一致**：滑鼠移上、開啟中的預覽用的是 `DrawHighlight`；對齊沒設時，預覽一律置中（實際是照遊戲靠左）；「所有按鈕」的預覽沒有圖示。
- **介面裡寫死的數字**：`Dialog_MainButtonBar.Appearance.cs` 的 1f、2f、4f、×2、/3（縮圖、色塊、錨點小方塊的大小），改放進 `Tuning.xml` 的 `settingsWindow`。
- **三層合併沒有單元測試**（規格 §7）：把解析規則抽成不依賴遊戲的 Core 類別再測。
- **規格 4.6**：「所有按鈕」那一列顯示「N 顆有自己的設定」，並加「全部改回跟所有按鈕」。
- **規格 4.10**：選色先用遊戲內建的選色視窗（1.7 有就用），並加十六進位色碼框。
- **圖示大小範圍 8～64**：按鈕只有 36 高，超過的部分沒作用。改成 `lookIconSize` 上限跟按鈕高度一致，或在畫面上說明。
- **`docs/API.md`**：寫出 worker 新增的 `ChangesButtons`、`LabelAlignFor`、`LabelFontFor`、`IconLookFor`。

## 注意

- 沒用自訂主題時，繪製結果要和改動前完全一樣。
- 每幀路徑不能新增配置。插圖的 `BarButtonImage` 要快取，參照字串的解析結果也要快取。
- 不寫死參數：新的數值都進 `Tuning.xml`；每個新的可調項目都要有預設值按鈕。
- 第十二輪的偏離規格（起點主題＝預設主題選的那款；切回預設主題時保留自訂資料）維持不變。

## 之後（不在這一輪）

- 一鍵做成主題模組：作者說之後有需求再決定。
- 第 14 輪：高度與兩排。開始前先問作者「高度」指什麼。
