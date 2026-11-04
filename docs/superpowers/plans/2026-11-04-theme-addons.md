# 主題附屬模組與萘伊主題 實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓純 XML 主題能放插圖（背景繪製方式、裝飾圖、各狀態文字色），附上範本模組、產生工具、開發模式檢查與重載，以及內建的萘伊主題（佔位圖）。

**Architecture:** `BarAppearanceDef` 加選用欄位，`BarAppearanceWorker` 加三個 virtual 畫背景圖和上層裝飾；擺放算法放在不依賴遊戲的 `Core/DecorationLayout.cs`。玩家可調值放 `BarConfig`，預設和範圍放 `Tuning.xml`。Python 端把畫圖程式碼搬進 `tools/make_theme.py`，`gen_theme_textures.py` 變成呼叫它的薄殼。

**Tech Stack:** C#（.NET 10，RimWorld 1.7 Waylith）、Harmony 3、xunit；Python 3.12＋Pillow 13（`RimClaude\tools\.venv_vector`）。

**Spec:** `docs/superpowers/specs/2026-11-04-theme-addons-design.md`

## Global Constraints

- 舊主題不加新欄位時行為完全不變；內建 5 款重產後逐檔一致（位元組；位元組不同時像素一致並記錄原因）。
- 程式裡不寫死可調數值：新數值進 `Tuning.xml`（`barDefaults`／`ranges`／`behaviour`）。
- 每個玩家可調值有「預設值」按鈕（既有 `SectionHeader`／`IntSetting` 樣式）。
- 開發模式的檢查只用 `Log.WarningOnce`，正式遊玩不出字。
- `ApiVersion` 1.0 → 1.1。
- Python 腳本頂層文件照 `CODING.md §4`；`python -I -X utf8` 執行。
- 29 種語言：新 Keyed 鍵和萘伊主題的 DefInjected 都補齊（`tools/i18n/*.json` → `gen_translations.py`）。

## Review Focus

1. 裝飾圖貼圖找不到（`Tex` 回 null）：跳過那張，不丟例外，不每幀洗 log → Task 4 測 `Tex` null 路徑；Task 7 的檢查報一次。
2. `size` 一邊為 0、貼圖大小為 0（還沒載入）：不除以零、不畫 → Task 3 測試 `Place_ZeroSizeUsesTexture`、`Place_OneSideZeroKeepsAspect`。
3. 玩家縮放 25%～300%、位移 ±400 與主題的 `offset` 疊加後仍以錨點為基準 → Task 3 測試 `Place_PlayerScaleKeepsAnchor`。
4. 抽屜視窗裡 `Mouse.IsOver` 用的是視窗區域座標 → Task 5 用 `windowRect`（已是視窗座標）判斷，測試清單 Task 11 實機驗。
5. 舊存檔沒有新欄位：`ExposeData` 預設值讓它們等於「未設定」 → Task 6 測試清單含「讀舊存檔外觀不變」。

---

### Task 1: `make_theme.py` 與內建 5 款的 JSON（貼圖逐檔一致）

**Files:**
- Create: `tools/make_theme.py`、`tools/themes/Imperial.json`、`Creed.json`、`GeneLab.json`、`Void.json`、`Starfarer.json`
- Modify: `tools/gen_theme_textures.py`（改成薄殼）

**Interfaces:**
- Produces: `make_theme.theme_load(path) -> dict`、`theme_build_textures(theme) -> dict[str, Image]`（檔名 → 影像，含 `Button`…`Gear`）、`theme_write_textures(theme, folder)`、`theme_write_mod(theme, out_dir)`、`icon_build_mod() -> Image`；CLI：`make_theme.py <theme.json> <out_dir> [--textures-only]`。
- JSON 結構（`tools/themes/README` 不另寫，寫在腳本 docstring）：
  ```json
  {"name": "Imperial", "defName": "MBBS_Imperial", "label": "imperial", "description": "...", "author": "NAI", "displayOrder": 10,
   "labelColor": [0.85,0.85,0.75,1], "labelMouseoverColor": [...], "labelActiveColor": null,
   "palette": {"base": [36,42,48], "edge": [...], "outline": null, "rule": [...], "accent": [...], "active": [...],
               "ornament": "finial", "double": true, "icon": null, "tile": null, "rivet": null, "active_rule": null, "glow_on_hover": false},
   "background": {"barFit": "Atlas", "drawerFit": "Atlas", "barTileScale": 1, "drawerTileScale": 1,
                  "bar": null, "drawer": null},
   "decorations": [],
   "mod": {"packageId": "...", "name": "...", "texturesRoot": "ExampleTheme", "supportedVersions": ["1.7"]},
   "translations": {"ChineseTraditional": {"label": "...", "description": "..."}}}
  ```
  `background.bar`／`drawer` 為 null 時用內建畫法（和現在一樣）；給 `{"placeholder": {"size": [w,h], "color": [r,g,b], "text": "..."}}` 時畫佔位圖。`decorations[]` 每項：`file`、`placeholder`（同上）或 `source`（複製既有 PNG）、`target`、`anchor`、`verticalAnchor`、`offset`、`size`、`opacity`、`layer`、`showWhen`。

- [ ] **Step 1: 用舊腳本做基準輸出**
  Run: `.venv_vector python -I -X utf8 tools/gen_theme_textures.py`（先把 `OUT` 用環境變數或複製一份改到 scratchpad）→ `scratch/baseline/<Theme>/*.png`、`ModIcon.png`。
  比對基準和 repo 已提交的 PNG：`cmp`；位元組不同的用 Pillow 比像素，結果記在 Task 12 的測試清單「貼圖重產」一節。
- [ ] **Step 2: 寫 `tools/make_theme.py`**：畫圖函式（`shade`…`tinted_gear`、`mod_icon`）原樣搬入，改名照 `CODING.md §5.4`（`_theme_*` 檔內、`theme_*` 對外），常數 `N`、`Q` 保留。加 `_theme_draw_placeholder(size, color, text)`、`_theme_write_about`、`_theme_write_defs`、`_theme_write_languages`、`_theme_write_preview`（About 的 `Preview.png`：640×360，用九宮格把 `Button`×3＋`More` 畫在 `Bar` 上，`_theme_draw_nine_slice(img, atlas, rect)`）。
- [ ] **Step 3: 寫 5 份 JSON**（`palette` 的值從舊 `THEMES` dict 逐字搬）。
- [ ] **Step 4: `gen_theme_textures.py` 改成：讀 `tools/themes/{Imperial,Creed,GeneLab,Void,Starfarer,Nai}.json`，各呼叫 `theme_write_textures(theme, Textures/MainButtonBarSystem/Themes/<name>)`，再寫 `About/ModIcon.png`。**
- [ ] **Step 5: 重產並比對**
  Run: `.venv_vector python -I -X utf8 tools/gen_theme_textures.py`，再 `cmp` 每個檔和 Step 1 的基準。Expected：50 個 PNG＋ModIcon 全部相同。
- [ ] 功能驗收：`git status` 只有新檔和 `gen_theme_textures.py` 的改動，`Textures/` 沒有變動（或只有像素相同的位元組差異，已記錄）。
- [ ] Code review：執行 `superpowers:requesting-code-review`
- [ ] Commit

### Task 2: `ExampleTheme` 與萘伊主題的素材

**Files:**
- Create: `tools/themes/ExampleTheme.json`、`tools/themes/Nai.json`、`docs/examples/ExampleTheme/**`（工具輸出）、`Textures/MainButtonBarSystem/Themes/Nai/*.png`
- Modify: `Defs/MainButtonBarSystem/BarAppearances.xml`（加 `MBBS_Nai`）、`tools/i18n/*.json`（`MBBS_Nai.label`／`.description`）

**Interfaces:**
- Consumes: Task 1 的 CLI。
- Produces: `MBBS_Nai` def（欄位見規格 §4.8）；`docs/examples/ExampleTheme/Defs/ExampleTheme_Themes.xml` 的 def `ExampleTheme_Example` 用到**全部**欄位（含 `barBackgroundFit=Stretch`、兩張裝飾、`labelActiveColor`、`preview`）。

- [ ] **Step 1: 寫 `Nai.json`**（palette：base `(45,53,79)`、edge `(218,212,216)`、rule `(91,88,93)`、accent `(138,114,170)`、active `(58,66,96)`、ornament `"petal"`（新樣式：角落一顆小圓點＋短斜線，只在 `_theme_draw_ornament` 加分支，不影響舊樣式）；bar 佔位 1024×36 `Stretch`；decorations：`Portrait` 200×280 Left/Bottom BelowButtons；`Mark` 48×48 Right/Bottom offset (−8,−40) AboveButtons showWhen [Mouseover]；`DrawerBadge` 64×64 target Drawer Right/Top AboveButtons）。
- [ ] **Step 2: 寫 `ExampleTheme.json`**（palette 中性灰藍；`mod.packageId` `yourname.mbbstheme.example`、`name` `Example Theme for Main Button Bar System`；translations 至少 ChineseTraditional、ChineseSimplified、Japanese）。
- [ ] **Step 3: 產生**：`make_theme.py tools/themes/ExampleTheme.json docs/examples/ExampleTheme`；`gen_theme_textures.py`（含 Nai）。
- [ ] **Step 4: `BarAppearances.xml` 加 `MBBS_Nai`**，`tools/i18n/English.json` 加 DefInjected 兩鍵，其餘 28 種語言補翻譯，跑 `gen_translations.py`。
- [ ] 功能驗收：`gen_translations.py --check` 無 ERROR、無 missing；`docs/examples/ExampleTheme` 有 About/Defs/Textures/Languages/Preview。
- [ ] Code review：執行 `superpowers:requesting-code-review`
- [ ] Commit

### Task 3: `Core/DecorationLayout.cs`（純數學）＋測試

**Files:**
- Create: `Source/MainButtonBarSystem/Core/DecorationLayout.cs`、`Source/MainButtonBarSystem.Tests/DecorationLayoutTests.cs`

**Interfaces:**
- Produces（namespace `MainButtonBarSystem.Core`）：
  ```csharp
  public enum DecorationAnchor { Left, Center, Right }
  public enum DecorationVerticalAnchor { Bottom, Center, Top }
  public readonly struct DecorationBox { public readonly float X, Y, Width, Height; }
  public static class DecorationLayout
  {
      /// targetX/Y/Width/Height：目標區域；sizeX/Y：XML size（0 = 貼圖）；texW/texH：貼圖像素；
      /// offsetX/Y：XML offset；playerScale：1 = 100%；playerOffsetX/Y：玩家位移。回傳 Width<=0 代表不畫。
      public static DecorationBox Place(float targetX, float targetY, float targetWidth, float targetHeight,
          DecorationAnchor anchor, DecorationVerticalAnchor verticalAnchor, float sizeX, float sizeY, int texW, int texH,
          float offsetX, float offsetY, float playerScale, float playerOffsetX, float playerOffsetY);
  }
  ```

- [ ] **Step 1: 測試**（xunit）：`Place_RightBottomTouchesCorner`（target (0,964,1920,36)，size 200×280 → X=1720, Y=720）、`Place_ZeroSizeUsesTexture`、`Place_OneSideZeroKeepsAspect`（size (100,0)，tex 200×400 → 100×200）、`Place_ZeroTextureReturnsEmpty`（tex 0×0、size 0 → Width 0）、`Place_PlayerScaleKeepsAnchor`（scale 2，Right/Bottom → 右下角不動）、`Place_OffsetsAdd`（offset (−8,−40)＋player (10,5) → X 差 2、Y 差 −35）。
- [ ] **Step 2: 實作 `Place`**。
- [ ] **Step 3: `dotnet test Source/MainButtonBarSystem.Tests`**（需要 SDK；沒有就記錄「未執行」）。
- [ ] 功能驗收：測試全綠（或記錄未執行原因）。
- [ ] Code review：執行 `superpowers:requesting-code-review`
- [ ] Commit

### Task 4: `BarAppearanceDef` 新欄位、繪製方式、裝飾圖、`labelActiveColor`、重載

**Files:**
- Modify: `Source/MainButtonBarSystem/API/BarAppearanceDef.cs`
- Create: `Source/MainButtonBarSystem/API/BarDecoration.cs`（`BarDecoration`、`BackgroundFit`、`DecorationTarget`、`DecorationLayer`、`DecorationState` 列舉）、`Source/MainButtonBarSystem/Rendering/TexturePainter.cs`（`DrawFitted(Rect, Texture2D, BackgroundFit, float tileScale)` 的 Atlas/Tile/Stretch/Fit/Cover 實作）

**Interfaces:**
- Produces（都在 `MainButtonBarSystem`）：
  - `BarAppearanceDef` 欄位：`BackgroundFit barBackgroundFit = Atlas; float barBackgroundTileScale = 1f; BackgroundFit drawerBackgroundFit = Atlas; float drawerBackgroundTileScale = 1f; List<BarDecoration> decorations; Color labelActiveColor;`
  - `public bool HasDecorations`；`public void ReloadTextures()`（規格 §4.9）；`public int ReloadedCount` 不要，改為回傳 `int`（重讀成功的張數）。
  - `ConfigErrors()` 加：裝飾的 `texture` 空字串、`opacity` 不在 0～1。
  - `BarAppearanceWorker` 新 virtual：`DrawBarBackgroundImage(Rect)`、`DrawBarOverlay(Rect)`、`DrawDrawerBackgroundImage(Rect)`、`DrawDrawerOverlay(Rect)`、`DrawDecorations(DecorationTarget target, DecorationLayer layer, Rect area)`；`DrawBarBackground`＝`DrawBarBackgroundImage`＋`DrawDecorations(Bar, BelowButtons)`；`DrawDrawerBackground` 同理（沒有背景圖時仍 `Widgets.DrawWindowBackground`）。
  - `LabelColor(state)`：Active → `labelActiveColor`（a>0）→ `labelMouseoverColor` → `labelColor`。`ChangesButtons` 含 `labelActiveColor.a > 0`。
  - 狀態判定：`static bool DecorationVisible(BarDecoration d, DecorationTarget target, Rect area)`（規格 §3.2）。
  - 玩家值：`BarConfigManager.Active` 的 `DecorationsShown`、`DecorationOpacity`（0～1）、`DecorationScale`（1 = 100%）、`DecorationOffset`（Vector2）、`BackgroundOpacity`（Task 6 提供）。
- [ ] **Step 1: 建 `BarDecoration.cs`**（欄位和預設值照規格 §3.2，`showWhen` 是 `List<DecorationState>`）。
- [ ] **Step 2: `TexturePainter.DrawFitted`**：Atlas→`Widgets.DrawAtlas`；Stretch→`GUI.DrawTexture(rect, tex)`；Fit→算置中矩形後 `GUI.DrawTexture`；Cover→算 UV 後 `GUI.DrawTextureWithTexCoords`；Tile→`GUI.BeginGroup(rect)` 內雙迴圈 `GUI.DrawTexture`，格子大小 `tex.width*tileScale`。透明度用 `GUI.color` 的 alpha 乘上 `BackgroundOpacity`，畫完還原。
- [ ] **Step 3: `BarAppearanceDef`／`BarAppearanceWorker` 改動**（上面的介面）。
- [ ] **Step 4: `ReloadTextures()`**：對 `textures` 的每個 key 和所有欄位路徑，在 `modContentPack.foldersToLoadDescendingOrder` 的每個資料夾找 `Textures/<path>.png`；讀到就 `new Texture2D(2,2)`＋`LoadImage`、`filterMode=Bilinear`、`wrapMode=Clamp`，放進 `textures` 並記在 `ownTextures`；上一輪的 `ownTextures` 先 `UnityEngine.Object.Destroy`；找不到的 `textures.Remove(path)`（下次 `Tex` 再走 ContentFinder）。
- [ ] 功能驗收：編譯 0 警告（`TreatWarningsAsErrors`）。
- [ ] Code review：執行 `superpowers:requesting-code-review`
- [ ] Commit

### Task 5: 接上列和抽屜

**Files:**
- Modify: `Source/MainButtonBarSystem/Rendering/BarRenderer.cs:112-122`（`mode.Worker.DrawBar(ctx)` 之後呼叫 `ctx.Appearance.DrawBarOverlay(ctx.ReportedBarRect)`，在同一個 try 裡）、`Source/MainButtonBarSystem/Rendering/Window_ButtonDrawer.cs:150-160`（齒輪之後 `appearance.DrawDrawerOverlay(inRect.ExpandedBy(B.drawerPadding))`，再 `GUI.color = Color.white`）、`Source/MainButtonBarSystem/UI/Dialog_MainButtonBar.cs:317-350`（預覽改呼叫 `DrawBarBackgroundImage`）、`Source/MainButtonBarSystem/API/MainButtonBarAPI.cs:21`（`"1.1"`）

- [ ] **Step 1: 三處改動。**
- [ ] 功能驗收：編譯通過；預覽對 5 款舊主題外觀不變（Atlas 路徑相同）。
- [ ] Code review：執行 `superpowers:requesting-code-review`
- [ ] Commit

### Task 6: 玩家可調值（`BarConfig`、`Tuning`、設定視窗）＋開發模式重載按鈕

**Files:**
- Modify: `Source/MainButtonBarSystem/Config/BarConfig.cs`、`Source/MainButtonBarSystem/API/MainButtonBarTuningDef.cs`、`Defs/MainButtonBarSystem/Tuning.xml`、`Source/MainButtonBarSystem/UI/Dialog_MainButtonBar.cs:250-272`、`tools/i18n/*.json`

**Interfaces:**
- `BarConfig`：`public const int UnsetOffset = int.MinValue; DefaultableBool decorationsShown; int decorationOpacity = Unset; int decorationScale = Unset; int decorationOffsetX = UnsetOffset; int decorationOffsetY = UnsetOffset; int backgroundOpacity = Unset;` 屬性 `DecorationsShown`（bool）、`DecorationOpacity`（float 0～1）、`DecorationScale`（float，1=100%）、`DecorationOffset`（Vector2）、`BackgroundOpacity`（float 0～1）；整數版 `DecorationOpacityPercent` 等給滑桿。`Clone`／`ResetToUntouched`／`ExposeData` 補齊。
- `Tuning.xml`：`barDefaults` 加 `decorationsShown=true`、`decorationOpacity=100`、`decorationScale=100`、`decorationOffsetX=0`、`decorationOffsetY=0`、`backgroundOpacity=100`；`ranges` 加 `decorationOpacity 0~100`、`decorationScale 25~300`、`decorationOffset -400~400`、`backgroundOpacity 0~100`、`percentStep 5`；`behaviour` 加 `themeAtlasMaxSize=72`、`themeDecorationMaxSize=2048`、`themeTileMaxCount=4096`；`settingsWindow` 加 `themeSettingsGap`（主題卡片和下方那一節的間距）。
- 設定視窗：外觀分頁改成一欄捲動的 listing（主題標題列、卡片、再接「插圖與背景」節）；只有選中的主題 `HasDecorations` 或有背景圖時顯示那一節的滑桿，否則顯示一行灰字說明。開發模式多一顆「重新載入主題貼圖」按鈕（`Prefs.DevMode`），按下：對 `BarModel.Appearances` 逐個 `ReloadTextures()` 加總，`ThemeValidator.ValidateAll()`，`Messages.Message("MBBS_ThemesReloaded".Translate(n), MessageTypeDefOf.NeutralEvent, false)`。
- 新 Keyed：`MBBS_Section_Illustration`、`MBBS_DecorationsShown`、`MBBS_DecorationsShownTip`、`MBBS_DecorationOpacity`（{0}%）、`MBBS_DecorationScale`、`MBBS_DecorationOffsetX`、`MBBS_DecorationOffsetY`、`MBBS_BackgroundOpacity`、`MBBS_IllustrationNone`、`MBBS_ReloadThemes`、`MBBS_ReloadThemesTip`、`MBBS_ThemesReloaded`（{0}）。

- [ ] **Step 1: `BarConfig`＋`Tuning`（def 與 XML，`ConfigErrors` 檢查 `themeAtlasMaxSize > 0`）。**
- [ ] **Step 2: 設定視窗**：新增 `OffsetSetting(listing, label, stored, resolved, range)`（哨兵 `UnsetOffset`）；其餘用 `IntSetting`／`BoolSetting`。
- [ ] **Step 3: 29 種語言的 Keyed，跑 `gen_translations.py`。**
- [ ] 功能驗收：編譯；`gen_translations.py --check` 乾淨。
- [ ] Code review：執行 `superpowers:requesting-code-review`
- [ ] Commit

### Task 7: 開發模式主題檢查

**Files:**
- Create: `Source/MainButtonBarSystem/Diagnostics/ThemeValidator.cs`
- Modify: `Source/MainButtonBarSystem/Diagnostics/StartupDiagnostics.cs:29-41`（加 `Section(report, "Themes", ThemeValidator.Report)`）

**Interfaces:**
- `public static class ThemeValidator { public static int ValidateAll(); public static void Report(StringBuilder sb); }`：每個問題 `Log.WarningOnce($"{LogPrefix}Theme {defName} ({mod}): {field} ...", (defName+field+kind).GetHashCode())`；`Report` 在報告裡寫「themes N, warnings M」。檢查項照規格 §3.6；atlas 欄位＝`button`、`buttonMouseover`、`buttonActive`、`buttonDisabled`、`moreButton`，加上 fit 為 Atlas 的 `barBackground`／`drawerBackground`。

- [ ] **Step 1: 寫 `ThemeValidator`。**
- [ ] **Step 2: 接進 `StartupDiagnostics.RunAtStartup`。**
- [ ] 功能驗收：編譯；對內建 6 款預期 0 警告（Nai 的裝飾圖不是 atlas，不檢查正方形）。
- [ ] Code review：執行 `superpowers:requesting-code-review`
- [ ] Commit

### Task 8: 文件

**Files:**
- Modify: `docs/API.md`（第 4 節＋`API stability` 的版本號）、`README.md`（工具表加 `make_theme.py`、`tools/themes/`）、`docs/workshop/description.txt`（主題那條加「背景圖與裝飾圖」一句）
- Create: `docs/API.zh-TW.md`、`docs/workshop/theme-template-release.md`

- [ ] **Step 1: `API.md` §4**：新欄位表（fit 五種、`decorations` 欄位表、`labelActiveColor`）、狀態定義、預覽不畫裝飾、抽屜裁切、玩家可調值會疊加、開發模式檢查與重載、「Workshop：Required Items 加 Main Button Bar System」、版本相容承諾（欄位只加不刪；刪除先 `[Obsolete]` 一版；`ApiVersion` 1.1）。
- [ ] **Step 2: `API.zh-TW.md`**：§4 全文繁中＋「軟性依賴」「API 穩定性」兩段。
- [ ] **Step 3: 評估文件**：兩個方案（放進發佈檔 `ThemeTemplate/`／獨立工作坊項目）的優缺點、建議、怎麼切換。
- [ ] 功能驗收：文件裡的欄位名和 C# 一致（grep 對照）。
- [ ] Code review：執行 `superpowers:requesting-code-review`
- [ ] Commit

### Task 9: `make_release.py`

**Files:**
- Modify: `tools/make_release.py`

- [ ] **Step 1:** 複製 `docs/examples/ExampleTheme` → `<target>/ThemeTemplate/ExampleTheme`，`tools/make_theme.py`＋`tools/themes/ExampleTheme.json` → `<target>/ThemeTemplate/tools/`，`docs/API.zh-TW.md` → `API.zh-TW.md`；加 `--no-template` 旗標。頂層 docstring 更新。
- [ ] 功能驗收：對 scratchpad 跑一次，列出檔案數；確認 `ThemeTemplate/` 下沒有會被遊戲讀到的頂層資料夾名（`Defs`、`Textures`、`Languages` 都在第二層以下）。
- [ ] Code review：執行 `superpowers:requesting-code-review`
- [ ] Commit

### Task 10: 編譯與單元測試

- [ ] **Step 1:** `dotnet build Source/MainButtonBarSystem/MainButtonBarSystem.csproj -c Release`（沒有 SDK 就先問作者怎麼安排）。
- [ ] **Step 2:** `dotnet test Source/MainButtonBarSystem.Tests`。
- [ ] **Step 3:** `1.7/Assemblies/MainButtonBarSystem.dll` 更新後，用 `RimClaude\tools` 的 `verify_dll.py` 對遊戲本體檢查 0 問題。
- [ ] 功能驗收：0 警告、測試全綠、verify_dll 0 問題。
- [ ] Commit（DLL 和原始碼一起）

### Task 11: 測試清單

**Files:**
- Create: `docs/testing/theme-addons.md`

- [ ] **Step 1:** 情境：把 `docs/examples/ExampleTheme` 複製到 `Mods/ExampleTheme`（不改本模組）；用 `ModsConfig_20261104_1231_mbbs_selftest_cleanlog.xml` 那種小排序加上它。步驟：載入無紅字、外觀分頁出現「Example」卡片（作者行顯示模組名）、選它後背景 Stretch、左下立繪長出列、滑鼠移上右端標記出現、抽屜右上標記、文字三種狀態顏色、五個滑桿和預設值按鈕、「全部重設」、開發模式警告（故意把一張 PNG 改名後重開）、重載按鈕（改 PNG 顏色後按）、舊主題外觀不變、舊存檔讀取、萘伊主題同樣一輪、`make_theme.py` 從 JSON 重產 ExampleTheme 後 `git status` 乾淨。
- [ ] Commit

### Task 12: 整支分支 code review 與收尾

- [ ] **Step 1:** `superpowers:requesting-code-review` 對整個分支（含 Python）。
- [ ] **Step 2:** 修正，重跑 Task 1 Step 5 的比對和 `gen_translations.py --check`。
- [ ] Commit

---

## 執行紀錄（2026-11-04）

- Task 1～9、11 完成 `2026-11-04`；Task 10 編譯：這台機器原本沒有 .NET SDK，作者同意後用微軟官方 `dotnet-install.ps1` 把 SDK 10.0.403 裝到 `C:\Users\IL\.dotnet`（不改 PATH，`C:\Users\IL\.dotnet\dotnet.exe build …`），Release 0 警告 0 錯誤，xunit 46 個測試通過，`verify_dll.py` 0 問題 `2026-11-04`。
- 和計畫不同的地方：`settingsWindow.themeSettingsHeight` 改成 `themeSettingsGap`（外觀分頁改成一欄捲動的 listing，不用固定高度）；`behaviour` 多一個 `themeTileMaxCount`（Tile 的格數上限，超過就改成 Stretch）；重載的舊貼圖不 `Destroy`（避免同一幀還在用它的繪圖拿到已銷毀的物件），只是不釋放。
- 各任務的 code review 合併成兩次：Python＋C#＋文件一次整支分支 review（Sonnet 子代理），修正後再補一次自查。
- 五款舊主題的 PNG：提交版是別的 Pillow 版本編的，像素相同、位元組不同；本機重產後和同機的舊腳本輸出 50 張＋模組圖示位元組全同，所以提交的 PNG 沒有覆蓋。
- 沒做：進遊戲驗證（留給作者照 `docs/testing/theme-addons.md` 做，或由 Claude 用 23 個模組的測試排序開遊戲）。
