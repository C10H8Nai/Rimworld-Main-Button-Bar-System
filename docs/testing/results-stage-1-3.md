# 第一到三階段實機測試結果

- 測試分支：`claude/stage-3`（commit `4800254`）
- 環境：RimWorld 1.7.4995 rev309（Waylith，.NET 10），啟用 2,201 個模組，本模組排在第 677 個（Harmony 之後）。
- TooManyButtons、Custom Bottom Bar：有訂閱，但**沒有啟用**，所以第三階段的相容項目（步驟 4）沒有測。

## 1. 靜態檢查

### verify_dll.py

`1.7/Assemblies/MainButtonBarSystem.dll`：**問題 0**（對 Assembly-CSharp 的方法、欄位引用全部找得到）。

另外補查了 verify_dll 不看的部分：

- 組件參考和遊戲實際的版本一致：

  | 參考 | DLL 要求 | 遊戲提供 |
  |---|---|---|
  | System.Runtime | 10.0.0.0 | 10.0.0.0 |
  | Assembly-CSharp | 1.7.9800.9270 | 1.7.9800.9270 |
  | UnityStubs | 1.0.0.0 | 1.0.0.0 |
  | System.Collections | 10.0.0.0 | 10.0.0.0 |
  | 0Harmony | 3.0.0.0 | 3.0.0.0（Harmony 模組 2009463077） |
  | System.Xml.ReaderWriter | 10.0.0.0 | 10.0.0.0 |
  | System.Linq | 10.0.0.0 | 10.0.0.0 |
  | System.Threading | 10.0.0.0 | 10.0.0.0 |
  | System.Text.Encoding.Extensions | 10.0.0.0 | 10.0.0.0 |
  | System.Text.RegularExpressions | 10.0.0.0 | 10.0.0.0 |

- 對 UnityStubs 的 43 個成員引用：問題 0。

### 其他

- `check_order.py`：沒有和本模組有關的問題（只有原本就有的 2 個缺前置、25 組同名 DLL，都是別的模組）。
- `xml_check.py`（`Defs/MainButtonBarSystem/*.xml`）：問題 0。

## 2. 原版行為確認（從遊戲的 Assembly-CSharp.dll 反組譯 IL）

### a. 改名後的標籤快取

- `MainButtonDef` 的建構子把 `cachedLabelCapWidth`、`cachedShortenedLabelCapWidth` 設成 **`-1f`**。
- `LabelCapWidth`：`cachedLabelCapWidth < 0` 時重算（IL 是 `bge.un 0`，所以 NaN 也會重算）。重算時暫時把字型改成 `GameFont.Small`，取 `Text.CalcSize(LabelCap).x`，存回欄位，再把字型改回原本的。
- `ShortenedLabelCapWidth`：條件一樣（`< 0`），量的是 `ShortenedLabelCap`，字型也是 Small。
- `ShortenedLabelCap`（string）：`cachedShortenedLabelCap == null` 時，用 `LabelCap.Shorten()` 重算。**設成空字串不會重算**，要設 null。
- `Def.LabelCap`：
  - `label` 是 null 或空字串時，直接回傳空的 TaggedString，不寫快取。
  - 否則在 `cachedLabelCap.NullOrEmpty()`（RawText 是 null 或空）時，用 `label.CapitalizeFirst()` 重算。
  - 所以 `default(TaggedString)` 和空字串都會觸發重算。
- **結論**：雲端的四個重設值（-1、-1、null、空值）都和原版的判斷條件一致。四個都要清：`ShortenedLabelCap` 從 `LabelCap` 算出來，兩個寬度又從這兩個算出來，只改 `label` 不會讓任何一個失效。

### b. 隱藏按鈕的快捷鍵

`MainButtonsRoot.MainButtonsOnGUI` 的流程：

1. `Event.current.type == Layout` 時直接 return。
2. 呼叫 `DoButtons()`。
3. 依序走 `allButtonsInOrder`，每顆只檢查三件事：
   - `!Worker.Disabled`
   - `hotKey != null`
   - `hotKey.KeyDownEvent`

   三個都成立就 `Event.current.Use()`，再呼叫 `Worker.InterfaceTryActivate()`，然後 return。一次只觸發一顆。

**不檢查 `Visible`，也不檢查 `buttonVisible`**。`InterfaceTryActivate` 只檢查教學模式和 TilePicker（選格子時不能逃離的狀態），也不看可見性。所以雲端的假設正確。前提：

- 按鈕要留在 `allButtonsInOrder` 裡。原版建構子用 `DefDatabase<MainButtonDef>.AllDefs.OrderBy(d => d.order).ToList()`，`buttonVisible=false` 的按鈕也包含在內。
- 快捷鍵迴圈和 `DoButtons()` 在同一個方法裡，排在它後面，所以 `DoButtons` 的 Prefix 回傳 false 不影響快捷鍵。
- TooManyButtons 會用 Traverse 換掉 `allButtonsInOrder`。如果它換上去的清單不含被隱藏的按鈕，那些按鈕的快捷鍵就會失效（推定，沒有驗證）。

`Worker.Disabled` 在以下情況為真：

- 沒有目前地圖時：`validWithoutMap=false` 的按鈕，或「世界」按鈕本身。
- 世界路線規劃正在組商隊時：同上的條件。
- TilePicker 啟用而且不能 Esc 時：同上的條件。

附帶：`KeyBindingDef.KeyDownEvent` 的條件：

- 只認 `KeyDown` 事件，而且 `keyCode` 不是 None。
- 按著 Command／Win 鍵、而綁定本身不是 Command 鍵（KeyCode 309／310）時，回傳 false。
- `Find.WindowStack.AnySearchWidgetFocused` 時，回傳 false。
- 不檢查一般文字框的焦點，因為文字框收到按鍵後會自己 `Use()` 掉事件。

雲端自訂快捷鍵用的是 `GUIUtility.keyboardControl != 0` 就不處理，比原版嚴格（任何控制項有鍵盤焦點都會擋）。

### c. 地圖滾輪縮放的位置

- 縮放在 **`CameraDriver.CameraDriverOnGUI`** 裡處理。`CameraDriver.Update` 沒有讀滾輪（沒有 `Input.mouseScrollDelta`，也沒有 `GetAxis`）。
- 條件只有 `Event.current.type == EventType.ScrollWheel`，計算是 `desiredSize -= delta.y × ScrollWheelZoomRate × zoomSpeed × RootSize / 35`。不檢查 `mouseCoveredByUI`，也不會 `Use()` 事件。所以原版在主按鈕列上滾滾輪，地圖會縮放。
- 沒有目前地圖時，`CameraDriverOnGUI` 一開始就 return。
- 呼叫順序：`Verse.Root.OnGUI` 先呼叫 `UIRoot.UIRootOnGUI()`，`UIRoot_Play` 會在裡面呼叫 `MainButtonsOnGUI`；之後才呼叫 `Find.CameraDriver.CameraDriverOnGUI()`。
- 所以畫主按鈕列時對滾輪事件 `Use()`，事件類型就變成 `Used`，`CameraDriverOnGUI` 不會縮放。在 `CameraDriverOnGUI` 加 Prefix 擋掉也可以。兩種做法擇一就夠，雲端目前兩種都做了（`BarDisplayModeDef` 裡 `Use()`，加上 Prefix）。

### d. 原版 DoButtons 的排版

1. 第一輪：`allButtonsInOrder` 中 `Worker.Visible` 為真的按鈕，累加 `minimized ? 0.5f : 1f`，得到 `total`（float）。
2. 一格寬 `int full = (int)(UI.screenWidth / total)`，小數直接捨去。半格寬 `int half = full / 2`，整數除法。
3. `last = allButtonsInOrder.FindLastIndex(b => b.Worker.Visible)`。
4. `GUI.color = Color.white`。
5. 第二輪：`int x = 0`。每顆可見的按鈕：
   - 寬度：`minimized ? half : full`。
   - 如果是第 `last` 顆，寬度改成 `UI.screenWidth - x`，補滿剩下的寬度。
   - `Rect(x, UI.screenHeight - 35, 寬度, 36)`，呼叫 `Worker.DoButton(rect)`，然後 `x += 寬度`。

注意：

- **高度是寫死的 `36`**，不是 `MainButtonDef.ButtonHeight`（const int `35`）。y 是 `screenHeight - 35`，所以按鈕底部會超出畫面 1px。
- 雲端的 `BarRenderer.DefaultBarRect` 和按鈕 rect 用的高度是 `ButtonHeight`（35）。按鈕的背景貼圖會在 35px 內縮放，底部的邊框落在畫面最後一行，原版則是落在畫面外。可能有 1px 的視覺差異，要和原版完全一樣就把高度改成 36。
- `MainButtonWorker.Visible`：Ideology 啟用、目前是經典信仰模式、而且 `validWithClassicIdeo=false` 時為 false；其他情況就是 `buttonVisible`。

### e. 快捷鍵定義的產生

- `KeyBindingDefGenerator.ImpliedKeyBindingDefs` 是 iterator。它把 `AllDefs` 依 `order` 排序後逐一處理，**只替 `defaultHotKey != KeyCode.None` 的按鈕產生 KeyBindingDef**（IL 對列舉值做 `brfalse`，None＝0）。
- 產生的 KeyBindingDef：
  - defName：`MainTab_` + 按鈕的 defName
  - label：`Toggle {label} tab`
  - category：`MainTabs`
  - `defaultKeyCodeA = defaultHotKey`
  - modContentPack：沿用按鈕的
- 同時把按鈕的 `hotKey` 設成這個 KeyBindingDef。hotReload 時會沿用已經存在的同名 Def。
- 所以 `defaultHotKey=None` 的按鈕 `hotKey` 是 null，原版 `MainButtonsOnGUI` 會直接跳過，`KeyPrefs.xml` 也不會有它的條目。雲端的假設正確。

## 3. Player.log

這次遊戲從 2026-11-03 20:45 開到 21:31，第 2737 行有 `[Main Button Bar System] Loaded.`。

### 本模組的紅字、黃字

**0 筆。**

- 掃了所有 WARN／ERROR 的訊息和堆疊，找 `MainButtonBarSystem`、`MBBS_`、`BarDisplayMode`、`BarAppearance`、`CompatBridge`、`MainButtonArrangement`、`nai.mainbuttonbarsystem`，一筆都沒有。
- 和主按鈕相關、但沒有點名本模組的只有 1 筆，是 21:03:38 的 `Tried to use an uninitialized DefOf of type KeyBindingDefOf`。它出現在載入 Def 的那一段，前後是一整串別的模組的 DefOf 警告（LetterDefOf、DamageDefOf、SoundDefOf、ThingDefOf），沒有堆疊。本模組的 Def 類別沒有用 DefOf 當欄位預設值，所以應該不是本模組造成的。

### 翻譯沒套上的原因（已查到）

`BarDisplayModeDef`、`BarAppearanceDef` 的 DefInjected **整份沒有載入**，所以顯示方式、外觀的名稱和說明都是英文。

- 原版 `LoadedLanguage.LoadData` 用 `GenTypes.GetTypeInAnyAssembly(資料夾名稱)` 找 Def 型別。
- `GetTypeInAnyAssemblyInt` 依序試：
  1. 資料夾名稱當完整型別名稱。
  2. 加上忽略命名空間（Verse、RimWorld 等）。
  3. 混合組件的泛型型別。

  `BarDisplayModeDef` 在命名空間 `MainButtonBarSystem` 裡，三種都找不到。
- 找不到時不會寫進 Player.log，而是加進語言的 `loadErrors`（「Translation data for language Simplified Chinese has N errors」那一類），內容是「dir BarDisplayModeDef doesn't correspond to any def type. Skipping...」。
- 一開始看 log 沒有這句，以為型別有找到，是我判斷錯了。
- **修法**：把 `Languages/*/DefInjected/` 底下兩個資料夾改名成 `MainButtonBarSystem.BarDisplayModeDef`、`MainButtonBarSystem.BarAppearanceDef`。其他模組也都這樣寫，例如 `Vehicles.VehicleDef`、`AlienRace.ThingDef_AlienRace`。簡中、繁中都要改。`tools/gen_translations.py` 沒有提到 DefInjected，應該不受影響。

### 診斷原文

路徑裡的使用者資料夾改寫成 `%USERPROFILE%`。

```
2026-11-03 21:06:37.190  INFO [Verse] [Main Button Bar System] Stage 1 diagnostics (temporary)
  Buttons: 84 MainButtonDefs, 75 buttonVisible, 31 minimized, 55 with iconPath
  Menu def: order 500, last in vanilla order: False
  Hotkeys: 13 buttons have a KeyBindingDef, 71 do not
  Hotkeys: buttons with defaultHotKey but no KeyBindingDef: 0
  Hotkeys: KeyBindingDefs named MainTab_<defName>: 13/13
  Hotkeys: KeyBindingDefs starting with MainTab_ in database: 13
  Hotkeys: sample without KeyBindingDef: Inspect, MintMenus, MintResearch, MintDesignators, Milian_Work, Kiiro_Merchant, HardworkingWork, HardworkingAssign
  Hotkeys: with a key bound in slot A: 13
  Icon sources: resolved 54/55 through mod content holders
    Vanilla Textures Expanded: 14
    Bradson's Main Button Icons (Forked + Expanded): 10
    WVC - Work Modes: 1
    Equal Milking: 1
    Hospital: 1
    Simple Warrants: 1
  Icon sources: unresolved 1, of which ContentFinder still loads 1; sample: Ideos=UI/Buttons/MainButtons/Ideoligions
  Icon sources: textures registered across all mod content holders: 167183
  LoadImage: embedded 4x4 PNG -> returned True, texture 4x4
  LoadImage: custom icon folder %USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\MainButtonBarSystem\Icons has 0 PNG file(s)
  Button table: %USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\MainButtonBarSystem\Diagnostics\buttons.tsv
```

```
2026-11-03 21:20:29.389  INFO [Verse] [Main Button Bar System] Patches on main button methods (temporary)
  MainButtonsRoot.DoButtons:
    prefix: NAI.MainButtonBarSystem (priority 0) MainButtonBarSystem.MainButtonsRoot_DoButtons_Patch.Prefix
    prefix: wowgag.guiperformancepatch (priority 800) GUIPerformancePatch.GppUiSections.Pre
    postfix: wowgag.guiperformancepatch (priority 0) GUIPerformancePatch.GppUiSections.Post48
  MainButtonsRoot.MainButtonsOnGUI:
    prefix: Dubwise.DubsMintMinimap (priority 400) DubsMintMinimap.H_MainButtonsOnGUI.Prefix
    prefix: Dubwise.DubsMintMenus (priority 400) DubsMintMenus.HarmonyPatches+Patch_MainButtonsOnGUI.Prefix
    prefix: wowgag.guiperformancepatch (priority 800) GUIPerformancePatch.GppUiSections.Pre
    postfix: wowgag.guiperformancepatch (priority 0) GUIPerformancePatch.GppUiSections.Post3
  MainButtonsRoot.HandleLowPriorityShortcuts:
    prefix: wowgag.guiperformancepatch (priority 800) GUIPerformancePatch.GppUiSections.Pre
    postfix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.MainButtonsRoot_HandleLowPriorityShortcuts_Patch.Postfix
    postfix: wowgag.guiperformancepatch (priority 0) GUIPerformancePatch.GppUiSections.Post15
    transpiler: Troy_Alias.TacticalGroups (priority 400) TacticalGroups.HarmonyPatches.HandleLowPriorityShortcuts_Transpiler
    transpiler: rimworld.mrhydralisk.LayeredAtmosphereOrbit (priority 400) LayeredAtmosphereOrbit.HarmonyPatches.MBR_HandleLowPriorityShortcuts_Transpiler
  MainButtonWorker.DoButton:
    prefix: wowgag.guiperformancepatch (priority 800) GUIPerformancePatch.GppUiSections.Pre
    postfix: Uuugggg.rimworld.TD_Enhancement_Pack.main (priority 400) TD_Enhancement_Pack.ResearchingIndicator.Postfix
    postfix: rimiris.rimiris (priority 400) RimIris.RimIrisTutorial+Tutorial_BarRect_Patch.Postfix
    postfix: wowgag.guiperformancepatch (priority 0) GUIPerformancePatch.GppUiSections.Post49
    transpiler: OskarPotocki.VanillaTexturesExpanded (priority 400) VanillaTexturesExpanded.Patch_MainButtonWorker+DoButton.Transpiler
  CameraDriver.CameraDriverOnGUI:
    prefix: ChezhouLib.lib (priority 400) ChezhouLib.CustomMission.Levels.Patch_ClCameraInput.Prefix
    prefix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.CameraDriver_CameraDriverOnGUI_Patch.Prefix
    prefix: ray1203.SimpleCameraSetting (priority 400) SimpleCameraSetting.CameraDriverOnGUIPatch.Prefix
    transpiler: com.seohyeon.letterstackcleaner (priority 400) LetterStackCleaner.CameraDriver_Patch.Transpiler
  KeyBindingDefGenerator.ImpliedKeyBindingDefs: no patches
```

診斷裡值得注意的：

- **TooManyButtons、Custom Bottom Bar 沒有啟用**，所以沒有它們的 Harmony id，下一階段的相容功能還是得靠參考模組的原始碼確認。
- `MainButtonsOnGUI` 上有 **Dubs Mint Menus**、**Dubs Mint Minimap** 的 Prefix。Mint Menus 自己也有主按鈕（`MintMenus`、`MintResearch`、`MintDesignators`），它的 Prefix 會不會在某些設定下跳過原版（連帶跳過 `DoButtons` 和快捷鍵迴圈），還沒查。
- `MainButtonWorker.DoButton` 上有 VTE 的 transpiler（換圖示）、TD Enhancement Pack 的研究進度條、RimIris 的教學框。本模組自己畫按鈕時，如果也是呼叫 `Worker.DoButton(rect)`，這些都會照常生效。
- `HandleLowPriorityShortcuts` 上有 Tactical Groups 和 Layered Atmosphere Orbit 的 transpiler，本模組的自訂快捷鍵掛在這裡的 Postfix。

`buttons.tsv` 已經複製到 `docs/research/buttons_stage1.tsv`（84 顆按鈕，沒有本機路徑）。

### 測完留下的設定檔（給雲端參考）

- `Config/Mod_MainButtonBarSystem_MainButtonBarSystemMod.xml`：`alwaysDrawBar=True`（測試用的開關測完沒關）；`customHotkeys` 是空的，所以這次沒有存到任何「原版沒有快捷鍵的按鈕」的自訂鍵。
- `Config/MainButtonBarSystem_Template.xml`（21:31 存）：顯示方式 `CollapseScroll`，`defaultWidth` 100，`minWidth` 80，有一串 `inDrawer` 的 override。
- `MainButtonBarSystem/Exports/Arrangement_20261103_211812.xml`：「存成文件」有產生檔案。
- `KeyPrefs.xml`：`MainTab_AM_LevelSchedule`（Ancient urban ruins 的「楼层管制」）和 `MainTab_History` 都是 **F9**，原本就撞鍵。截圖 03 的快捷鍵欄位裡，楼层管制顯示 F9。可以拿這組來驗證撞鍵警告。

## 4. 測試清單逐項結果

使用者這次只測了一部分，其餘沒測。使用者的判斷是：程式碼看起來沒問題，發佈到工作坊後真的有人回報再處理。下表的「通過」只限於有截圖或設定檔可以佐證的部分。

| 清單 | 項目 | 結果 | 依據／看到的情況 |
|---|---|---|---|
| 第一階段 | 診斷輸出（Stage 1 diagnostics、Patches、buttons.tsv） | 通過 | 見第 3 節 |
| 第一階段 | 模式 1／2 比對（現在改成「总是由本模组绘制底部栏」） | 未測 | 開關有打開（設定檔 `alwaysDrawBar=True`），但沒有回報比對結果 |
| 第一階段 | 捲動模式：滾輪不縮放地圖、箭頭、菜单固定、半露按鈕可點 | 未測 | |
| 第一階段 | 選做：匯入自訂圖片 | 未測 | Icons 資料夾有建立，裡面 0 張 PNG |
| 第二階段 1 | 主選單打開設定，標題寫預設範本 | 通過 | 截圖 01：「主按钮栏设置（默认模板）」 |
| 第二階段 1 | 儲存後產生 `MainButtonBarSystem_Template.xml` | 通過 | 檔案存在 |
| 第二階段 2 | 讀檔後和沒裝本模組時一樣 | 未測 | |
| 第二階段 3 | 遊戲中打開設定，標題寫目前存檔 | 通過 | 截圖 03：「主按钮栏设置（当前存档）」 |
| 第二階段 3 | 拖曳、上下移、隱藏後按快捷鍵、改名、取消還原 | 未測 | |
| 第二階段 4 | 儲存→存檔→讀檔後保留 | 未測 | |
| 第二階段 5 | 摺疊：「更多（N）」、格狀抽屜 | 部分通過 | 截圖 04：抽屜能打開、按鈕排成格狀、下方有設定按鈕。問題見下 |
| 第二階段 5 | 摺疊＋捲動 | **不通過** | 截圖 02：列的右端切點沒對齊、有重複區塊，「更多（26」被截斷 |
| 第二階段 5 | 捲動 | 未測 | |
| 第二階段 6 | 進階：寬度、固定位置、強制顯示、間距、填滿、解鎖菜单 | 未測 | 截圖 03 看得到這些選項；「文字對齊」選項不存在（見下） |
| 第二階段 7 | 範本套到新遊戲 | 未測 | |
| 第二階段 | 顯示方式名稱是中文 | **不通過** | 英文，原因見第 3 節 |
| 第三階段 1 | 沒有紅字 | 通過 | 本模組 0 筆 |
| 第三階段 1 | 顯示方式四種、名稱是中文 | **不通過** | 四種都在，但名稱是英文 |
| 第三階段 2 | 快捷鍵編輯 | 未測 | `customHotkeys` 是空的 |
| 第三階段 3 | 匯出成檔案 | 通過 | `Exports/Arrangement_20261103_211812.xml` 存在 |
| 第三階段 3 | 複製成文字、從剪貼簿匯入、複製成模組 XML | 未測 | |
| 第三階段 4 | 相容橋接 | 不適用 | TooManyButtons、Custom Bottom Bar 沒有啟用 |

### 測試中記下的問題

- **UX：勾選欄不能按住左鍵拖過去一次勾很多格**（使用者回報）。設定畫面的「抽屉」欄要一格一格點，很多按鈕要收進抽屉時很慢。原版的 `Widgets.Checkbox` 有 `paintable` 參數，可以按住拖過去連續勾選（工作分頁就是這樣），建議「抽屉」「显示」欄都支援。
- **UX：只有清單上方那一區能拖動視窗**（使用者回報）。清單和右側面板都蓋滿控制項，所以只剩標題列附近能拖。
- **顯示方式的名稱是英文**（原因和修法見第 3 節「翻譯沒套上的原因」）：
  - 簡中介面下，清單第一列顯示「整条栏（默认值） — Collapse + scroll」（截圖 `screenshots/01_settings_template_mode_label_english.png`）。
  - 右側「显示方式」的四個選項是 Vanilla、Collapse、Scroll、Collapse + scroll（截圖 `screenshots/03_settings_bar_panel.png`）。
  - 外觀（`BarAppearanceDef`）的名稱應該也一樣。
- **UX：每個調整選項旁邊都要有「預設值」按鈕**（使用者要求）。例如「滚动时每个按钮的最小宽度」「按钮默认宽度」「按钮间距」「填满画面宽度」，以及單顆按鈕的寬度、名稱等，按一下就回到預設值。目前只能整份「全部重置」，單顆按鈕的名稱有「还原」。
- **抽屜格子的寬度不能調**（使用者問寬度吃哪個參數，截圖 `screenshots/04_drawer_grid.png`）：`Window_ButtonDrawer` 寫死 `CellWidth = 170f`、`CellHeight = ButtonHeight（35）`、最多 4 欄，不受「按钮默认宽度」或單顆寬度影響。建議至少跟著整條列的寬度設定，或另外給抽屜的格寬、欄數設定。
- **沒有統一的文字對齊（左／中／右）**（使用者回報）。規格 3.2 的表格把「文字對齊」列為整條列的進階設定（使用者在示意圖 v5 確認過）。`BarConfig.labelAlign` 欄位已經有，也會存檔，但設定畫面沒有這個選項，繪製時也沒有讀它。抽屜裡現在是有文字的按鈕靠左、只有圖示的置中，看起來不一致（截圖 04）。
- **設計原則（使用者 2026-11-03）：任何固定參數都不能寫死在程式裡。** 目前寫死的數值（掃 `Source/` 的常數和欄位初始值）：

  | 位置 | 數值 | 建議 |
  |---|---|---|
  | `Window_ButtonDrawer`：`CellWidth` 170、`CellHeight` 35、`MaxColumns` 4、`Pad` 6、`SettingsRowHeight` 30 | 抽屜格寬、格高、欄數、內距 | 給使用者調：抽屜小節放欄數（自動／固定）、格寬（跟随整条栏／自訂）；格高跟主按鈕列 |
  | `BarLayout.Options`：`MoreButtonWidth` 70、`ArrowWidth` 24 | 「更多」按鈕寬、箭頭寬 | 給使用者調。「更多（26）」放不進 70px，右括號被截掉，可能和上面右端重疊的問題有關 |
  | `BarLayout.Options.DefaultWidth` 120、`MinWidth` 80；`BarConfig.DefaultDefaultWidth` 120、`DefaultMinWidth` 100 | 預設寬度、最小寬度的預設值 | **同一個預設值寫在兩個地方，最小寬度一個是 80、一個是 100**。改成只有一個來源 |
  | `BarDisplayModeDef.WheelPixelsPerUnit` 25 | 滾輪一格捲幾 px | 給使用者調（捲動速度） |
  | `Window_ButtonDrawer.ReopenGuardSeconds` 0.3 | 防止點「更多」關了又開 | 內部用，可以放進 XML Def，不用開放到畫面上 |
  | `Dialog_MainButtonBar`：`RowHeight` 30、`HeaderHeight` 36、`ColumnHeaderHeight` 22、`FooterButtonHeight` 34、`Gap` 10、欄寬 22／32／30／46 | 設定視窗本身的版面 | 放進外觀 Def（`BarAppearanceDef`）之類的 XML，讓外觀插槽可以換 |
  | `BarConfig.CurrentFormatVersion` 1 | 存檔格式版本 | 這個要留在程式裡，不算參數 |

  建議做法：預設值統一放在 XML Def（例如 `BarAppearanceDef` 或新的設定 Def），附屬模組可以用 XML 覆蓋；使用者看得到的值放進設定畫面，旁邊都有「預設值」按鈕（見上面的要求）。
- **「按钮间距」滑桿右邊有一段淡淡的「0.000」字樣**（截圖 03，滑桿右端）。看起來像是滑桿數值的殘影或標籤，和左邊「按钮间距：0px」重複。
- **列的右端切得不對，有重複的區塊**（使用者回報，截圖 `screenshots/02_bar_right_end_overlap.png`）：
  - 由左到右依序是右箭頭 `>`、一小段露出來的按鈕（看得到「1」）、「更多（26」。
  - 「更多」的文字被截斷，右括號看不到。
  - 捲動區、右箭頭、「更多」三塊的切點沒有對齊，捲動區裡的按鈕畫到了箭頭和「更多」之間（或底下）。
