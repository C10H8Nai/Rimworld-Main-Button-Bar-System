# 第十一輪：實機測試結果

- 測試版本：`claude/round-12-custom-theme` @ `3dbd9e7`（包含第十、十一輪的所有 commit），用 repo 裡雲端編的 DLL，沒有重編。
- 舊格式資料用 `claude/round-10-fixes` @ `b3bde94` 做（存檔格式 2）。
- 環境：RimWorld 1.7.4998 rev1299（Waylith，.NET 10），開發模式開著，畫面 2560×1440，UI 縮放 1.25，語言簡中。
- 測試前把本機設定恢復成預設狀態：`alwaysDrawBar` 改成 False；原本的範本先移開。兩個都在測完後放回。

## 總結

| 項目 | 結果 |
|---|---|
| A.1 讀舊存檔 | ✅ 底部列和第十輪逐像素相同；設定視窗每顆按鈕的值相同；本模組 0 紅字 |
| A.2 存檔格式 3 | ✅ `formatVersion` 3，`overrides` 只列出改過的欄位 |
| A.3 新遊戲用範本 | ✅ |
| A.4 匯入舊檔案、剪貼簿 | ✅ 兩種匯入的結果，都和舊存檔轉換後的設定完全相同 |
| A.5 SoftRace 排法（格式 2） | ✅ 報告和結果都和第十輪一樣 |
| A.6 停用模組再讀檔 | ✅ 停用期間轉換時，那顆按鈕的每個欄位都明確保留；重新啟用後沒被模組預設蓋過（存檔是手動做的，見 A 節） |
| B.1～B.5 跟著模組預設 | ✅ B.5 要先在範例加 `<icon>` 才測得到 |
| C.1～C.6 平分時尊重單顆寬度 | ✅ C.4 照設計等比例縮小，但⚠️ 窄按鈕的圖示會畫到按鈕外，「更多」只剩一個字 |
| D.1～D.5 回歸 | ✅ |
| 其他發現 | ⚠️ 裝 TooManyButtons（本模組優先）時，同一次啟動的第二局起，`order` 相同的按鈕順序會對調（見「其他發現」） |

## 實機測試的環境

排序檔都在 `RimCrow\backups\other\`，基本排序沿用使用者指定的第十輪 `perf84_branch`（78 個）。

| 用途 | 排序 | 模組數 |
|---|---|---|
| 舊資料、A～D 大部分 | `ModsConfig_20261105_1631_mbbs_r12_base.xml`：perf84 加 TooManyButtons、SoftRace 範例模組 | 80 |
| A.6（停用 Hospital）、B.2、B.4、B.5 | `…_mbbs_r12_nohospital.xml`：base 拿掉 Hospital 和它的漢化 | 78 |
| A.6（停用 SoftRace） | `…_mbbs_r12_nosoftrace.xml`：base 拿掉 SoftRace | 79 |
| A.6（SoftRace 重新啟用）、D.2 | `…_mbbs_r12_cbb.xml`：base 拿掉 TooManyButtons、加 Custom Bottom Bar | 80 |

- check_order：四份都只剩 1 組已知的同名 DLL（`0multiplayerapi.dll`）。
- SoftRace 範例模組從 repo 的 `examples/` 複製到 `Mods\MBBS_Example_SoftRace`，測完已移走。B.2 時把它的 `MainButtons.xml` 改成 `inDrawer false`，並加 `<icon>MBBSExample/SoftRace/Icons/LeafGold</icon>`（B.5 用），之後又換回原檔。
- 存檔（存檔管理的 Default 資料夾）：
  - 第十輪版本做的：`MBBS_r11_old`、`MBBS_r12_imperial`、`MBBS_r12_nai`
  - 這輪做的：`MBBS_r11_A2`、`MBBS_r11_B`、`MBBS_r11_A6src`（手動改的，見 A.6）、`MBBS_r11_A6h`、`MBBS_r11_A6s`
- 匯出檔：`r11_old_file.xml`、`r11_old_file_noGuests.xml`（手動拿掉 `Guests`，D.3 用），測完已從 Exports 移到本機的測試資料夾。
- log：本機 `_資料與測試\MainButtonBarSystem\round-12\logs\`。

## A. 舊存檔、舊檔案

用第十輪版本準備的資料（格式 2）：
- 範本：`Work` 改名「工作T」、`Schedule` 隱藏。
- `MBBS_r11_old`（Dev 快速測試開的新局）：
  - 顯示方式「摺疊＋捲動」，文字靠左。
  - 工作T 只顯示圖示；方案圖示＋文字。
  - 研究、模組中心固定左端；Robots 固定右端。
  - 建築規劃寬度 200。
  - 故人旧影用自訂圖示 `test_gold_gear_64.png`。
  - 边缘真香隱藏；動物、野生動物收進抽屜。
  - TooManyButtons 自己的按鈕明確設成顯示。
- 匯出：存成檔案 `r11_old_file`、複製成文字 `r11_old_text`。

| # | 結果 |
|---|---|
| A.1 | ✅ 讀 `MBBS_r11_old`：底部列（y 1404～1440）和第十輪的截圖逐像素相同（`round11_a1_bar_compare.png`）。設定視窗的清單、固定、抽屜、每顆按鈕的值都一樣，選一顆按鈕時多了「重置这个按钮」。本模組 0 紅字黃字 |
| A.2 | ✅ 重存成 `MBBS_r11_A2`：`formatVersion` 3，`overrides` 只列出改過的欄位（下面那段）。TooManyButtons 自己的按鈕在格式 2 是一筆沒有任何欄位的 override，轉換後變成 `hidden False`：執行期預設是隱藏，所以「明確顯示」保住了 |
| A.3 | ✅ 回主選單用 Dev 快速測試開新局：套了範本（工作T、管制隱藏），其他照原版 |
| A.4 | ✅ 匯入 `r11_old_file`（報告：61 顆兩邊都有，外觀主題沒有設定），以及剪貼簿文字 `r11_old_text`（先按「全部重置」）。每次匯入後都「複製成文字」，把 `<config>` 拿去和 A.2 存檔的設定比：兩次都完全相同 |
| A.5 | ✅ 匯入 SoftRace 的排法「Leaf folk layout (example)」：報告文字和第十輪一樣（`round11_a5_report.png`）。結果也一樣（`round11_a5_result.png`）：摺疊模式、Leaf folk 主題；History、Factions 進抽屜；leaf folk 不在抽屜；原本的設定保留 |
| A.6 | ✅ 見下 |

A.2 存檔裡的 `config`：
```xml
<formatVersion>3</formatVersion>
<displayMode>CollapseScroll</displayMode>
<customTheme IsNull="True" />
<order IsNull="True" />
<labelAlign>Left</labelAlign>
<overrides>
  <li><defName>Work</defName><label>工作T</label><iconMode>Icon</iconMode></li>
  <li><defName>Schedule</defName><hidden>True</hidden></li>
  <li><defName>Research</defName><pin>Left</pin></li>
  <li><defName>CL_ModuleCenter</defName><pin>Left</pin></li>
  <li><defName>AIRobots</defName><pin>Right</pin></li>
  <li><defName>Architect</defName><width>200</width></li>
  <li><defName>Assign</defName><iconMode>Both</iconMode></li>
  <li><defName>GuRenGuiLai_Archive</defName><icon>file:test_gold_gear_64.png</icon></li>
  <li><defName>PrisonerRecruit</defName><hidden>True</hidden></li>
  <li><defName>Animals</defName><inDrawer>True</inDrawer></li>
  <li><defName>Wildlife</defName><inDrawer>True</inDrawer></li>
  <li><defName>TMB_MainButton</defName><hidden>False</hidden></li>
</overrides>
```

**A.6 的做法**：`MBBS_r11_old` 裡沒有「模組會被停用」的按鈕設定，所以複製一份存成 `MBBS_r11_A6src`，在格式 2 的 `overrides` 手動加兩筆：
- Hospital 的 `Patients`：改名「病人X」、固定右端。
- SoftRace 的 leaf folk：只改名「LF2」。在格式 2，這等於「明確不在抽屜」。

| 步驟 | 結果 |
|---|---|
| 停用 Hospital，讀 `A6src`，存成 `A6h` | `Patients` 每個欄位都明確寫出（`hidden False`、`inDrawer False`、`label 病人X`、`width 0`、`pin Right`、`forceShow False`、`iconMode FollowBar`、`icon @own`）：模組不在時算不出繼承值，所以全部保留 |
| 重新啟用 Hospital，讀 `A6h` | ✅ 「病人X（固定右端）」和停用前一樣 |
| 停用 SoftRace，讀 `A6src`，存成 `A6s` | leaf folk 每個欄位都明確寫出，包括 `inDrawer False` |
| 重新啟用 SoftRace（原本的 XML，預設收進抽屜），讀 `A6s` | ✅ leaf folk 留在列上，沒有被模組預設收進抽屜；「更多 (2)」只有動物、野生動物 |

## B. 跟著模組預設走（SoftRace 範例）

| # | 結果 |
|---|---|
| B.1 | ✅ 新局切到摺疊模式，只把 leaf folk 改名「叶民R」並儲存：仍在抽屜，仍是圖示＋文字 |
| B.3 | ✅ 同一局把「收進抽屜」取消再勾回（勾回後「預設值」變灰），存成 `MBBS_r11_B`：存檔裡 leaf folk 只有 `label`，沒有 `inDrawer` |
| B.2 | ✅ 關遊戲，範例 XML 改成 `inDrawer false`，重開讀 `MBBS_r11_B`：leaf folk 移出抽屜（抽屜空了，「更多」消失），名字還是「叶民R」 |
| B.4 | ✅ 先替 leaf folk 設快捷鍵 K，再按「重置这个按钮」：名字回到「Leaf folk」，其他回到模組預設，快捷鍵 K 還在 |
| B.5 | ✅ 範例本來沒有用 `<icon>` 宣告圖示，這次在測試用的 XML 加上 LeafGold。在圖示選擇器選「使用按钮原本的」：顯示按鈕自己的圖示（白色葉子），不是模組宣告的金色葉子；按「默认值」回到金色葉子 |

## C. 平分時尊重單顆寬度

量法：截圖的最下面一列找按鈕邊界，算出每顆的寬度（2560 寬，UI 縮放 1.25）。

| # | 結果 |
|---|---|
| C.1 | ✅ 原版模式，建築規劃寬度 200：248px（200×1.25）；其他按鈕平分約 77.5px；整條列到 2559 剛好填滿（`round11_c1_vanilla_w200_bar.png`） |
| C.2 | ✅ 原本就是半格的按鈕（`minimized`，例如 Equal Milking、悬赏令）約 39px，是一般的一半 |
| C.3 | ✅ 摺疊模式、填滿畫面：建築規劃 248px，其他平分；「更多」約 77px，選單按鈕 101px，填滿整條（`round11_c3_collapse_fill_bar.png`） |
| C.4 | ✅（照設計）⚠️ 建築規劃、工作T、勤工分配、醫療、方案都設 400：五顆各縮成約 442px，其餘每顆 12px（`Tuning.xml` 的 `splitMinButtonWidth`），整條列沒有超出畫面。「更多」和選單按鈕點得開（用滑鼠提示確認位置後點）。⚠️ 但窄按鈕的圖示會畫到按鈕外、彼此疊在一起；「更多」約 13px，字只剩「更」（`round11_c4_5x400_bar.png`、`round11_c4_5x400_right_zoom.png`） |
| C.5 | ✅ 「整条栏」出現「让 5 个有自己宽度的按钮都改为跟随整条栏」，按下後全部回到平分，那顆按鈕消失 |
| C.6 | ✅ 摺疊（關閉填滿）：按鈕放不下時照原本的做法等比例縮小（建築規劃 130px，其他 79px，比例 250:150）。捲動模式：建築規劃 249px、其他 150px、半格 75px，加捲動箭頭（`round11_c6_collapse_nofill_bar.png`、`round11_c6_scroll_bar.png`） |

C.4 補充：設寬度時，一開始在摺疊模式點錯位置，按到「宽度」的「默认值」，建築規劃的寬度被清掉。順便確認了「默认值」會把這一項回到跟隨整條列。

## D. 回歸

| # | 結果 |
|---|---|
| D.1 | ✅ base 排序的新局：TooManyButtons 自己的按鈕在清單裡是隱藏（執行期預設） |
| D.2 | ✅ cbb 排序。Custom Bottom Bar 本來沒有任何設定，所以先在它的設定檔手動加兩筆 override：隱藏野生動物；研究 `customWidth`＋`width 180`＋`customName RS`。按「导入 Custom Bottom Bar 的设置」：野生動物隱藏，研究改名 RS、有自己的寬度，顯示方式變成捲動、填滿畫面關。圖示沒有測（它的 `customIcon` 要放檔案在它的資料夾） |
| D.3 | ✅ 先把「勤工分配」「訪客」都設 300，再匯入 `r11_old_file_noGuests`（手動把 `Guests` 從按鈕列表拿掉），只勾「大小与对齐」。報告列出「只有你的游戏有、保留你的设置：1 个（访客）」。結果：訪客保留 300（對方遊戲沒有這顆）；勤工分配回到跟隨整條列（對方有、但沒改過）；建築規劃 200、文字靠左（來自檔案）；名稱、圖示、顯示方式沒變 |
| D.4 | ✅ 四種顯示方式、改名、隱藏、抽屜、固定、圖示模式在上面各節都用過，正常 |
| D.5 | ✅ 本模組優先。同一次啟動裡，第一次讀 `MBBS_r11_old` 時，存成的 A.2 存檔是 `hidden False`；之後第二次讀 `MBBS_r11_old`，清單裡 TMB 也是顯示 |

## 其他發現

1. **TooManyButtons 在時，`order` 相同的按鈕順序不穩定**（`round12_a1_imperial_order_swap.png`）：
   - 現象：同一次啟動裡，第一局（讀舊存檔）的順序是「Equal Milking→求醫者」「经商管理→悬赏令」，和第十輪一樣；之後的局（新局、讀其他存檔）兩對都對調。
   - 兩對的 `order` 都相同（54、66）。
   - 拿掉 TooManyButtons、改裝 Custom Bottom Bar 的那次啟動，第一局和第二局都是「经商管理→悬赏令」，沒有對調。
   - 推定：和 TooManyButtons 相容橋接處理「遊戲的按鈕順序」的方式有關（`BarModel.IgnoreLiveOrderOf`、還原 `def.order`），沒有查到確切原因。第十輪的 E.4 是在 23 個模組的排序測的，那時沒有 `order` 相同又同時顯示的按鈕，所以沒發現。
2. `MBBS_r11_old` 的「目前不在栏上：按钮自身的出现条件不成立」提示照舊。

## 測完的還原

- `Config\` 裡本模組的設定、範本、`Prefs.xml`、`KeyPrefs.xml`、UI Rethemed、TooManyButtons、Custom Bottom Bar 的設定，都從 `round-12\config_backup\` 放回，雜湊和測試前相同。TooManyButtons 的設定檔在測試中被它自己改寫過，也一樣放回。
- `Mods\MBBS_Example_SoftRace` 已移走。測試匯出檔已從 `Exports` 移到本機測試資料夾。
- 遊戲語言最後切回簡中。
- ModsConfig：最後一次啟動用的是 `ModsConfig_20261105_1101_mbbs_r10_mbbs_first.xml`（9 個），要不要換回由使用者決定。
- 測試存檔留在 Default 資料夾（`MBBS_r11_*`、`MBBS_r12_*`）。
