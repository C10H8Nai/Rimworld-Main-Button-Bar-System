# 第十輪：實機測試結果

- 測試版本：`claude/round-10-fixes` @ `b3bde94`，repo 根目錄直接當模組資料夾（`1.7/Assemblies/MainButtonBarSystem.dll` 用分支裡的，沒有重編）。
- 環境：RimWorld 1.7.4998 rev1299（Waylith，.NET 10），開發模式開著，畫面 2560×1440。
- 測試前把本機設定恢復成預設狀態：`alwaysDrawBar` 原本是 True（之前測試留下的），改成 False；範本檔先移開（F.3 測完再刪掉測試用的範本）。原檔都有備份，測完放回。

## 總結

| 項目 | 結果 |
|---|---|
| A. 圖示快取（每幀成本） | ✅ 有改善，但⚠️ 改用 78 個模組量：套帝國主題、讀檔後沒開設定視窗，本模組畫列 main 128.7 µs → 本分支 91.1 µs／幀（−29%）；原版外觀兩邊都約 8 µs。大型排序（2,175 個）兩次都沒量成（遊戲卡死、主選單點不動，和本模組無關） |
| B.1 紅字黃字 | ✅ 11 次開遊戲（本分支 10 次、main 對照 1 次），本模組 0 紅字、0 黃字；沒有 `Could not apply` 或 `Field … was not found` |
| B.2 一般操作 | ✅ 沒有新紅字；組商隊選路線時停用的按鈕照原版畫成空白底框，列沒有退回原版 |
| B.3、B.4（選做） | 跳過，見 B 節 |
| C. 摺疊溢出 | ✅ |
| D. 數字框 | ✅ 全部通過；D.4「點地圖」那一下會連帶關掉下面的原版模組設定視窗（原版視窗本身的行為，見 D 節） |
| E. 順序跟著遊戲 | ✅ E.1、E.2、E.4；E.3 跳過（測試排序裡沒有 DragReorder） |
| F. 回歸 | ✅；另外發現兩個小問題（F 節、其他發現） |
| G. UI Rethemed - Main Bar | ✅ 六格的黃字、log 都符合清單；G5 的錯位比「可能錯位」嚴重：右下角控制項整組消失 |

## 實機測試的環境

| 用途 | 排序（`RimCrow\backups\other\`） | 模組數 |
|---|---|---|
| B、C、D、E.1、E.2、F | `ModsConfig_20261104_1231_mbbs_selftest_cleanlog.xml`（原版＋DLC＋VEF、VTE、Bradson 圖示、10 個主按鈕模組等） | 23 |
| E.4 | `ModsConfig_20261104_1309_mbbs_toomanybuttons.xml`（上面那份＋TooManyButtons，相容設定是「本模組優先」） | 24 |
| G1～G3 | `ModsConfig_20261105_1101_mbbs_r10_lfs_first.xml`：Harmony、本體、5 個 DLC、UI Rethemed、本模組 | 9 |
| G4～G6 | `ModsConfig_20261105_1101_mbbs_r10_mbbs_first.xml`：同上，本模組在 UI Rethemed 前面 | 9 |
| A（大型，沒量成） | `ModsConfig_20261105_1101_mbbs_r10_perf_branch.xml`：作者的完整清單（2,201 個）拿掉 RimTalk 系列 26 個（會用 API key 呼叫 AI） | 2,175 |
| A（實際用的） | `ModsConfig_20261105_1312_mbbs_r10_perf84_branch.xml`／`_perf84_main.xml`：第七輪的 `retexture_noroe`（完整清單裡支援 1.7 的主按鈕模組＋前置、漢化，加 VTE、Bradson's、Tang's 換圖示模組，沒有 Rules of Engagement）再拿掉 6 個 RimTalk 相關模組。兩份只差本模組是本分支還是 main 複本 | 78 |

- 每份排序都跑過 check_order：小排序 0 問題；78 個那兩份只剩 1 組已知同名 DLL；大型排序只剩完整清單本來就有的已知項目（1 個找不到資料夾、2 個缺前置、25 組同名 DLL）。
- 存檔：`MBBS_r10_base_theme`、`MBBS_r10_tmb`、`MBBS_r10_G`（UI Rethemed 在前、本模組設定未改）、`MBBS_r10_p84_theme`、`MBBS_r10_p84_vanilla`（A 節），以及大型排序做的 `MBBS_r10_perf_theme`、`MBBS_r10_perf_vanilla`（沒用上）。
- 每次開遊戲的 log 都備份在本機 `RimClaude\logs_saved\20261105_1130_mbbs_round10_base\`（A 節的在 `…\20261105_1233_mbbs_round10_perf_hang\`）。

## A. 圖示快取（效能）

結論：套帝國主題、讀檔後沒開設定視窗時，本模組畫列的成本 main 128.7 µs → 本分支 91.1 µs（−37.6 µs，−29%）。本分支冷啟動讀檔和「同一次遊戲先開過設定視窗再讀檔」都是 91 µs，所以「沒開過設定視窗」已經不會讓它變慢。原版外觀兩邊都約 8 µs。整幀時間五格都在 1.40～1.48 ms，差距比雜訊小。只量了 78 個模組；大型排序兩次都沒量成（見最後一小節）。

### 量法

- 工具：遊戲內建的 Frame profiler（開發模式 → 動作/工具 → Frame profiler）。Settings → Instrumentation 打開，Filters 填：
  ```
  Assembly-CSharp!RimWorld.UIRoot_Play::UIRootOnGUI
  MainButtonBarSystem!MainButtonBarSystem.MainButtonsRoot_DoButtons_Patch::Prefix
  MainButtonBarSystem!MainButtonBarSystem.BarRenderer::TryDraw
  ```
  main 的 prefix 只有兩行（`prefixRan = true; return !BarRenderer.TryDraw(__instance);`），分析器把它當成 trivial 跳過（「1 skipped as trivial」），所以兩邊用 `BarRenderer.TryDraw` 比；本分支的 prefix 91.3 µs 裡 TryDraw 就佔 91.1 µs。`UIRootOnGUI` 在這份排序裡沒有出現獨立節點，UI 總量看分析器內建的 `OnGUI (repaint) → Root_Play`。
- main：`Mods\MBBS_main_perf` 是 main（`11c509b`，DLL 和分支起點 `0d9cf3b` 相同）的複本，packageId 改成 `NAI.MainButtonBarSystem.MainPerf`；讀本分支做的存檔會跳「Mod 不匹配」，按「仍然载入」。測完已移出 Mods。
- 存檔：在本分支用 Dev 快速測試開局（範本是帝國主題，所以一開局就是帝國，設定視窗沒開過），暫停後存成 `MBBS_r10_p84_theme`；再開設定視窗按「全部重設」、儲存，另存 `MBBS_r10_p84_vanilla`（原版外觀，本模組讓給遊戲畫）。
- 每格：從主選單讀檔 → 空白鍵暫停（分析器顯示 ticks +0.0）→ 鏡頭和滑鼠不動 → Clear → 等 12 秒 → 暫停擷取 → 框選整段，看 1,981 幀的平均。

### 數字（78 個模組，2560×1440）

| 情境 | 整幀 | `Root_Play`（OnGUI repaint） | 本模組 | 截圖 |
|---|---|---|---|---|
| main・帝國・冷啟動讀檔 | 1.420 ms | 318.4 µs | TryDraw **128.7 µs** | `round10_a_main_theme.png` |
| 本分支・帝國・冷啟動讀檔 | 1.421 ms | 294.9 µs | TryDraw **91.1 µs**（prefix 91.3） | `round10_a_branch_theme_cold.png` |
| 本分支・帝國・同一次遊戲先開過設定視窗 | 1.466 ms | 279.0 µs | prefix 91.1 µs | （第一次量的） |
| main・原版外觀 | 1.396 ms | 257.8 µs | TryDraw 8.4 µs | `round10_a_main_vanilla.png` |
| 本分支・原版外觀・同一次遊戲先開過設定視窗 | 1.477 ms | 266.1 µs | prefix 7.7 µs | — |

- 第一次量本分支時，同一次遊戲裡為了做原版外觀的存檔先開過設定視窗，和 main 的冷啟動條件不同，所以本分支帝國又冷啟動重量一次（第二列）。兩次都是 91 µs。
- main 帝國另外量過一次（還沒加 TryDraw 計時點）：整幀 1.431 ms、`Root_Play` 319.4 µs，和上表一致。
- 對照預期：「main 明顯比原版外觀慢」成立（TryDraw 128.7 vs 8.4 µs，`Root_Play` 多 61 µs）。「本分支和原版外觀差不多」：以整個 UI 看，`Root_Play` 多 29 µs（約 11%），整幀在雜訊內；但本模組自己仍是 91 vs 8 µs，套主題後畫底板、圖示、文字還要約 83 µs。這 83 µs 再細分是什麼，這次的計時點分不出來。
- `Root_Play` 底下各格都有 22～32 µs 的 `TexUploadLocked`（貼圖上傳），和本模組無關。
- 2,200 個模組時的差距沒量到。main 的慢路徑是每次繪製都經 `ContentFinder` 掃過所有模組找圖，模組越多應該差越多（推測，沒驗證）。

小排序（23 個，只量本分支，`UIRootOnGUI` 有獨立節點）試量：原版外觀 `UIRootOnGUI` 153.3 µs、prefix 0.18 µs；同一次遊戲開設定視窗換成帝國後 157.8 µs、prefix 46.9 µs。

### 大型排序（2,175 個）沒量成

1. 第一次（本分支）：約 21 分鐘到主選單。Dev 快速測試開局，存了 `MBBS_r10_perf_vanilla`、`MBBS_r10_perf_theme`，接著讀 `perf_vanilla` 時卡在 MapInitializing：主執行緒停在引擎原生層（`vma.DLL` → coreclr 等待），CPU 時間不動、視窗沒回應，log 裡沒有本模組的訊息。強制結束（`Player.log`）。
2. 第二次（本分支，改成範本帶主題、開新局不讀檔）：到主選單後點擊沒反應。右上角的 Claude 視窗蓋住主選單按鈕，WOWGAG GUI Performance Patch 的背景工作也還沒做完。正常關閉（`Player_run2_menu_stuck.log`）。
3. 之後改用 78 個模組（`Player_p84_branch.log`、`Player_p84_main.log`、`Player_p84_branch_cold.log`）。

## B. 例外防護

### B.1 紅字、黃字 ✅

十一次開遊戲（小排序 1 次、TooManyButtons 1 次、G 節 4 次、A 節 5 次：大型排序 2 次、78 個模組 3 次，其中 1 次是 main 對照）本模組都是 **0 ERROR、0 WARN**，log 裡沒有本模組的 `Could not apply`、`Field … was not found`（大型排序的 log 有 9 行 `[Miracle] Caravan worker check was not found in …`，是 Miracle 的）。本模組在 log 裡只有這幾行 INFO：

```
[Main Button Bar System] Loaded.
[Main Button Bar System] Diagnostics (dev mode)
  LoadImage: custom icon folder …\MainButtonBarSystem\Icons has 1 PNG file(s)
  Button table: …\MainButtonBarSystem\Diagnostics\buttons.tsv
[Main Button Bar System] Patches on main button methods (dev mode)
    prefix: NAI.MainButtonBarSystem (priority 0) MainButtonBarSystem.MainButtonsRoot_DoButtons_Patch.Prefix
    postfix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.MainButtonsRoot_DoButtons_Patch.Postfix
    postfix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.MainButtonsRoot_HandleLowPriorityShortcuts_Patch.Postfix
    prefix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.CameraDriver_CameraDriverOnGUI_Patch.Prefix
    prefix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_ButtonTextSubtle_Patch.Prefix
    postfix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_ButtonTextSubtle_Patch.Postfix
    prefix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_DrawAtlasDrawTop_Patch.Prefix
    postfix: NAI.MainButtonBarSystem (priority 400) MainButtonBarSystem.Widgets_DrawAtlasDrawTop_Patch.Postfix
[Main Button Bar System] UI Rethemed - Main Bar: not active.
[Main Button Bar System] Play data reloaded (for example a language change); refreshed the button list.   ← 切語言時
[Main Button Bar System] TooManyButtons: removed its patch TooManyButtons.Patch_MainButtonsRoot_DoButtons.Prefix on MainButtonsRoot.DoButtons (this mod has priority).
[Main Button Bar System] TooManyButtons: removed its patch TooManyButtons.Patch_Game_FinalizeInit.Postfix on Game.FinalizeInit (this mod has priority).
```

G 節那幾次多一行 `UI Rethemed - Main Bar: active; …`（見 G 節）。其他模組的紅黃字（VEF 設定類別改名 4 筆、醫療分頁和原版 M 鍵衝突、UI Rethemed 自己的 `SmartSpeedCompat probably needs a StaticConstructorOnStartup attribute`）和本模組無關。

### B.2 一般操作 ✅

開過的分頁、商隊、顯示方式、抽屜：沒有新紅字。組商隊 →「改變路徑」時，地圖以外的主按鈕停用：本模組的列照原版的寫法畫成空白底框（原版 `MainButtonWorker.DoButton` 停用時本來就只畫底框），沒有整條退回原版，取消後恢復。截圖 `screenshots/round10_b2_route_disabled.png`。

### B.3、B.4（選做）：跳過

B.3 要編譯一個會丟例外的 `MainButtonDef`，這台只有 .NET 執行階段、沒有 SDK，沒做。B.4 的測試模組（XML 補丁刪掉 `behaviour`）和排序 `ModsConfig_20261105_1210_mbbs_r10_b4.xml` 準備好了，但時間排給了 A 節，沒開遊戲測；測試模組已移出 Mods。

## C. 摺疊溢出 ✅

摺疊、關掉「填滿畫面」、預設寬度 250、派系收進抽屜：列上的按鈕等比例縮小到剛好放得下，「更多 (1)」和菜单完整露出（`round10_c2_fold_250.png`）。寬度改回 80：按鈕回到 80，右邊留空，不縮放（`round10_c3_fold_80.png`）。

## D. 數字框（進階 → 預設寬度，範圍 40～400）✅

| 步驟 | 結果 |
|---|---|
| 1. 全選刪掉 | ✅ 框是空的，不跳回 |
| 2. 依序打 3、35、350 | ✅ 3、35 時標籤仍是 250px；350 時立刻變 350px |
| 3. 打英文字母 | ✅ 打不進去 |
| 4. 打到一半點別處 | ✅ 點視窗內空白處、按 Enter、點地圖（視窗外）都結束編輯並顯示目前值；點地圖後按 8、9 不會改到值。底部列沒有點（點下去會開分頁），只點了地圖 |
| 4. 從「選項 → 模組設定」打開時按 Enter | ✅ 整條欄寬度框、背景不透明度框、裝飾圖位移框各試一次，下面的模組設定視窗都還在 |
| 4. 單顆按鈕寬度打 150、不透明度打 50 | ✅ 1 → 15 → 150、5 → 50，每一位都打得進去，沒掉焦點 |
| 5. 打完不點別處直接拖滑桿 | ✅ 300 → 拖到 155，框跟著顯示 155；按「預設值」回到 120，之後不再變回去 |
| 6. A（工作）打 150 再點 B（醫療） | ✅ B 仍是 0（跟隨整條欄），框顯示 B 的值；按取消後重開，A、B 都是 0 |
| 7. 位移先打 `-` | ✅ `-` → `-30`，裝飾圖往左移 |
| 8. Esc | 編輯途中按 Esc：設定視窗直接關掉，改動全部捨棄（等於取消），下面的模組設定視窗還在 |

補充：D.4 第一次「點地圖」時，下面那個原版的「Main Button Bar System」模組設定視窗一起被關掉了。另外單獨開它再點地圖，也一樣會關，所以是原版 `Dialog_ModSettings` 點視窗外就關的行為，不是 Enter 的問題；本模組的視窗不受影響。

## E. 順序跟著遊戲

| 步驟 | 結果 |
|---|---|
| E.1 新存檔只選帝國主題 | ✅ 順序和原版完全一樣（`round10_e1_vanilla_bar.png`、`round10_e1_imperial_bar.png`） |
| E.2 移動後儲存、再全部重設 | ✅ 把「工作」往後移兩格：列照移動後的順序；全部重設：回到遊戲順序與原版外觀（`round10_e2_moved.png`、`round10_e2_reset.png`）。拖曳排序用 computer use 拖不動（遊戲收不到拖曳事件），改用「下移」按鈕 |
| E.3 DragReorder | 跳過（測試排序裡沒有） |
| E.4 TooManyButtons 本模組優先 | ✅ 範本是星航主題、沒設順序。第一局（Dev 快速測試）和存檔、回主選單、讀檔後的第二局，兩張底部列截圖逐像素相同，都是原版順序（`round10_e4_tmb_game1.png`、`_game2.png`）。log 有兩行「removed its patch …（this mod has priority）」 |

## F. 回歸

- F.1 ✅ 四種顯示方式各切一次（原版／摺疊／捲動／摺疊＋捲動）；捲動（滾輪）、抽屜（兩顆）、固定右端（「工作」改名「工作X」、圖示＋文字）、隱藏（研究）都正常；整條欄四種圖示模式都正常，「只顯示圖示」裝著 VTE 也只顯示圖示（第七輪的問題已修好）。抽屜右下角的齒輪可以打開設定。截圖 `round10_f1_collapsescroll.png`。
- F.2 ✅ 主選單切英文（遊戲中不能換語言）：log 一行 `Play data reloaded`，列和設定視窗正常、沒有紅字。⚠️ 但「來源模組」欄顯示的是**上一個語言**的名稱：簡中介面時是 Core／Biotech，切到英文後變成「核心／生物技术／文化」（`round10_f2_english_template_window.png`，G 節英文、日文截圖也一樣）。推測 `ButtonBaseline.SourceModName` 是在換語言途中、新語言的模組名稱還沒套上時重抓的。
- F.3 ✅ 主選單改範本（星航）並儲存，Dev 快速測試開新局：列是星航主題（`round10_f3_newgame_template.png`）。

## G. UI Rethemed - Main Bar（工作坊 3826865706）

每格都重開遊戲。G1、G2 用 Dev 快速測試開的新局，開局後先存 `MBBS_r10_G`（本模組設定未改），G3～G6 都讀這個存檔。它的「Main bar」在它的模組設定頁切換（存到 `Mod_3826865706_UIRethemedMainBarMod.xml`）。

| # | 順序 | Main bar | 本模組設定 | 黃字 | log（讀檔後第一次畫列） | 畫面 |
|---|---|---|---|---|---|---|
| G1 | UI Rethemed 在前 | 開 | 未改 | ✅「UI Rethemed - Main Bar 正在绘制底部栏，这里的设置不会生效。要改用本模组，请在它的模组设置中关闭「Main bar」，或在模组列表中把本模组排在它前面。」 | ✅ `mainButtons = True; bar drawn by an earlier mod (this mod's prefix was skipped)` | ✅ 它的列（右邊有日期、溫度、時間控制） |
| G2 | UI Rethemed 在前 | 開 | 選帝國、儲存 | ✅ 同 G1，選的當下和儲存後重開都一樣 | （同 G1 那次） | ✅ 仍是它的列 |
| G3 | UI Rethemed 在前 | 關 | 選帝國、儲存 | ✅ 改之前、改之後都沒有 | ✅ `mainButtons = False; bar drawn by the game or a later mod (this mod left it: its settings are untouched)` | ✅ 本模組的列；右下角控制項、研究分頁、世界檢視面板位置正常 |
| G4 | 本模組在前 | 開 | 未改 | ✅ 同 G1 | ✅ `mainButtons = True; bar drawn by the game or a later mod (this mod left it: …)` | ✅ 它的列 |
| G5 | 本模組在前 | 開 | 視窗裡選帝國 | ✅ **當下**換成「UI Rethemed - Main Bar 的「Main bar」也开着，它会把右下角控件和窗口位置按它自己的栏来摆放，画面可能错位。建议在它的模组设置中关闭「Main bar」。」 | （同 G4 那次） | 本模組的列；⚠️ 錯位，見下 |
| G6 | 本模組在前 | 關 | 選帝國、儲存 | ✅ 改之前、改之後都沒有 | ✅ `mainButtons = False; … this mod left it …` | ✅ 本模組的列；位置正常 |

dev 模式的補丁清單也印出兩邊 prefix 的順序：G1～G3 是 UI Rethemed 在前，G4～G6 是本模組在前，兩個都是 priority 0。log 那行是從各次的 `Player.log` 抄的（備份在 `…\20261105_1130_mbbs_round10_base\Player_G*.log`），沒有另外截圖。

各格截圖（`screenshots/`）：

| # | 黃字 | 整個畫面（含右下角） | 分頁視窗 | 世界檢視面板 |
|---|---|---|---|---|
| G1 | `round10_g1_notice.png` | `round10_g1_screen.webp` | — | — |
| G2 | `round10_g2_notice.png` | `round10_g2_screen.webp` | — | — |
| G3 | （沒有黃字）`round10_g3_window.png` | `round10_g3_screen_before.webp`（改之前）、`round10_g3_screen.webp` | `round10_g3_tab.webp` | `round10_g3_world.webp` |
| G4 | `round10_g4_notice.png` | `round10_g4_screen.webp` | — | — |
| G5 | `round10_g5_notice_live.png` | `round10_g5_screen.webp`、`round10_g5_bottomright.webp` | `round10_g5_tab.webp` | `round10_g5_world.webp` |
| G6 | （沒有黃字）`round10_g6_window.png` | `round10_g6_screen_before.webp`（改之前）、`round10_g6_screen.webp` | `round10_g6_tab.webp` | `round10_g6_world.webp` |

G1、G2、G4 是 UI Rethemed 畫的列，右下角、分頁、世界面板都是它自己的版面，所以只截整個畫面。

**G5 的錯位**（`round10_g5_screen.webp`、`round10_g5_bottomright.webp`、`round10_g5_vs_g6_offset.png`）：

- 右下角的時間控制（暫停／速度）、日期、時間、天氣、遊戲設定圖示**整組不見了**，只剩「缺少床鋪」之類的提醒。它把這些控制項搬進自己的列，但列是本模組畫的，所以哪裡都沒畫。暫停只能靠空白鍵。
- 研究分頁和世界檢視面板都比 G6 高約 5 px（2560×1440 下），和底部列之間露出一條地圖。

**其他項目**

1. ✅ 每種情況的 log 那行都對（上表）。
2. ✅ 主選單打開設定視窗（編輯範本）：沒有 UI Rethemed 的黃字（`round10_g_extra2_mainmenu_template.png`）。
3. ✅ 黃字換行：簡中一行；英文、日文兩行，分頁跟著往下，沒有被切掉或壓到（`round10_g_extra3_english.png`、`_japanese.png`）。
4. ✅ 不裝 UI Rethemed：沒有黃字，log 是 `UI Rethemed - Main Bar: not active.`。
5. **G4 的感受：會誤導。** 本模組已經排在前面，玩家照著「或在模組列表中把本模組排在它前面」去看清單，會發現已經是這樣，然後不知道該做什麼；真正原因是本模組還沒有任何設定所以把列讓給它。建議 G4（`ThisModYielded` × 開）另給一句，例如「本模組還沒有任何設定，所以底部列由 UI Rethemed - Main Bar 畫。要改用本模組，在這裡改任一項設定即可（它的 Main bar 開著時畫面可能錯位，建議一起關掉）。」
6. G5 那句的「画面可能错位」偏輕描淡寫，實際是右下角的時間控制整組消失。建議直接寫出「右下角的時間控制、日期等會消失」。

## 其他發現（不在這輪清單內）

- **日文的底部按鈕文字被切**：設定視窗下方「デフォルトテンプレートとして保存」左右兩端被切掉，「デフォルトテンプレートを適用」也略被切（`round10_g_extra3_japanese.png`）。
- 「來源模組」欄的語言落後一次（F.2）。
- computer use 操作上：遊戲裡的勾選框（`ToggleInvisibleDraggable`）要分開送「按下、放開」才會切換；拖曳排序拖不動。只影響自動化測試，不影響玩家。

## 測完的還原

- 本模組設定（`alwaysDrawBar` True）和範本檔從備份放回（`RimClaude\backups\config\mbbs_round10_20261105_1102\`，雜湊和備份相同）；`Prefs.xml` 和測試前相同（語言簡中）。
- `Mods\MBBS_main_perf`、`Mods\MBBS_r10_B4_test` 已移出 Mods。
- Frame profiler 的設定按了「Reset to defaults」（instrumentation 關、Filters 回預設）。
- `Config\Mod_3826865706_UIRethemedMainBarMod.xml` 是 G 節產生的，「Main bar」已切回開（它的預設值），檔案留著。
- **ModsConfig 目前是 `perf84_branch`（78 個）**，要在 RimCrow 匯入 `ModsConfig_20261103_2245_full.xml` 換回完整清單。測試開始前的 ModsConfig 是另一輪測試的 25 個排序，備份在上面同一個資料夾。
- 測試存檔留在存檔管理的 Default 資料夾（`MBBS_r10_*` 7 個），不需要可以刪。
