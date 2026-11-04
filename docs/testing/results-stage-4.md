# 第六輪＋第四階段：實機測試結果

- 測試版本：`main`（commit `4a6e8b8`，含 PR #6、#7），`modVersion` 0.7.0
- 環境：RimWorld 1.7.4995 rev309（Waylith，.NET 10），完整模組清單 2,201 個，有啟用 BetterSliders Continued。

## 1. 靜態檢查

- `verify_dll.py`（`1.7/Assemblies/MainButtonBarSystem.dll`）：**問題 0**。
- 組件參考和上一輪相同（沒有新增組件），版本和遊戲一致。
- 對 UnityStubs 的 73 個成員引用：問題 0。
- `check_order.py`（完整清單）：只有原本就有的 2 個缺前置、25 組同名 DLL，和本模組無關。
- `xml_check.py`（Defs）：問題 0。
- 翻譯檔：29 種語言都是合法的 XML，沒有多出或缺少的鍵，佔位符和英文一致。英文 161 個鍵；其他語言 181 個，多出來的是 DefInjected。
- 貼圖：
  - 5 款主題的按鈕、整條列、抽屜、更多、齒輪都是 64×64 正方形，箭頭 32×32。
  - `UI/Gear.png` 64×64。
  - `About/ModIcon.png` 128×128，`About/Preview.png` 640×360。
  - 都符合 `DrawAtlas` 的條件：正方形，不超過 72。
- `LoadFolders.xml`：`v1.7` 載入根目錄和 `1.7`，符合預期。

## 2. 發佈流程試做

`python tools/make_release.py RimClaude/MainButtonBarSystem/release_test_20261104`（repo 外的資料夾）：**通過**。

- 輸出 146 個檔、1.1 MB。最上層只有 `About`、`1.7`、`Defs`、`Languages`、`Textures`、`LoadFolders.xml`、`API.md`。
- 沒有原始碼、`.csproj`、`.pdb`、`.git`、`docs` 或測試截圖。
- DLL 和 `Textures` 跟 repo 裡的完全相同。
- 沒有上傳工作坊。

## 3. 主題配色對照

做法：讀遊戲資料夾裡 5 個 DLC 的 `Data/<DLC>/About/Preview.png`（1920×1080，官方預覽圖）。

- **整張圖**的主要顏色五款都一樣，是深海軍藍（#101010、#101030、#103030…），和配色調查說的「主選單背景都是同一類太空圖」一致，所以分不出差別。
- 改量**下方 logo 那一帶**（x 300–1300、y 820–1060）：只取亮度和飽和度夠的像素，依色相分組後取平均。

| DLC | logo 帶量到的顏色（由多到少） | 主題 | 對照結果 |
|---|---|---|---|
| Royalty | 金 #E0CF6C、橘 #DC8D44（人物外套）、紅 #A3554A | 帝國 Imperial：海軍藍底、金框、象牙白／金字 | **像** |
| Ideology | 青 #43888C、珊瑚紅 #CC5A56 | 信條 Creed：深青底、珊瑚紅框、淡珊瑚／金字 | **像** |
| Biotech | 青 #66B0B5、深紅 #A84457 | 基因實驗室 GeneLab：藍底 #1D3966 一類、青框、粉紫角飾、淡藍字 | 大致像，但偏藍。logo 和 `primaryColor`（#7DB3B3）都是**青色**，不是藍色；第二色是深紅，不是粉紫。建議底色往青藍調整，角飾改深紅 |
| Anomaly | 鼠尾草綠 #AEBC7E（遠多於其他）、暖橘 #BE8761；紅 #A60A1A 只有零星幾點 | 虛空 Void：黑底、紅色發光框、**紅色齒輪和箭頭**、鼠尾草綠字 | **不太像**。預覽圖的主色是鼠尾草綠和青綠色背景，紅色只是巨石的小光點。建議框線、齒輪、箭頭改用鼠尾草綠，紅色只留在很細的內線或滑鼠移上去的狀態 |
| Odyssey | **紫 #9074BB**（遠多於其他）、暖橘 #CCA275、淡青 #9DDCE0 只有零星 | 星航 Starfarer：太空黑底、鋼灰框、**青色**齒輪、箭頭和底線、白字 | **明顯不像**。Odyssey 的 logo 是紫色（「ODYSSEY」字樣和四角星符號）。配色調查說「主視覺沒拿到」，所以漏掉了。建議主色改紫 #9074BB 一類，青色當次要色 |

- 對照圖放在本機 `RimClaude/MainButtonBarSystem/theme_vs_dlc_20261104.png`，沒有放進 repo，因為裡面有 DLC 的官方美術。
- 量測是在本機用 PowerShell（System.Drawing）做的，腳本沒有放進 repo。

## 4. Player.log

遊戲在 11-04 07:02 更新成 **1.7.4998 rev1299**（之前是 4995 rev309）。對新版重跑 `verify_dll.py`：本模組問題 0。

### 25 個的測試排序（12:26，`ModsConfig_20261104_1026_mbbs_selftest.xml`）

- 進地圖 13 秒，「null texture passed to GUI.DrawTexture」就累積到 9,999 筆，log 停止記錄。92% 來自 Dubs Mint Minimap，8% 來自 Rules of Engagement，跟本模組無關，但這次的 log 不能拿來判斷本模組。

### 23 個的測試排序（12:31～12:33，拿掉上面兩個）

- 空貼圖警告 0 筆，log 沒有到上限。
- ERROR 4 筆，都和本模組無關：Vanilla Expanded Framework 舊的設定類別名稱（`VFECore.VFEGlobalSettings`、`HeavyWeapons.HeavyWeaponsSettings`、`AnimalBehaviours.*`），遊戲自己改用 `VEF.*` 的新名稱讀進來了。
- 本模組沒有 ERROR。開發者模式下有 2 筆 WARN：
  - `Type ButtonPainter probably needs a StaticConstructorOnStartup attribute, because it has a field <ActiveBothIcon>k__BackingField of type Texture2D`
  - `Type Window_ButtonDrawer probably needs a StaticConstructorOnStartup attribute, because it has a field defaultSettingsIcon of type Texture2D`
  - 這是原版在開發者模式下的檢查：類別有 `Texture2D` 型別的靜態欄位，卻沒有 `[StaticConstructorOnStartup]`。目前兩個欄位都是在主執行緒繪製時才設定，實際上不會出錯，但開發者模式的玩家會看到。
  - 第五輪的完整清單只有 1 筆（`Window_ButtonDrawer`）；`ButtonPainter.ActiveBothIcon` 是第四階段「圖示＋文字」新加的。
  - 建議：把這兩個欄位移到一個加了 `[StaticConstructorOnStartup]` 的類別，或改成不是 `Texture2D` 型別的靜態欄位（例如放在非靜態的物件裡）。改 `defaultSettingsIcon` 時可以順便處理第 5 節「切換語言後快取的貼圖被銷毀」的問題。
- 其他 WARN 都是原版或引擎的：別的模組 About 裡的前置沒有 `downloadUrl`、關遊戲時的 `Persistent allocation`。
- 這次從 12:32:33 開新局到 12:33:27 關遊戲，大約 1 分鐘，log 只代表「載入＋開局」沒有問題，還沒包含逐項操作。

## 5. 測試清單逐項結果

各項狀態見 `checklist-stage-4.md`。這輪實際測到的只有：載入＋開局（第 4 節）、設定視窗兩個分頁（B1 ✅），以及遊戲中切換語言（B7 ❌，見下面）。其他項目還沒測；M 節（圖示來源和換圖示模組）的預期結果是靜態推算的，還沒進遊戲驗證。

### 切換語言後，按鈕清單的名稱停在切換前的語言（B6／B7 時發現）

- 現象：遊戲中把語言從英文切成簡中後，下方按鈕列是簡中（「建筑规划」「工作」），設定視窗的按鈕清單、右邊的標題、「自定义名称（留空＝原本的名称：Architect）」都還是英文。模組本身就是中文的按鈕（故人归来的「故人旧影」）不受影響。重開遊戲就正常（使用者 2026-11-04 確認）。截圖：`screenshots/stage4_01_language_switch_stale_labels.webp`（切換後）、`screenshots/stage4_02_after_restart.webp`（重開後）。
- 原因（反組譯原版確認）：
  - `LanguageDatabase.SelectLanguage` 不會重開遊戲，而是排一個 long event，依序跑 `PlayDataLoader.ClearAllPlayData()` 和 `LoadAllPlayData()`。所有 Def 重新讀取、重新套補丁和翻譯，換成新的物件。
  - `[StaticConstructorOnStartup]` 是靠 `RuntimeHelpers.RunClassConstructor` 執行，同一個程序裡只會跑一次，所以 `ModStartup` 不會再跑，`ButtonBaseline.Capture()` 也不會重新擷取。
  - `DisplayLabel` 和右邊的標題讀的是 `ButtonBaseline.Get(name).Label`，也就是切換前那批 Def 的 `label`。
- 建議修法：在重新載入完成後重新擷取基準狀態，例如 Harmony Postfix `PlayDataLoader.LoadAllPlayData`，或在 `PlayDataLoader.Loaded` 從 false 變回 true 時重抓。這時其他模組的靜態建構子同樣不會重跑，所以重抓的結果仍然符合「補丁都套完、執行期修改之前」的定義。
- 同一個原因可能還影響（推測，沒驗證）：
  - `ClearAllPlayData` 會清掉模組內容並銷毀貼圖。`CustomIcons.cache`、`Window_ButtonDrawer.defaultSettingsIcon` 這類靜態欄位如果存的是 `ContentFinder` 拿到的 `Texture2D`，切換語言後可能指向已銷毀的物件，畫出空白。
  - 用 PNG 匯入的圖示是自己 `LoadImage` 讀的，不受影響。
  - 驗證方法：設一顆內建路徑的自訂圖示，再切一次語言，看那顆圖示和齒輪還在不在。

## 6. 工作坊用的檔案

- `About/Preview.png`：目前是用圖形畫的佔位圖，畫面乾淨、字看得清楚。建議換成實機截圖，最能看出這個模組在做什麼：
  - 上半是「使用前」：原版在大型模組清單下擠成縮寫的主按鈕列（例如 Fshn、Mmry、Cmbt、PrsnrR）。
  - 下半是「使用後」：捲動＋「更多」抽屜。
  - 使用者已經截好素材，放在本機 `RimClaude/MainButtonBarSystem/workshop_images/`。
  - 如果要放主題，用本模組自己的貼圖，不要用 DLC 美術。
- `About/ModIcon.png`：齒輪＋格子＋一排按鈕，在小尺寸下看得出是「主按鈕列＋抽屜」，可以用。
- `docs/workshop/description.txt`：
  - 沒提到 **5 款內建主題**（帝國、信條、基因實驗室、虛空、星航，預設仍是原版），建議在 Features 加一行。
  - Compatibility 那段寫 TooManyButtons、Custom Bottom Bar 的設定「can be imported」，簡中、繁中也寫「可導入／可匯入對方的設定」。但第四階段的清單自己寫這一節**完全沒驗證過**，原始碼也找不到。建議在驗證前改成「實驗性」，或先拿掉「可匯入」。
  - 可以補一句和 BetterSliders 相容（設定畫面的滑桿自己畫，有自己的數字輸入框），裝了 BetterSliders 的玩家會在意這點。
