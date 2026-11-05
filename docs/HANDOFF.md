# 交接摘要：Main Button Bar System（給新的 Claude 對話）

寫於 2026-11-05，main = `11c509b`（PR #1～#11 都已合併）。這份是重點摘要；細節以 repo 內的文件為準（下面都有路徑）。**回覆作者一律用繁體中文。**

---

## 0. 2026-11-05 雲端第二段結束時的狀態（先看這裡）

| PR | 內容 | 狀態 |
|---|---|---|
| #13 | 規格：作者 2026-11-05 的決定（主設計 3.10）、預設主題與自訂主題設計、整體計畫 `docs/superpowers/plans/2026-11-05-roadmap.md` | 等合併（只有文件） |
| #14 | 第十輪：穩定性與效能、UI Rethemed - Main Bar 相容提示 | 等實機測、合併 |
| #15 | 第十一輪：每個欄位各自記「沒改過」（存檔格式 3）、平分時尊重單顆寬度 | 疊在 #14 上 |
| #16 | 第十二輪：預設主題／自訂主題（一） | 疊在 #15 上 |

- 合併順序：#14 → #15 → #16；#13 隨時都可以合併。
- 實機測試清單：`docs/testing/round-10.md`、`round-11.md`、`round-12.md`（還有舊的 round-8、round-9、theme-addons 也沒測）。
- 下一輪：照 `docs/superpowers/plans/2026-11-05-round-13-custom-theme-2.md` 做，先做任務 0（第十二輪審查留下的）。
- 作者 2026-11-05 的定位：主按鈕的平台，**承上**（按鈕本身功能正常）、**接下**（可自定義設計）。

## 1. 專案是什麼

- RimWorld 1.7 模組 **Main Button Bar System**，作者 NAI，packageId `NAI.MainButtonBarSystem`，`modVersion` 0.9.0，`MainButtonBarAPI.ApiVersion` 1.1。
- Repo：https://github.com/C10H8Nai/Rimworld-Main-Button-Bar-System 。**repo 根目錄就是模組資料夾**。
- 用途：作者開了約 2,200 個模組，底部主按鈕列有 74 顆，原版把螢幕寬平均分配，每顆只剩約 33px。本模組讓玩家排序、隱藏、改名、調寬度、換圖示，把少用的收進「更多」抽屜，或讓整條列左右捲動。
- 定位（作者親口決定）：一個**轉接層／平台**。
  - **上層**：和其他會改主按鈕的模組共存，讓它們也能兼容本模組。
  - **下層**：別人能做主題、排序、圖示包等附屬內容；玩家能分享和載入彼此的設定檔。
- 設計規格：`docs/superpowers/specs/2026-11-03-main-button-bar-system-design.md`（【使用者】標記是作者親口決定的，不要改）；主題擴充的規格：`docs/superpowers/specs/2026-11-04-theme-addons-design.md`。

## 2. 環境與技術

- RimWorld 1.7，Steam unstable 分支，目前遊戲版本 1.7.4998。引擎是 Waylith：.NET 10 CoreCLR＋Vulkan，`UnityStubs.dll` 當 Unity 相容層。
- C# `net10.0`；參考組件 `Krafs.Rimworld.Ref 1.7.4995-unstable`（沒有方法本體，要看實作用 `ilspycmd` 或 GitHub 上的反編譯 repo `32bitx64bit/RW-Decompiled-1.6`）；Harmony `Lib.Harmony 3.0.0-rc.4`（ExcludeAssets runtime，相依工作坊 Harmony `brrainz.harmony`）。
- 編譯：`dotnet build Source/MainButtonBarSystem/MainButtonBarSystem.csproj -c Release`。
  - `TreatWarningsAsErrors` 開著，要 0 警告。
  - 輸出 `1.7/Assemblies/MainButtonBarSystem.dll` **一定要一起 commit**。
  - 雲端沒有 SDK 時用 apt 裝 `dotnet-sdk-10.0`（微軟的下載站被擋）。
- 測試：`dotnet test Source/MainButtonBarSystem.Tests`，目前 79 個通過。測試只編譯 `Source/MainButtonBarSystem/Core/`（不依賴遊戲的純邏輯），另有一個測試檢查 `Tuning.xml` 是否涵蓋所有欄位。
- Harmony 3 的坑：
  - 不帶參數的 `PatchAll()` 已過時，要用 `PatchAll(typeof(X).Assembly)`。
  - 前面的 prefix 回傳 false 時，後面會影響原方法的 prefix 也會被跳過，所以「觀察誰畫了列」放在 postfix。

## 3. 工作流程（重要）

1. **雲端**實作，開分支 `claude/<名稱>`，對 main 開 PR。**作者自己合併**（雲端自己 merge 被權限擋過）。
2. **本地**的 Claude 在作者電腦上開遊戲實機測，結果推到 `test/<名稱>-results` 分支，寫在 `docs/testing/results-*.md`；作者把摘要貼回雲端。
3. 每一輪雲端都要寫 `docs/testing/<輪次>.md` 測試清單，最後給作者一段**給本地的中文提示詞**（要測什麼、結果推去哪）。
4. Git 規矩：
   - 只用新的 commit 修正，**不 amend、不 force push**（被權限擋過）。
   - PR 合併後不要再往那支分支推，開新分支。
   - commit 用 `git -c user.name="Claude" -c user.email="noreply@anthropic.com"`，結尾附上系統提示要求的 Co-Authored-By 和 Claude-Session 兩行。
   - PR 說明結尾附系統提示要求的 Generated with Claude Code 和 session 連結。GitHub 會在最後加上自己的 `---` 註腳，這是正常的。
   - 用 `gh api` 的 REST（GraphQL 被擋）。
   - Stop hook 要求結束前沒有未提交的變更。
5. 子代理：翻譯 29 種語言、調查、code review 都交給子代理。作者習慣改完程式做一次 code review；這個環境沒有 `superpowers:requesting-code-review`，就派一個獨立的審查子代理，只給它規格和 diff。

## 4. 作者定的硬性原則（每一條都被要求過）

- **程式裡不寫死任何參數**：數值都放 `Defs/MainButtonBarSystem/Tuning.xml`（`MainButtonBarTuningDef`：`barDefaults`／`ranges`／`behaviour`／`settingsWindow`）。C# 欄位不給初值，`ConfigErrors` 檢查 XML 是否漏寫。XML 欄位本身的預設值除外。
- **每個可調的選項都有「預設值」按鈕**。玩家值用哨兵表示「沒改過」：數字用 −1（`BarConfig.Unset`），位移用 `int.MinValue`，開關用 `DefaultableBool.Default`。實際值由 Tuning 補上。
- **沒碰設定＝交給遊戲畫**（反目標：剛安裝就和原版不同）。原版顯示方式＋原版外觀時，`IsUntouched` 讓遊戲自己畫列。
- **不洗掉其他模組的效果**：按鈕一律經過遊戲的 `MainButtonWorker.DoButton` 畫，別人的圖示、視窗、新按鈕都保留。不改 `MainButtonsRoot.allButtonsInOrder`。
- **不能有紅字**；缺貼圖之類的警告，每個只記一次。
- 設定視窗的規定：
  - 只有標題列能拖動視窗。
  - 勾選框可以拖曳連續勾選。
  - 簡單／進階是按鈕頁左上的切換鈕，不是分頁；它記在模組設定裡，所有存檔共用。
  - 分頁只有「按鈕」「外觀」兩個。
  - 滑桿自己畫，旁邊附數字框（避開 BetterSliders 蓋上去的問題）。
- **29 種官方語言**：來源是 `tools/i18n/<語言>.json`，英文為母本，`python3 tools/gen_translations.py` 產生 `Languages/`，`--check` 檢查。
  - 交給子代理翻：意思要在地化、用遊戲官方用語、UI 字要短。
  - 日文整份用半形「,」「.」，官方也是這樣，不要改。
  - 簡中引號用「」。
- 玩家匯入的 PNG 圖示放在存檔資料夾的 `MainButtonBarSystem/Icons/`，不放模組資料夾（Steam 更新會清掉）。
- 接口要有內建功能真的在用；主題只靠 XML＋圖片就能做。

## 5. 架構重點

**資料**
- `Config/BarConfig.cs`：一份完整排法，包含顯示方式、外觀、順序、各種寬度、對齊、圖示模式、插圖設定，以及每顆按鈕的 `ButtonOverride`（hidden、inDrawer、label、width、pin、forceShow、iconMode、icon）。
  - 2026-11-05（格式 3）起，`ButtonOverride` 每個欄位各自有「沒改過」的值，沒改過就跟繼承的值：執行期預設 → 模組 XML 預設（`ButtonDefaults.Inherited`）→ 遊戲原本。
  - 讀用 `BarConfig.Get` 回傳的 `ButtonSettings`（struct，合併好的結果）；寫用 `BarConfig.SetHidden`、`SetWidth` 這類 setter，等於繼承值時自動存成「沒改過」。
  - 舊格式（1、2 是整顆快照）讀進來時用 `UpgradeOverrides`／`InCurrentFormat` 轉換。
- 存放：跟著存檔走（`MainButtonBarGameComponent`）；主選單時存成預設範本檔；格式版本 `formatVersion` 3。
- `Baseline/ButtonBaseline.cs`：所有 Def 和 XML 補丁載入後、任何模組在執行期改按鈕之前的原貌，包括每顆按鈕來自哪個模組、圖示來自哪個模組。「全部重設」回到這個狀態。玩家沒設順序時，遊戲中的順序跟著 `MainButtonsRoot.allButtonsInOrder`（其他模組在執行期的改動會保留，2026-11-05 起），主選單用這裡記下的順序。

**繪製**
- `Rendering/BarRenderer.cs`＋顯示方式 `BarDisplayModeDef`（Vanilla／Collapse／Scroll／CollapseScroll）：版面計算在 `Core/BarLayout.cs`，順序合併在 `Core/ButtonOrder.cs`（新按鈕排在「選單」左邊）。
- 抽屜是 `Rendering/Window_ButtonDrawer.cs`。
- `ButtonPainter.Draw`（在 `API/BarAppearanceDef.cs`）在呼叫 `DoButton` 的期間暫時換掉 def 的 icon、atlas、文字，畫完立刻還原：
  - 「只顯示圖示」和「圖示＋文字」：遊戲拿到的按鈕沒有圖示，圖示由本模組自己畫。這樣 Vanilla Textures Expanded 的 transpiler 就無法把文字加回來或把圖示移到左邊。
- Harmony 補丁（完整列表在 `docs/API.md` 的〈Harmony patches〉）：
  - `MainButtonsRoot.DoButtons`：prefix 用 `Priority.Last`，只在本模組畫列時回傳 false；另有 postfix 只觀察。
  - `MainButtonsRoot.HandleLowPriorityShortcuts`：postfix，處理本模組自己存的快捷鍵。
  - `CameraDriver.CameraDriverOnGUI`：滾輪捲動列時不讓地圖縮放。
  - `Widgets.ButtonTextSubtle`：文字對齊、主題文字色、自畫圖示。
  - `Widgets.DrawAtlas(Rect, Texture2D, bool)`：先畫按鈕圖，再換成主題 atlas。
  - `PlayDataLoader.LoadAllPlayData`：切換語言後重抓 baseline、清掉快取。
- 捲動時被遮住的部分不能點（滑鼠在可視範圍外視為 Unreachable）。

**五個積木接口（slots，全部是 Def）**，附屬模組的寫法在 `docs/API.md`（繁中節錄 `docs/API.zh-TW.md`）：
1. 顯示方式 `BarDisplayModeDef`（worker，需要 DLL）。
2. 外觀主題 `BarAppearanceDef`（只要 XML＋圖片，worker 選用）。
3. 相容橋接 `CompatBridgeDef`：TooManyButtons、Custom Bottom Bar、UI Not Included、DragReorder。玩家選誰畫列；選「本模組優先」時移除對方在列上的補丁，**這次啟動內固定，重開才生效**。
4. 按鈕預設值：`MainButtonBarDefaults`（DefModExtension），另有執行期 API。
5. 查詢 API `MainButtonBarAPI`：哪些按鈕在列上、哪些在抽屜、列的位置。

另外還有：
- `CompatNoticeDef`（2026-11-05）：只提示、不切換優先也不拿掉補丁的相容定義，全部寫在 XML（packageId、用反射讀的開關、「開關狀態 × 誰畫列 → 提示文字」）。內建 [LFS] UI Rethemed - Main Bar，在 `Defs/MainButtonBarSystem/CompatNotices.xml`。
- `MainButtonArrangementDef`：排序積木，訂閱後出現在匯入選單。
- `MainButtonIconSetDef`：圖示包。

**自訂主題（2026-11-05 起，第 12 輪）**
- 外觀分頁上方切換「預設主題／自訂主題」，兩種模式都有和按鈕分頁一樣的底部按鈕。
- 資料：`BarConfig.appearanceMode`、`customTheme`（`Config/CustomTheme.cs`：`CustomThemeData` 有「所有按鈕」一層和每顆按鈕一層的 `ButtonLook`）。每一項：這顆 → 所有按鈕 → 預設主題選的那款 → 遊戲。
- 繪製：`BarAppearanceWorker_Custom` 疊在選的主題上；worker 新增 `ChangesButtons`、`LabelAlignFor`、`LabelFontFor`、`IconLookFor`，由 `ButtonPainter` 套到每顆按鈕。圖示擺放在 `Core/IconPlacement.cs`（有測試）。
- 介面：`UI/Dialog_MainButtonBar.Appearance.cs`（設定視窗改成 partial）。

**主題系統（API 1.1）**
- 按鈕外觀是 64×64 九宮格 atlas，四角是貼圖寬的 1/4，最多 18px，所以超過 72×72 沒意義；中段會拉伸。
- 1.1 新增：
  - `barBackgroundFit`／`drawerBackgroundFit`：Atlas／Tile／Stretch／Fit／Cover。
  - `decorations`：例如長出列外的立繪，可選在按鈕下層或上層、只在滑鼠移上時顯示等。
  - `labelActiveColor`。
  - **按鈕裡的圖** `buttonImage`／`buttonImages`：依 defName 指定按鈕；預設 Cover，可設錨點、inset、opacity；繪製順序是圖→框→文字。
- 擺放算法放在 `Core/DecorationLayout.cs`、`Core/ImageFit.cs`，都有測試。
- 開發者模式：`Diagnostics/ThemeValidator.cs` 啟動時檢查主題；外觀分頁有「重新載入主題貼圖」按鈕。
- 玩家可在外觀分頁調插圖：顯示與否、透明度、大小、位移、背景透明度。
- 內建主題（`Defs/MainButtonBarSystem/BarAppearances.xml`）：
  - 原版。
  - 帝國、信條、基因實驗室、虛空、星航：配色依各 DLC 預覽圖實測，見 `docs/research/dlc-visuals-2026-11-04.md`；沒有用到任何 DLC 美術。
  - 萘伊 `MBBS_Nai`：像素主題，原版風格的中空框，每顆主按鈕一張萘伊像素圖（有圖示的按鈕是萘伊雙手拿著對應道具，沒圖示的是從黑板後面探頭）；77 顆按鈕的對照寫在 Def 的 `buttonImages`，其他按鈕用只有黑板的 `buttonImage`。圖由作者在 repo 外產生，來源 PNG 在 `tools/themes/Nai/Buttons/`。
- 工具：
  - `tools/make_theme.py`：從 `tools/themes/*.json` 產生貼圖或整個主題模組，palette 可設 `hollow`（中空框）或 `frame: vanilla`（原版風格的中空框）。
  - `tools/gen_theme_textures.py`：重產內建主題的貼圖。
  - 範本：`docs/examples/ExampleTheme`。

**分享（平台下層）**
- 匯出可以取名、填作者和說明，有四種方式：存成檔案、複製文字（給 Discord）、複製成模組 XML、**一鍵做成排序模組**（寫進遊戲的 Mods 資料夾，含 About.xml 和用到的 PNG 圖示）。
- 匯出內容在 `config` 旁多一段 `meta`：名稱、作者、版本，以及匯出那台遊戲的每顆按鈕和它來自的模組。
- 匯入前先跳出報告（哪些按鈕對得上、缺哪些模組、缺主題或圖示），可以只勾部分匯入：順序與位置／大小與對齊／名稱／圖示／主題。對方遊戲沒有的按鈕保留玩家自己的設定。
- 程式在 `Config/ArrangementIO.cs`、`ArrangementMeta.cs`、`ArrangementMerge.cs`、`UI/Dialog_Sharing.cs`、`Core/Sharing.cs`。
- 剪貼簿的文字裡只要有 `Class=` 屬性就拒絕匯入（安全考量）。

**上層：在其他模組裡附帶內容**（例如種族模組自帶主題）：用 `LoadFolders.xml` 的 `IfModActive`，或在 Def 上加 `MayRequire`。範例在 `examples/MBBS_Example_SoftRace`（軟相依）和 `examples/MBBS_Example_ThemePack`（硬相依）。

**其他**
- 快捷鍵（`Hotkeys/Hotkeys.cs`）：
  - 有 KeyBindingDef 的按鈕用遊戲的 KeyPrefs。
  - 沒有的存在本模組設定，隱藏或收進抽屜的按鈕快捷鍵照樣有效。
  - 撞鍵判斷照原版 `ConflictingBindings` 規則。
- 改名直接改 `MainButtonDef.label`，並清掉 label 快取。
- `Diagnostics/StartupDiagnostics.cs`：開發模式輸出 `Diagnostics/buttons.tsv`（每顆按鈕的 iconPath／iconSourceMod）。

## 6. 實機確認過的原版行為（不用再查）

- `DoButtons` 的列：y = 螢幕高 − 35，按鈕高 36。
- `DoButton`：def 有 Icon 時才畫 32×32 圖示（滑鼠移上偏移 2px）；沒有時用 `ButtonTextSubtle` 畫文字；**停用時只畫 atlas、不畫文字**。
- 停用條件：沒有地圖，或路線規劃中正在組商隊等等。測試時用「組成商隊 → 選擇路線」觸發。
- `DrawAtlas` 九宮格：角 = min(貼圖寬/4, 高/2, 寬/2)。
- 切換語言不重開遊戲：`ClearAllPlayData` 後 `LoadAllPlayData`，所有 Def 換成新物件，`[StaticConstructorOnStartup]` 不會再跑。
- 類別裡有靜態 Texture2D 欄位卻沒加 `[StaticConstructorOnStartup]` 時，開發模式會出黃字。
- `ImpliedKeyBindingDefs` 只替 `defaultHotKey` 不是 None 的按鈕產生。
- Vanilla Textures Expanded 的 DoButton transpiler 在按鈕夠寬時，會把文字加回來、把圖示移到左邊。

## 7. 還沒做完 / 待驗證

- **實機還沒測**（照清單測）：
  - `docs/testing/round-8.md`：只顯示圖示＋VTE、Void 滑鼠移上、停用狀態、文字靠左、Custom Bottom Bar 本模組優先、checklist M 節。
  - `docs/testing/round-9.md`：分享、跨模組清單匯入、做成排序模組、範例模組（特別是**沒裝本模組時 `MayRequire` 的寫法會不會出紅字**）。
  - `docs/testing/theme-addons.md`：A～H 節，H 是按鈕圖。
- 匯入 TooManyButtons／Custom Bottom Bar 的設定：作者說之後交給本地處理；工作坊說明標為「實驗性」。
- 工作坊還沒上架：
  - 說明文字在 `docs/workshop/description.txt`（BBCode，英文＋簡中＋繁中），發佈清單在 `docs/workshop/release-checklist.md`。
  - `tools/make_release.py` 會產生乾淨的上傳資料夾。
  - `About/Preview.png` 已換成實機截圖。
- 作者保留決定的事項：見兩份 spec 的〈保留給作者決定的〉。

## 8. 可以接著做的候選（作者沒指定時先問他，或挑風險低的）

- 設定視窗的按鈕清單加搜尋或篩選：74 顆按鈕很難找。
- 多份具名範本，可以快速切換（例如「戰鬥用」「建設用」兩套排法）。
- 替 `docs/research/ecosystem-2026-11-04.md` 列出的其他主按鈕相關模組做相容橋接或預設值。
- 效能：檢查 `BarRenderer` 和 `ButtonPainter` 每幀的配置（GC alloc），對 74 顆按鈕的情境做最佳化。
- 更多內建主題，或替萘伊主題做佔位按鈕圖。
- 把 `ArrangementMerge` 的合併規則抽成不依賴遊戲的程式碼放進 Core，再加單元測試（目前只有 `ButtonMatch` 有測）。
- 工作坊說明補其他語言版本。

## 9. 常用指令

```bash
dotnet build Source/MainButtonBarSystem/MainButtonBarSystem.csproj -c Release
dotnet test Source/MainButtonBarSystem.Tests
python3 tools/gen_translations.py --check      # 只檢查
python3 tools/gen_translations.py              # 產生 Languages/
python3 -I -X utf8 tools/gen_theme_textures.py # 重產內建主題貼圖（PNG 只比像素，位元組不同的不要提交）
python3 -I -X utf8 tools/make_theme.py tools/themes/ExampleTheme.json docs/examples/ExampleTheme
python3 tools/make_release.py <repo 外的資料夾>
```
