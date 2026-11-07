# 外觀分頁「新增／刪除主題」設計（主題專案）

日期：2026-11-07。作者（NAI）和 Claude 在對話裡討論定案，作者 2026-11-07 同意照這份實作。執行流程在 `openspec/changes/theme-projects/`。

## 1. 為什麼要做

- 本模組內建的外觀是 DLC 配色五款（帝國、信條、基因實驗室、虛空、星航）加上萘伊像素主題。玩家要更多外觀，就訂閱別人做的主題模組，或自己做。
- 現在自己做主題的門檻在「做出第一版並看到效果」這一步：
  - 要複製 `docs/examples/ExampleTheme` 改名、改 XML，或裝 Python＋Pillow 跑 `tools/make_theme.py`。
  - 改完圖要重開遊戲。開發模式的「重新載入主題貼圖」（`BarAppearanceDef.ReloadTextures`）可以免重開，但只有開發模式看得到，而且只讀已載入模組的資料夾，新做的主題模組要先啟用、重開一次才讀得到。
- 目標：只會畫圖的作者在遊戲裡按一下，就拿到一整套能直接改的主題檔案；改 PNG 後馬上在真正的底部列上看到結果；做完那個資料夾就是一個主題模組，可以啟用、可以用遊戲內建的功能上傳工作坊。

## 2. 名詞

| 名詞 | 意思 |
|---|---|
| 預設主題 | 一個 `BarAppearanceDef`，一整套風格一致的外觀。內建的、工作坊主題模組的都是 |
| 自訂主題 | 第十三輪做的：每顆按鈕各自從不同主題挑零件混搭；可以存成 `Themes/<名稱>/theme.xml` |
| **主題專案**（本設計新增） | 由本模組「新增主題」產生的主題模組資料夾，裡面有本模組寫的標記檔。它是預設主題那一層的東西，做好後它的零件也會出現在自訂主題的選圖清單 |

外觀分兩層（預設主題、自訂主題）是作者定的分法；參數（按鈕分頁：長寬、順序、顯示方式、顯示內容）不屬於主題，主題不改參數。

## 3. 目標、不做的、不能發生的

**目標**
1. 外觀分頁有「新增主題…」：產生一整套空白主題檔案（圖片＋顏色），新增後不用重開遊戲就能在清單裡選到、在底部列上看到。
2. 作者改 PNG（或 Def 的顏色）後，不開開發模式也能重新載入；設定視窗開著時會自動偵測檔案變動。
3. 只有主題專案能刪除；刪除可以復原（搬走，不是真的刪掉）。
4. 主題專案本身就是合法的主題模組：在模組清單啟用後和一般主題模組完全一樣，可以用遊戲內建的上傳功能發到工作坊。

**不做（Non-goals）**
- 從現有主題複製出新主題（版權問題、範圍）。
- 在本模組裡上傳工作坊（用遊戲內建的）。
- 逐顆按鈕插圖（`buttonImages`）、裝飾圖（`decorations`）的產生或編輯介面。空白主題不附這兩樣；`docs/API.md` 寫怎麼手動加。作者說過裝飾圖（萘伊原本左下的立繪）不是主要做法。
- PNG 以外的格式。主題專案的貼圖和熱加載只讀 `.png`；遊戲本身讀得了 jpg、dds 等，啟用成模組後照遊戲的規則。之後有人要求再加 JPG（簡單）或 DDS（要借遊戲的 DDS 讀取程式，中等）。
- 刪除自訂主題存檔（`Themes/`）。（Claude 的決定，範圍控制；之後可以另外加）
- 改 `make_theme.py`、`ExampleTheme`，或決定範本怎麼發佈（`docs/workshop/theme-template-release.md` 照舊）。
- UI 縮放 2 倍時九宮格可能變糊（`themeAtlasMaxSize` 72 是以 UI 單位算，推定，沒驗證）。

**不能發生的（Anti-goals）**
- 只想整理按鈕的玩家多看到東西或被打擾：「新增主題…」只在外觀分頁的主題清單後面，不跳提示、不改任何預設值。
- 動到不是本模組新增的檔案：內建主題、工作坊主題、手動做的主題模組都不能被刪、被改。
- 紅字；每幀配置記憶體（自動偵測不能每幀掃檔案）。
- 主題專案啟用後被載入兩次（遊戲載一次、本模組又載一次）。

## 4. 作者的流程

1. 外觀分頁 → 主題清單最後（內建主題卡片、自訂主題存檔之後）→「新增主題…」。
2. 對話框填名稱（必填）、作者、說明（選填）；packageId 自動產生、可以改，和「做成排序模組」的對話框一樣。
3. 本模組在遊戲的 `Mods\` 建好資料夾，清單多一張卡片並自動選上它；底部列馬上變成空白主題的樣子（上色九宮格、格線、準心）。
4. 卡片上按「打開資料夾」，照 `_Guide.png` 的說明改 PNG，存檔。
5. 設定視窗開著的話幾秒內自動更新；也可以按卡片上的「重新載入」。
6. 做完：在模組清單啟用它（之後就是一般主題模組），或用遊戲內建的上傳工作坊。不要了就按「刪除」。

## 5. 設計

### 5.1 新增

- 位置：卡片和對話框在外觀分頁（`Dialog_MainButtonBar.DrawAppearanceTab`）主題清單的最後一段，自訂主題存檔（`DrawSavedThemes`）之後。
- 對話框：名稱、作者、說明、packageId。作者的預設值沿用上次「做成排序模組」或匯出用的作者名（實作者看現有的 `ArrangementMeta` 怎麼記）。packageId 預設 `<作者>.mbbstheme.<名稱>`，用 `SharingNames.Identifier` 正規化，和已安裝模組撞名時加後綴。名稱空白、packageId 不合法時「建立」是灰的並提示原因。
- 寫到哪：`GenFilePaths.ModsFolderPath\<資料夾名>`，資料夾名用 `SharingNames.Unique` 避開撞名。`Mods` 寫不進去 → 訊息說明原因（含路徑），什麼都不建立。**不做後備位置**：主題專案的意義就是一個模組，放在 `Mods` 以外的地方還得教作者搬，增加一種要測的情況（Claude 的決定，寫設計時從「有後備位置」改成這樣）。寫 About、前置項網址的邏輯和 `ArrangementIO.ExportModFolder` 共用。
- 產生的檔案（照 `ExampleTheme` 的結構，`<Id>` 是由 packageId 衍生、可當 defName 和資料夾名的識別字）：

  | 檔案 | 內容 |
  |---|---|
  | `About/About.xml` | name、author、packageId、`modVersion` 1.0.0、`supportedVersions` 1.7、`modDependencies` 本模組（帶 `downloadUrl`／`steamWorkshopUrl`，沿用 `ArrangementIO` 產生前置項的邏輯，不能再出現 `needs to have <downloadUrl>` 黃字）、`loadAfter` 本模組、description |
  | `About/Preview.png` | 工作坊預覽圖的佔位圖，640×360 |
  | `Defs/<Id>_Theme.xml` | 一個 `BarAppearanceDef`：defName `<Id>_Theme`、label＝名稱、description、author、`displayOrder`（Tuning 值，排在內建主題後面）；`button`／`buttonMouseover`／`buttonActive`／`buttonDisabled`；`labelColor`／`labelMouseoverColor`／`labelActiveColor`；`barBackground`（Tile）、`drawerBackground`（Atlas）、`moreButton`、`arrowLeft`／`arrowRight`、`drawerSettingsIcon`、`preview`；`buttonImage`（Cover、置中）。每個欄位上面一行英文註解說明它管哪裡，最後註解指向 `API.md` 的對應節（`buttonImages`、`decorations` 怎麼加） |
  | `Textures/<Id>/*.png` | 下表的空白圖 |
  | `_Guide.png` | 說明圖（放根目錄，遊戲不會載入）：底部列示意圖，每塊標出對應的檔名、尺寸、九宮格切法、插圖的安全範圍 |
  | `MBBS_ThemeProject.xml` | 標記檔（根目錄，遊戲不會載入）：格式版本、packageId、defName、建立時間、本模組版本。**有這個檔的資料夾才算主題專案** |

- 空白圖（尺寸照現有規格：九宮格超過 72×72 只會被縮小；插圖是按鈕的 2 倍）：

  | 檔案 | 尺寸 | 樣式 |
  |---|---|---|
  | `Button.png`、`ButtonMouseover.png`、`ButtonActive.png`、`ButtonDisabled.png` | 64×64 | 上色九宮格：四角、四邊、中間各一種深淺，切線畫在 1/4 處。四種狀態各一色調（一般藍、滑鼠移上綠、開啟中橘、停用灰），作者把滑鼠移上去就知道哪張管哪個狀態。中間半透明（插圖畫在按鈕框下面，全不透明會蓋掉插圖） |
  | `More.png`、`Drawer.png` | 64×64 | 同樣的上色九宮格，用另一個色調（例如紫） |
  | `Bar.png` | 寬 256、高 36 | 列背景，Tile：格線＋左右兩端的接縫記號 |
  | `ButtonPicture.png` | 240×72 | 準心、X、外框，加上「按鈕寬 120／80／40 時 Cover 裁切後還看得到的範圍」三個巢狀框 |
  | `ArrowLeft.png`、`ArrowRight.png` | 32×32 | 箭頭輪廓 |
  | `Gear.png` | 64×64 | 齒輪輪廓 |
  | `Preview.png`（Textures 裡，主題卡片的預覽） | 600×36 | 用上面的圖拼出的底部列示意 |

  四種狀態的色調、各區塊的深淺是 Claude 的建議，實作時可以微調，但「四種狀態一眼分得出來」「九宮格的切線看得見」「插圖的裁切範圍看得見」這三點要保留。
- 空白圖從哪來：本模組隨附一套做好的 PNG（含 `_Guide.png`、About 的 Preview），新增時複製過去。放在 repo 根目錄的 `ThemeKit/`（遊戲不會載入），`tools/make_release.py` 的 `FOLDERS` 和 `RimClaude\模組開發\環境\deploy.ps1` 的 `$folders` 都要加上它。這套圖用 repo 的 Python 腳本（`tools/make_theme_kit.py`，Pillow）產生、產生結果也 commit，和 `gen_theme_textures.py` 的做法一樣。
- 新增完：清單多一張卡片、`working.appearance` 設成新的 defName（自動選上），訊息列顯示資料夾路徑。

### 5.2 不用重開就能用（執行中載入）

- 遊戲啟動完（Def 都載入後）和每次新增後，掃描主題專案：`Mods\` 底下有 `MBBS_ThemeProject.xml` 的資料夾。
- **判斷「遊戲已經載入它」只看一件事**：標記檔的 packageId（不分大小寫）是否在 `LoadedModManager.RunningMods` 裡。用執行中的模組清單，不用 `ModsConfig`：玩家在模組清單勾選後還沒重開，遊戲其實還沒載入它，這時仍由本模組載入。在 `RunningMods` 裡 → 遊戲已經載入，本模組不再載入（避免載兩次）。
- 不在 → 本模組讀它 `Defs\*.xml` 裡**所有**的 `BarAppearanceDef` 節點（作者可能自己多加一款），用遊戲自己的 Def 解析（`DirectXmlToObject` 這條路，實作者確認 1.7 的 API）產生物件，所以作者照 `API.md` 手動加的任何合法欄位（`buttonImages`、`decorations`、各種 fit）都要能用；接著 `ResolveReferences`、補 shortHash，加進 `DefDatabase<BarAppearanceDef>`（全模組都用 `DefDatabase<BarAppearanceDef>.GetNamedSilentFail` 找主題，所以加進去後選主題、存讀檔、自訂主題選圖都能直接用；`BarModel.Appearances` 每次都重新排序，沒有另外的快取要清，實作者再確認一次）。defName 和已經存在的撞到 → 那一款略過，log 一行黃字。
- 標記檔只用來認出「這是主題專案」和取 packageId。它記的 defName 只是資訊；這個專案有哪些主題，以掃描 `Defs\*.xml` 的結果為準（作者改了 defName 也照樣運作）。
- 這種主題的貼圖不經過 `ContentFinder`：`BarAppearanceDef` 用一個 `[Unsaved]` 欄位記住它的專案資料夾，`Tex`／`TexQuiet` 從 `<資料夾>\Textures\<path>.png` 讀（`LoadImage`，和 `ReloadTextures` 現有的讀檔方式一樣）；`modContentPack` 是 null，用到它的地方（卡片來源文字、`ThemeValidator`、`LoadFromModFolder`）都要處理。讀不到 → 那個零件用遊戲的外觀，警告一次，不紅字。
- 卡片的來源文字：已啟用的照現在顯示模組名稱；本模組載入的顯示「主題專案（未啟用）」。
- 存檔裡選了某主題專案，但下次啟動時它不見了（被刪、被搬走）→ 現有機制退回原版（`BarConfig` 找不到 defName 時用 Vanilla）。

### 5.3 重新載入

- 主題專案的卡片（選中時）多一列按鈕：「重新載入」「打開資料夾」「刪除」。不開開發模式也有。
- 「重新載入」：
  - 本模組載入的（未啟用）：重讀 Def XML 和全部 PNG。Def 的**所有欄位**都要生效（顏色、fit、`buttonImage`、`buttonImages`、`decorations`、`suggestedLayout` 等），所以除了就地更新欄位，還要清掉跟著 def 的快取（`textures`、`buttonImageIndex`、`workerInt`，以及繪製端如果另外留了 Texture 參考也要清；實作者把用到 `BarAppearanceDef` 的快取都找出來）。作者在 XML 新增一款主題 → 加進清單；刪掉一款 → 從清單拿掉（目前選它的話回到原版）。
  - 已啟用的：只重讀 PNG（沿用 `ReloadTextures`），提示「改了 Defs 要重開遊戲」。
  - 完成後跑 `ThemeValidator.Validate(def)`、`CustomThemes.ResetCache()`，訊息列顯示讀了幾個檔案。
- 自動偵測：設定視窗開著、外觀分頁顯示、選中的是主題專案時，每隔 `Tuning` 的間隔（預設 1 秒）比對它 `Textures\` 下 `.png` 和 `Defs\` 下 `.xml` 的最後修改時間；有變就做一次「重新載入」。只在間隔到時才查檔案，不是每幀。檔案正在被繪圖軟體寫入（讀失敗）時下一輪再試，不報錯。
- 開發模式現有的「重新載入主題貼圖」按鈕維持不變。

### 5.4 刪除

- 只有主題專案（資料夾有標記檔）的卡片有「刪除」。內建主題、工作坊主題、手動做的主題模組都沒有。判斷用標記檔，不用資料夾位置。
- packageId 在 `LoadedModManager.RunningMods` 裡，或在 `ModsConfig` 的啟用清單裡（勾了還沒重開）→ 按鈕灰掉，提示「先在模組清單停用並重開遊戲」。
- 按下 → 確認視窗（名稱、資料夾路徑、「會搬到 …\DeletedThemes\，要復原就搬回 Mods」）→ 整個資料夾搬到 `ModPaths.DataFolder\DeletedThemes\<資料夾名>_<yyyyMMdd_HHmmss>`。
- 搬完：這個專案載入的所有主題都從清單拿掉（從 `DefDatabase` 移除或標記成隱藏，實作者看哪個安全）；目前選的是它 → `working.appearance` 回到原版；自訂主題用到它的零件 → 照現有「找不到主題」的處理（用主題自己的圖，一行黃字）。
- 搬移失敗（檔案被鎖）→ 訊息說明原因，什麼都不改。

### 5.5 共同要求（HANDOFF §4）

- 數值放 `Tuning.xml`：自動偵測間隔、主題專案的 `displayOrder`、後備資料夾名這類，每個有預設值。
- 新增的介面文字 29 種語言（`tools/i18n/English.json` → `gen_translations.py`）。
- 純邏輯放 `Core/` 並有單元測試：packageId／資料夾名／defName 的產生與撞名處理、標記檔讀寫與版本判斷、「能不能刪」的判斷、修改時間比對。
- 文件：`docs/API.md` 新增一節「在遊戲裡新增主題」（流程、檔案清單、怎麼手動加 `buttonImages`／`decorations`、上傳工作坊要注意的），`docs/API.zh-TW.md` 節錄。
- 實機測試清單：`docs/testing/theme-projects.md`。

## 6. 決定紀錄

| 項目 | 決定 | 誰定的 |
|---|---|---|
| 外觀分兩層：預設主題、自訂主題；新增的是預設主題那層 | 是 | 作者 |
| 外觀分頁加新增、刪除；內建和工作坊的不能刪，只能刪自己新增的 | 是 | 作者 |
| 按鈕分頁維持現狀，主題 Def 先不加「文字／圖示位置」欄位 | 是 | 作者 |
| 空白主題只管圖片和顏色（按鈕背景、插圖、捲動鍵、抽屜，加列背景、文字顏色） | 是 | 作者（列背景、文字顏色是 Claude 補的，作者看過說沒問題） |
| 只用 PNG，之後有人要求再加其他格式 | 是 | 作者 |
| 不預設 4K，用現有規格的尺寸 | 是 | 作者同意 Claude 的建議 |
| 放 `Mods\`；寫不進去就報錯，不做後備位置 | Claude 的決定 | 作者看過的版本有後備位置；寫設計時為了少一種要測的情況改成報錯 |
| 「遊戲已經載入」只看 packageId 是否在 `RunningMods`；刪除另外擋 `ModsConfig` 已勾選的 | Claude 的決定 | 寫設計時定的（實作者檢查提出） |
| 執行中載入用遊戲的 Def 解析，支援任何合法欄位；重新載入時所有欄位都生效 | Claude 的決定 | 同上 |
| 一個專案有哪些主題以 `Defs\*.xml` 為準，標記檔只用來認專案 | Claude 的決定 | 同上 |
| 執行中加進 `DefDatabase`、啟用後交給遊戲 | Claude 的決定 | 同上 |
| 上色九宮格、格線、準心＋裁切框的空白圖樣式 | Claude 的決定 | 同上 |
| 不附裝飾圖、逐顆插圖 | Claude 的決定 | 同上 |
| 刪除是搬到 `DeletedThemes\` | Claude 的決定 | 同上 |
| 自動偵測檔案變動 | Claude 的決定 | 同上 |
| 自訂主題存檔不加刪除 | Claude 的決定（範圍控制） | 寫設計時定的，作者還沒特別看過 |
