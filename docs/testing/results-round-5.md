# 第五輪：實機測試結果

- 測試分支：`claude/round-5`（commit `91b7a44`）
- 環境：RimWorld 1.7.4995 rev309（Waylith，.NET 10）

## 1. 靜態檢查

- `verify_dll.py`（`1.7/Assemblies/MainButtonBarSystem.dll`）：**問題 0**。
- 組件參考和上一輪相同，版本和遊戲一致。
- 對 UnityStubs 的 61 個成員引用：問題 0。
- `check_order.py`：
  - 目前啟用的排序（原版＋本模組，8 個）：0 問題。
  - 完整清單（2,201 個）：只有原本就有的 2 個缺前置、25 組同名 DLL。
- `xml_check.py`（Defs，含 Tuning.xml）：問題 0。
- 翻譯檔（29 種語言，`Languages/*/Keyed`、`DefInjected`）：
  - 全部是合法的 XML。
  - 除了英文，每種語言 136 個鍵。英文只有 Keyed 的 126 個，因為 DefInjected 的英文就在 Def 本身。
  - 沒有多出或缺少的鍵。
  - 佔位符：10 種語言的 `MBBS_Compat_OursNotice` 用了兩次 `{0}`，英文只用一次。這 10 種是 Czech、French、German、Italian、Polish、Russian、Slovak、Spanish、SpanishLatin、Ukrainian。`string.Format` 允許重複使用同一個佔位符，所以不是錯誤，只是譯文把模組名稱寫了兩次。

## 2. 原版行為確認（從遊戲的 Assembly-CSharp.dll 反組譯 IL）

### `Widgets.DrawAtlas`

**兩個多載的關係**：`DrawAtlas(Rect rect, Texture2D atlas)` 只做一件事：呼叫 `DrawAtlas(rect, atlas, drawTop: true)`。所以攔截 3 參數的版本就涵蓋兩者。如果兩個都攔截，2 參數的會先進一次，3 參數的又進一次，要注意不要重複處理。

`DrawAtlas(Rect rect, Texture2D atlas, bool drawTop)`：

1. **只在 Repaint 事件畫**，其他事件直接 return。
2. 把 `rect` 的 x、y、寬、高各自 `Mathf.Round`，再用 `UIScaling.AdjustRectToUIScaling` 對齊到目前介面縮放的像素格。
3. **四角大小**：
   - `corner = min(貼圖寬度 × 0.25, rect.height / 2, rect.width / 2)`。
   - 再用 `UIScaling.AdjustCoordToUIScalingCeil` 往上取整，讓它在目前縮放下是整數像素。
   - 所以畫面上的四角大小 = **貼圖寬度（像素）的四分之一**（以 UI 單位計），而且最多是按鈕高度的一半、寬度的一半。
   - **只看貼圖寬度，不看高度。**
4. **九宮格的切法**（UV，以 PNG 看到的方向為準；`DrawTexturePart` 會把 y 翻轉，所以「左上」就是圖片的左上）：
   - 四角：各取外側 25%×25%。
   - 上、下邊：中間 50% 寬 × 外側 25% 高。
   - 左、右邊：外側 25% 寬 × 中間 50% 高。
   - 中央：中間 50%×50%。
5. **畫的順序和位置**（c = 四角大小）：
   - `drawTop` 為真時，先畫左上角 `(x, y, c, c)`、右上角。
   - 左下角、右下角一定畫。
   - 中央 `(x+c, y+c, w−2c, h−2c)`。`drawTop` 為假時，中央往上延伸 c，蓋住原本頂邊的位置。
   - `drawTop` 為真時畫上邊 `(x+c, y, w−2c, c)`。
   - 下邊 `(x+c, y+h−c, w−2c, c)`。
   - 左邊 `(x, y+c, c, h−2c)`、右邊。`drawTop` 為假時兩邊也往上延伸 c。
   - 每一塊都用 `Widgets.DrawTexturePart` → `GUI.DrawTextureWithTexCoords`，**邊和中央是拉伸，不是重複平鋪**。
6. `drawTop = false` 就是「沒有上框」的版本：上面兩角和上邊不畫，左右邊和中央延伸到頂端。

**給主題作者的貼圖建議**（`docs/API.md` 第 4 節要照這個修正）：

- **貼圖要是正方形。** UV 的四分之一是分別對寬和高算的，但四角在畫面上的大小只看寬度。長方形貼圖的四角在垂直方向會被拉長或壓扁。
- **貼圖的像素大小直接決定畫面上的邊框粗細**，不是「解析度越高越清楚」：
  - 64×64 → 四角 16 UI 像素。
  - 128×128 → 照算是 32，但主按鈕高 36，會被限制成 18，四角的圖會縮小。
  - 想要多粗的邊框，就用「邊框粗細 × 4」當貼圖邊長。
  - 主按鈕列的按鈕高 36，所以四角最大 18，貼圖邊長超過 72 就沒有意義。
- 邊框和圓角的圖案要完全放在外側四分之一內。中間 50% 的部分會被拉伸，不要放會變形的花紋。
- 原版 `ButtonSubtleAtlas`（`UI/Widgets/ButtonSubtleAtlas`）的實際尺寸沒查到：1.7 的原版貼圖打包在資源檔裡，`Data/Core` 底下沒有 PNG。
- `API.md` 目前寫「64×64 works well」「keep the border art inside the outer quarter」，方向是對的。建議補上兩點：
  - 「四角的畫面大小 = 貼圖寬度 / 4，上限是按鈕高度的一半」。
  - 「一定要正方形」。

### `TexButton.OpenInspectSettings`

- 貼圖路徑：**`UI/Buttons/DevRoot/OpenInspectSettings`**（`TexButton` 的靜態建構子用 `ContentFinder<Texture2D>.Get` 載入）。
- 用在哪裡：開發模式畫面上方那排除錯按鈕（`DebugWindowsOpener.DrawButtons`）。提示文字是「Open the view settings. This lets you see special debug visuals.」，點了開 `ToggleDebugSettingsMenu`。
- **長得像不像齒輪：使用者在遊戲裡看過，和想要的齒輪不一樣**（見第 4 節）。
- 如果不像齒輪，原版還有這些候選。都是選項視窗左邊分類的圖示，定義在 `Data/Core/Defs/Misc/OptionCategoryDefs/OptionCategories.xml`：
  - `UI/Icons/Options/OptionsGeneral`
  - `UI/Icons/Options/OptionsGameplay`
  - `UI/Icons/Options/OptionsUI`
  - `UI/Icons/Options/OptionsControls`
  - `UI/Icons/Options/OptionsDev`
  - `UI/Icons/Options/OptionsMod`
  - `UI/Icons/Options/OptionsVideo`
  - `UI/Icons/Options/OptionsAudio`

## 3. Player.log

這一輪只開了一次遊戲：完整清單（2,201 個），2026-11-04 00:36～01:20，英文介面，開發模式。

- 第 2718 行有 `[Main Button Bar System] Loaded.`。
- **本模組的紅字、黃字：0 筆。**
- 和主按鈕相關、但沒點名本模組的只有 1 筆，和前幾輪相同：載入 Def 時的 `Tried to use an uninitialized DefOf of type KeyBindingDefOf`，夾在別的模組的 DefOf 警告中間。
- 整份 log 的 ERROR 有 773 筆，都是別的模組。
- 「Diagnostics (dev mode)」在 01:00:17，內容和前幾輪相同。

「Patches on main button methods (dev mode)」，01:08:00：

```
  （MainButtonsRoot.DoButtons、MainButtonsOnGUI、HandleLowPriorityShortcuts、MainButtonWorker.DoButton、
   CameraDriver.CameraDriverOnGUI、KeyBindingDefGenerator.ImpliedKeyBindingDefs 和上一輪完全相同，略）
  Widgets.ButtonTextSubtle:
    prefix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_ButtonTextSubtle_Patch.Prefix
    postfix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_ButtonTextSubtle_Patch.Postfix
  Widgets.DrawAtlas:
    prefix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_DrawAtlas_Patch.Prefix
    postfix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_DrawAtlas_Patch.Postfix
  Widgets.DrawAtlas:
    prefix: ColoredCategoriesMod (priority 400) ColoredCategories.Widgets_DrawAtlas_Patch.Prefix
    prefix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_DrawAtlasDrawTop_Patch.Prefix
    postfix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_DrawAtlasDrawTop_Patch.Postfix
```

- `ButtonTextSubtle`、2 參數的 `DrawAtlas`：只有本模組。
- 3 參數的 `DrawAtlas`：另外還有 **Colored Categories**（`ferny.coloredcategories`，工作坊 3323569935，有附原始碼）的 Prefix，排在本模組前面。
  - 它的做法：一個靜態旗標 `shouldReplaceAtlas`，只在建築分頁（`MainTabWindow_Architect.DoCategoryButton`，用 transpiler）畫分類按鈕的期間設成 true。旗標為 true 時，把 `atlas` 換成它自己的 `CategoriesBackground` 貼圖。它不會跳過原方法。
  - 本模組只在畫主按鈕列和抽屜時換貼圖，和它不會同時發生，所以**沒有衝突**。只要兩邊都維持「有旗標才動手、不跳過原方法」就好。
  - 附帶：它的 `LoadFolders.xml` 只有 `v1.5`，所以 1.7 載入的是 `1.5` 資料夾的 DLL，不是 `1.6` 的。這是它自己的問題，和本模組無關。
- 本模組同時補丁兩個 `DrawAtlas` 多載。2 參數的只會轉呼叫 3 參數的，所以畫一次 2 參數版本，本模組的兩組 Prefix／Postfix 都會跑。如果換貼圖的判斷是「`atlas == ButtonSubtleAtlas` 才換」，第二次進來時已經換掉了，不會重複處理；但如果還有其他副作用（例如計數、記錄），要確認不會做兩次。其實只補丁 3 參數的版本就夠了。

## 4. 測試清單逐項結果

使用者在完整清單（英文介面）測了一部分。「通過」只限於有截圖或使用者明確回報的。

| 步驟 | 項目 | 結果 | 依據／看到的情況 |
|---|---|---|---|
| 1 | 原版樣式的三個分頁 | 做出來了，但**設計要改** | 截圖 round5_01：「Simple／Advanced／Appearance」三個分頁，選中的和內容連在一起。使用者認為简单／进阶不該和外观並排（見下面的問題） |
| 2 | 只有標題列能拖動視窗 | 通過 | 使用者回報 |
| 3 | 「抽屜」欄連續勾選 | 通過 | 使用者回報 |
| 4 | 撞鍵（Tab 不誤報、M 有說明） | 未測 | |
| 5 | 抽屜的齒輪圖示 | **不通過**（圖示） | 有出現方形圖示按鈕，但 `UI/Buttons/DevRoot/OpenInspectSettings` 的樣子和使用者想的齒輪不一樣（沒有截圖）。提示、點擊打開設定沒有回報問題 |
| 6 | 英文「Drawer」欄標題 | 未測 | |
| 7 | 已是預設值的「預設值」按鈕是半透明 | 通過 | 截圖 round5_02：三條滑桿都是預設值，「Default」按鈕的字是暗的 |
| 8 | 外觀分頁 | 通過 | 截圖 round5_01：原版主題卡片有名稱、來源、說明、預覽，選中有綠點。預覽要加左右箭頭（見下面的問題） |
| 9 | 其他語言 | 未測 | 這次只用了英文介面。英文的設定畫面沒有被切掉的文字（截圖 round5_01、round5_02） |
| 10 | 文字對齊的滑鼠位移 | 未測 | |
| — | 捲動「放得下就沒有箭頭」的說明 | 通過 | 截圖 round5_02：最小寬度下面有「Arrows appear only when the buttons do not fit…」 |

### 測試中記下的問題

- **「简单／进阶」和「外观」不該並排成三個分頁**（使用者回報）：
  - 简单／进阶是「顯示多少選項」的切換，外观是另一個頁面，性質不同，放在同一排分頁會讓人以為是三個並列的頁面。
  - 建議：
    - 上方分頁只放「頁面」：「按钮」（排序、顯示、抽屜等，也就是現在简单／进阶的內容）和「外观」。之後有新頁面再加。
    - 简单／进阶改成「按钮」頁面裡的切換。例如頁面右上角一個「进阶」勾選框或兩段式開關，選中狀態要明顯。
    - 外观頁之後如果也有進階選項，用同一個切換。
    - 切換狀態記在模組設定裡，所有存檔共用，下次打開維持上次的選擇。
- **滑桿右端被另一個模組的數字輸入框蓋住**（使用者回報，截圖 `screenshots/round5_02_bettersliders_overlap.png`）：
  - 現象：「Minimum button width when scrolling」滑桿右端疊著「80.000」，「Scroll arrow width」疊著半透明的「24.000」。上一份報告說的「0.000」殘影也是同一件事。
  - **來源：BetterSliders Continued**（`linnun.bettersliders`，工作坊 3663410182，MIT 授權，有附原始碼；另有中文翻譯 `soulwolf.ch.bettersliders`）。不是本模組的 bug，上一輪「改成自己畫滑桿」也沒有避開它，因為 `Dialog_MainButtonBar` 第 886、896 行還是呼叫 `Widgets.HorizontalSlider`。
  - BetterSliders 的做法（`HarmonyPatches/ExpandedSliderHorizontal.cs`）：
    - 它補丁 9 個參數的 `Widgets.HorizontalSlider(Rect, float, float, float, bool, string, string, string, float)`。
    - Prefix 依傳進來的 `rect` 算出輸入框：寬 `rect.width / 5`，高是 Tiny 字型的一行，貼在 rect 的右上角。
    - 顯示方式設定是 `Hover`（預設）時，滑鼠移上去才畫輸入框，**直接蓋在滑桿右邊五分之一上**，沒有另外讓出空間。設定是 `AlwaysOn` 時，才會先把滑桿縮短（輸入框寬度＋5）。使用者沒有改過它的設定，所以是 Hover。
    - 原版的選項視窗也一樣會被蓋住，這是它的設計。
  - 使用者希望本模組能相容。建議：
    - **自己畫滑桿，不呼叫 `Widgets.HorizontalSlider`**，BetterSliders 就不會介入。
    - 在每條滑桿右邊預留一格本模組自己的數字輸入框（`Widgets.TextFieldNumeric`），寬度放 Tuning。這樣有沒有裝 BetterSliders，都能直接輸入數字，版面也固定。輸入框和「預設值」按鈕要對齊。
    - 保留 `Widgets.HorizontalSlider` 的話，沒辦法靠調整版面避開：輸入框的位置是從傳進去的 rect 算的，而滑桿本身也畫滿同一個 rect，縮短 rect 只會讓輸入框跟著移過去。所以在 Hover 模式下一定會蓋到。
- **抽屜設定按鈕的圖示不像齒輪**（使用者回報，沒有截圖）：原版的 `UI/Buttons/DevRoot/OpenInspectSettings` 是開發模式工具列「Open the view settings」的圖示，實際樣子和使用者要的齒輪不同。
  - 建議：本模組自帶一張齒輪貼圖（例如 `Textures/MainButtonBarSystem/Gear.png`），當作 Tuning `drawerSettingsIcon` 的預設值，不要依賴原版的除錯圖示。主題也可以換這張圖。
  - 原版的候選（第 2 節列的選項視窗分類圖示，例如 `UI/Icons/Options/OptionsGeneral`）長什麼樣子，也還沒在遊戲裡確認，不建議直接拿來用。
- **外觀分頁的預覽要加上捲動的左右箭頭**（使用者要求，截圖 `screenshots/round5_01_appearance_tab.jpg`）：
  - 現在的預覽只有「Architect／Work／Merchant／勤工分配」四顆按鈕和「More (4)」。
  - 左右箭頭也是主題可以換的貼圖（`arrowLeft`、`arrowRight`），而且在列上的位置固定，換了主題很容易看出差別，所以預覽裡也要畫出來。例如在按鈕兩端各放一顆。
  - 同一張截圖：「外观」分頁看得到「原版」主題的卡片，有名稱、來源模組、說明和預覽，選中的有綠色圓點。
