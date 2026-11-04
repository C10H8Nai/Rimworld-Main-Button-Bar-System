# 第七輪：實機測試結果

- 測試版本：PR #8 還沒合併（`origin/main` 仍是 `4a6e8b8`），所以直接測 `claude/round-7`（`6165087`）。
- 環境：RimWorld 1.7.4998 rev1299（Waylith，.NET 10），`Assembly-CSharp` 1.7.9803.38985。

## 總結

| 項目 | 結果 |
|---|---|
| 1. B7 切換語言（英↔日、英↔繁中＋TooManyButtons 本模組優先） | ✅ |
| 2. 開發模式的 StaticConstructorOnStartup 警告 | ✅ 0 筆（上一輪 2 筆） |
| 3. Gene lab、Void、Starfarer 主題和 DLC 對照 | ✅ 配色一致、字都看得清楚；Void 滑鼠移上去的紅光偏弱；停用狀態未測 |
| 4. 工作坊文字、模組選單顯示 | ✅；簡中有兩處用詞可以順手改 |
| 5. Preview.png | ✅ 已重做，參數在第 5 節 |
| 6. 日文標點 | 不用改，官方也全用半形 |
| 7. 第四階段剩下的項目 | ⚠️ 「只顯示圖示」裝了 Vanilla Textures Expanded 就失效；M 節未測；其他通過 |
| 本模組的紅字、黃字 | 4 次開遊戲都是 0 |

## 0. 靜態檢查

- `verify_dll.py`（`1.7/Assemblies/MainButtonBarSystem.dll`）：**問題 0**。
- 組件參考和上一輪相同：System.Runtime 10、Assembly-CSharp 1.7.9800.9270、UnityStubs 1.0、0Harmony 3.0，其餘是 System.* 10。遊戲已更新到 9803，名稱相同、版本較新，可以綁上；verify_dll 也通過。
- `xml_check.py`：問題 0。
- 翻譯：29 種語言的 Keyed 鍵和英文一樣，沒有缺鍵或多鍵，佔位符一致。`MBBS_Compat_OursNotice` 有幾種語言用了兩次 `{0}`，這是合法的寫法。
- 讀過 `Patches/PlayDataReload.cs` 和原版的對應流程：
  - 原版切換語言時，`LoadedModManager.ClearDestroy` 只清 `runningMods`，不清 `runningModClasses`；`CreateModClasses` 遇到已經建過的 Mod 類別會跳過（反組譯確認）。所以本模組的 Mod 建構子和 `PatchAll` 不會再跑一次，補丁不會重複，`Play data reloaded` 理論上只會印一行。
  - 第一次載入時，補丁是在 `LoadAllPlayData` 執行途中才套上的，那一次呼叫不會觸發 postfix；`Captured` 的判斷是第二層保險。

## 實機測試的環境

- 排序：`ModsConfig_20261104_1231_mbbs_selftest_cleanlog.xml`（23 個），另外用 `_1309_mbbs_toomanybuttons.xml`、`_1309_mbbs_custombottombar.xml`（各 24 個）測相容。check_order 都是 0 問題。
- 開發模式開著，用 Dev quickstart 開局，存檔 `MBBS_round7_test`、`MBBS_round7_tmb`。
- 四次開遊戲的 log 都備份了。本模組在四份 log 裡都是 **0 ERROR、0 WARN**。每份各有 4 筆 ERROR，都是 Vanilla Expanded Framework 的設定類別改名（`VFECore.VFEGlobalSettings` 等），和本模組無關。
- 語言只能在主選單換。所以每次切換的流程都是：存檔 → 回主選單 → 選項換語言 → 讀檔 → 打開設定視窗。

## 1. B7 重測：切換語言

**通過。** 截圖：`screenshots/round7_01_language_switch.webp`、`round7_04_toomanybuttons.webp`。

切換前先設好：Work 改名成 TestWork 並換成遊戲內的燈泡圖示，Research 用匯入的 `test_gold_gear_64.png`，主題用 Gene lab。

| 檢查 | 英文 → 日文 | 日文 → 英文 | 英文 → 繁中（TooManyButtons、本模組優先） | 繁中 → 英文 |
|---|---|---|---|---|
| 清單、右側標題、「原本的名稱」是新語言 | ✅ アーキテクト、時間割…；「空欄で元の名前: 優先順位」 | ✅ | ✅ 建築、工作、醫療…；相容區塊「本模組優先」 | ✅ |
| `Play data reloaded` 只印一行 | ✅ 1 行 | ✅ 1 行 | ✅ 1 行 | ✅ 1 行 |
| 改過名的按鈕保留自訂名稱 | ✅ TestWork | ✅ TestWork | — | — |
| 清掉自訂名稱後顯示新語言的原名 | ✅ 優先順位 | ✅ Work | — | — |
| 抽屜齒輪、主題貼圖、遊戲內圖示、匯入的 PNG 圖示 | ✅ 都正常，沒有空白或粉紅 | ✅ | ✅ | ✅ |
| 「本模組優先」的相容橋接還是由本模組畫列 | — | — | ✅ TooManyButtons 的 Inspect、Mint 按鈕沒有回來 | ✅ |

- 沒有改名的模組按鈕（Medical Tab - Continued、Hospital、Simple Warrants、Mech Work Tab）切到日文後還是英文，因為那些模組本身沒有日文翻譯。
- 和本模組無關的現象：切到繁中後，選項 → 模組設定的清單裡，有幾個其他模組的圖示變空白（Animal Behaviours、Dubs Mint Menus、Hospital、Vanilla Expanded Framework、Vanilla Textures Expanded）。本模組的圖示正常。

## 2. 開發模式的 StaticConstructorOnStartup 警告

**通過。** 上一輪（12:32）有 2 筆 `Type ButtonPainter / Window_ButtonDrawer probably needs a StaticConstructorOnStartup attribute`，這次 4 次開遊戲都是 0 筆。

## 3. 主題：基因實驗室、虛空、星航

截圖：`screenshots/round7_02_theme_gene.webp`、`round7_02_theme_void.webp`、`round7_02_theme_star.webp`。每張由上到下是：設定頁的預覽、遊戲裡滑鼠移到 Medical 上、Schedule 分頁打開、「更多」抽屜（滑鼠在 Patients 上）、DLC 的 `Preview.png`。

- 截圖用 PrintWindow 只擷取遊戲視窗。Claude 操作電腦時，螢幕邊緣會有一圈橘紅色光暈，直接截螢幕的話底部按鈕列會被染色，所以不用那種截圖。
- 每次截圖時「捲動時的最小按鈕寬度」都設成 150，讓左右箭頭出現；圖示模式設成「圖示＋文字」，方便看字。
- 「停用」狀態沒有測到：這份排序裡沒有能變成停用的按鈕（切到世界地圖也一樣），標成未測。

| 主題 | 對照的 DLC | 一般 | 滑鼠移上去 | 分頁打開 | 「更多」、抽屜、齒輪、箭頭 | 文字 |
|---|---|---|---|---|---|---|
| Gene lab | Biotech：青綠 logo、深紅、深藍太空 | 青藍底、青綠細框，四角是深紅色的基因方塊 | 下方一條青綠線 | 外框變亮，內側有一圈深紅框，加青綠底線 | 抽屜底色和列相同，齒輪和箭頭是青綠色 | 淡青白，看得清楚 |
| Void | Anomaly：鼠尾草綠 logo、黑、紅色碎屑 | 黑底、鼠尾草綠外框，上下各一條 1px 細紅線，接縫是碎片切角。**沒有紅光** | 下方紅線從 1px 變 2px，底色稍亮，文字變白 | 外框變亮，文字白色，下方是 2～3px 紅線，上方內側有一點紅光 | 黑底綠框加碎片切角，齒輪和箭頭是綠色 | 一般狀態是淡綠灰字，看得清楚 |
| Starfarer | Odyssey：紫色 logo、青綠星雲、金棕色 | 深紫底、薰衣草紫框，四角是青色鉚釘 | 文字變青色，下方一條青色推進器線 | 外框變亮，文字青色，加青色底線 | 抽屜是深紫色，齒輪和箭頭是薰衣草紫 | 淡紫白，看得清楚 |

- 三款的配色都和對應的 DLC 預覽圖一致。
- Void 的規格是「平常沒有紅光、只有細紅線；滑鼠移上去或打開時才有光和紅色底線」。一般狀態符合。滑鼠移上去的變化偏弱：我用像素量了下方 4 列，一般是 (52,21,20)/(136,41,41)/(22,13,11)/(7,9,7)，滑鼠移上去是 (62,29,27)/(137,41,39)/(133,43,35)/(62,24,22)，也就是紅線多了一列、外面多一點暈。正常距離看幾乎分不出來。如果想要「看得出在發光」，可以把滑鼠移上去時的紅光再加強一點。

## 4. 工作坊文字

讀過 `docs/workshop/description.txt` 和 `About/About.xml`：

- 上一輪的建議都有改進去：5 款內建主題（預設原版、不需要擴充）、匯入其他模組的設定標成實驗性、和 Better Sliders 相容。
- 和本機驗證過的結果一致：「圖示包（Bradson's、Vanilla Textures Expanded）照常運作」符合反組譯和掃描的結論，補丁後的 `iconPath` 和同路徑貼圖都是後載入的贏，本模組的「圖示來源」也是照這個規則算的。
- 簡中段落有兩處用了台灣用語，可以順手改（不影響理解）：
  - 「外观分页」→ 簡中通常說「外观标签页」或「外观选项卡」。
  - 「找不到类别」→ 程式的 class 在簡中通常叫「类」，例如「找不到类」。
- 模組選單裡的說明顯示：**通過**（`screenshots/round7_06_mod_menu.webp`）。主選單 → Mods → 選本模組：右側是新的 Preview.png，版本 0.7.0、遊戲版本 1.7，英文說明的段落和換行都正常，捲到最後一段「Settings: Options > Mod settings…」也完整。

## 5. Preview.png

用使用者截好的素材產生，結果可以用，已經換掉 `About/Preview.png`（原本的佔位圖還在 main）。

```
python tools/make_preview.py full_vanilla_crowded_shortened.webp drawer.png \
  --before-crop 330,0,850,55 --after-crop 0,0,729,223 --size 1280x720 --split 0.32 \
  --after-caption 'Main Button Bar System: readable buttons, the rest in a "More" drawer'
```

- 輸出 1280×720、362 KB（不到 1 MB，沒有減色）。
- 上半：原版 2000 px 寬的按鈕列，截取 x 330～1180 這段，看得到 Fshn、Mmry、Cmbt、PrsnrR、Bsnss、Rltns 這些縮寫。
- 下半：「更多」抽屜打開的實機截圖（抽屜的 7 顆按鈕、設定按鈕、下方的 More (7)）。
- 下方標題改了：素材裡的抽屜截圖看不到捲動箭頭，所以預設標題的「scroll」拿掉，改成「readable buttons, the rest in a "More" drawer」。之後如果補一張「摺疊＋捲動」同時看得到箭頭和抽屜的截圖，可以換回預設標題。
- 只用了遊戲畫面和本模組的原版外觀，沒有 DLC 美術。
- 這台電腦沒有 `python3`，系統 Python 也沒有 Pillow，是用本機另一個裝了 Pillow 的虛擬環境執行的，腳本本身沒改。

## 6. 日文翻譯的標點

**不用改。** 讀遊戲附的官方日文翻譯（`Data/*/Languages/Japanese (日本語).tar`，Core 和 5 個 DLC 共 1,537 個檔），只算日文字後面的標點：

| 來源 | 「、」 | 「。」 | 「,」 | 「.」 |
|---|---|---|---|---|
| Core | 0 | 0 | 13,274 | 11,412 |
| Royalty | 0 | 0 | 1,611 | 1,962 |
| Ideology | 0 | 0 | 1,935 | 3,038 |
| Biotech | 0 | 0 | 1,601 | 2,269 |
| Anomaly | 0 | 0 | 1,896 | 2,473 |
| Odyssey | 0 | 0 | 1,177 | 1,853 |

官方全部用半形「,」「.」（例如「標的の心に憤怒を呼び起こし,誰彼構わず攻撃させます.」），一個「、」「。」都沒有。本模組的日文也沒有任何「、」「。」，句讀都是半形，和官方一致。

## 7. 第四階段還沒測的項目

| 項目 | 結果 | 說明 |
|---|---|---|
| 四種圖示模式（整條列） | ⚠️ 3/4 | 照遊戲原本的 ✅、只顯示文字 ✅、圖示＋文字 ✅；**只顯示圖示 ❌**（見下面） |
| 整條列的設定 | ✅ | 最小按鈕寬度、圖示模式、文字對齊：調整時按鈕列馬上預覽，存檔後保留 |
| 匯入 PNG 圖示 | ✅ | `test_gold_gear_64.png` 用在 Research，切兩次語言都正常 |
| 文字對齊 | ✅（靠左沒測） | 置中、靠右都正常 |
| 其他模組設定：TooManyButtons | ✅ | 會偵測並提示；「匯入 TooManyButtons 的設定」會套用對方的排序（故人旧影 移到 Animals 後面）、清掉抽屜勾選，並提示要按 Save；改成「本模組優先」會提示要重開，重開後就由本模組畫列 |
| 其他模組設定：Custom Bottom Bar | ✅ | 相容區塊會用灰字列出對方的參考值（Width 120px、Gap 0px、Fill the screen）；匯入後預設按鈕寬度變 120px、間距 0、勾上填滿螢幕寬度，並提示要按 Save。沒有測切換優先 |
| checklist M 節（換圖示的排序） | 未測 | 使用者要用電腦，沒有開那三份 `_noroe` 排序。之後只要各開到主選單，讀開發模式輸出的 `Diagnostics/buttons.tsv` 的 `iconPath`／`iconSourceMod` 欄就能對照 |

截圖：`screenshots/round7_03_icon_modes.webp`、`round7_04_toomanybuttons.webp`、`round7_05_custombottombar.webp`。

### 只顯示圖示：裝了 Vanilla Textures Expanded 時，按鈕寬一點就還是會顯示文字

- 現象：整條列設成「只顯示圖示」、最小按鈕寬度 150 時，每顆按鈕都是圖示加文字。對齊選「照遊戲原本的」或「靠右」都一樣。
- 本模組的做法：`BarAppearanceDef.Draw` 對 `IconMode.Icon` 不做任何處理（註解寫「Default and Icon: the game shows the icon whenever there is one」），靠的是原版的行為。原版 `MainButtonWorker.DoButton` 在 `def.Icon != null` 時，傳給 `Widgets.ButtonTextSubtle` 的是空字串（反組譯 IL 0x009c～0x00b5）。
- 文字是 Vanilla Textures Expanded 加的。它對 `DoButton` 有 transpiler，`DoButton.NoBlankText` 在按鈕夠寬（`VanillaTexturesExpandedUtility.CanDrawIconAndLabel(width, def)`）或它自己的設定 `mainButtonMode == 2` 時，會把空字串換回按鈕名稱。本模組的開發模式診斷也列出了這個補丁：`MainButtonWorker.DoButton: transpiler OskarPotocki.VanillaTexturesExpanded`。
- 影響：Vanilla Textures Expanded 是很常見的模組，有裝的人選「只顯示圖示」會沒有效果。
- 修法建議：本模組已經補丁了 `Widgets.ButtonTextSubtle`，可以在「只顯示圖示」而且按鈕有有效圖示時，把要畫的文字清成空字串，和「只顯示文字」時把圖示拿掉是同樣的做法。
- 還沒確認的：`CanDrawIconAndLabel` 的內容沒有反組譯，所以不知道 Vanilla Textures Expanded 的其他 `mainButtonMode` 會不會讓它不換字；這台電腦上它的設定也沒查。只在 Gene lab 主題下測過，Vanilla 主題沒測。

## 測試用排序

都在 `RimCrow\backups\other\`，check_order 都只剩完整清單本來就有的同名 DLL。

| 檔案 | 模組數 | 用途 |
|---|---|---|
| `ModsConfig_20261104_1231_mbbs_selftest_cleanlog.xml` | 23 | 主要測試（B7、主題、圖示模式、PNG 匯入、文字對齊）。拿掉了會把 log 洗滿的 Dubs Mint Minimap 和 Rules of Engagement |
| `ModsConfig_20261104_1309_mbbs_toomanybuttons.xml` | 24 | 上面那份＋TooManyButtons：相容橋接「本模組優先」、參考值和匯入 |
| `ModsConfig_20261104_1309_mbbs_custombottombar.xml` | 24 | 上面那份＋Custom Bottom Bar：同上 |
| `ModsConfig_20261104_1309_mbbs_buttons_max_noroe.xml` | 80 | checklist M 節：45 個主按鈕模組，沒有換圖示模組 |
| `ModsConfig_20261104_1309_mbbs_retexture_noroe.xml` | 84 | M 節：再加 Vanilla Textures Expanded、Bradson's、Tang's |
| `ModsConfig_20261104_1309_mbbs_retexture_swap_noroe.xml` | 84 | M 節：Bradson's 移到 Vanilla Textures Expanded 後面 |

M 節的預期結果在 `test/stage-4-results` 分支的 `docs/testing/checklist-stage-4.md`。

## 給雲端的摘要

```
第七輪實機測試（claude/round-7 @ 6165087，RimWorld 1.7.4998）結果在 test/round-7-results 的 docs/testing/results-round-7.md，截圖在 docs/testing/screenshots/round7_*.webp。

通過：
- B7 切換語言：英→日→英、英→繁中→英（後者在 TooManyButtons「本模組優先」下）。清單、右側標題、原名都換成新語言；Play data reloaded 每次只印 1 行；自訂名稱保留，清掉後顯示新語言原名；齒輪、主題貼圖、遊戲內圖示、匯入 PNG 都正常；本模組優先時切語言後仍由本模組畫列。
- 開發模式 StaticConstructorOnStartup 警告：0 筆（上一輪 2 筆）。
- 主題 Gene lab／Void／Starfarer：配色和 Biotech／Anomaly／Odyssey 預覽圖一致，三款文字都看得清楚。Void 平常只有 1px 細紅線、沒有紅光，符合規格。
- 模組選單：新的 Preview.png 和英文說明都正常顯示。
- TooManyButtons：匯入會套用對方排序並清掉抽屜勾選；改成本模組優先要重開，重開後生效。Custom Bottom Bar：參考值有顯示，匯入會帶入寬度 120、間距 0、填滿螢幕。
- 整條列設定、匯入 PNG、文字置中／靠右都正常。本模組在 4 次開遊戲的 log 裡都是 0 ERROR、0 WARN。

要修：
1. 「只顯示圖示」在裝了 Vanilla Textures Expanded 時沒效果：按鈕夠寬就會同時顯示文字。原因是 BarAppearanceDef.Draw 對 IconMode.Icon 不做處理，靠原版 DoButton 在有圖示時傳空字串給 ButtonTextSubtle；但 Vanilla Textures Expanded 的 DoButton transpiler（DoButton.NoBlankText）在 CanDrawIconAndLabel(width) 或它自己的 mainButtonMode==2 時，會把空字串換回名稱。建議在本模組已有的 ButtonTextSubtle 補丁裡，於「只顯示圖示」且有圖示時把文字清空。
2.（建議）Void 滑鼠移上去的效果偏弱：下方紅線只從 1px 變 2px、多一點暈，正常距離幾乎看不出來；規格寫「發光＋紅色底線」，可以加強。

未測：停用狀態（沒有能停用的按鈕）、文字靠左、Custom Bottom Bar 的優先切換、checklist M 節（換圖示排序）。

不用改：官方日文全用半形「,」「.」（Core＋5 DLC 共 1,537 檔，「、」「。」0 個）。
工作坊簡中可以順手改：「外观分页」→「外观标签页」、「找不到类别」→「找不到类」。
Preview.png 已用素材重做（1280×720，參數在結果檔第 5 節），已推在 test/round-7-results。
```
