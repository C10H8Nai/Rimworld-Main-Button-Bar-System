# 第十二輪修正 實機測試結果

- 分支：`claude/round-12-fixes` @ `e890556`（已合併 main `0f8e673`，含 #17 萘伊像素主題）
- 測試日期：2026-11-06（Claude 用 computer use 操作，本機）
- 遊戲：RimWorld **1.7.5000 rev1275**（之前幾輪是 4998），開發模式開，2560×1440 全螢幕，介面簡中
- DLL：本機 SDK 10.0.403 編的版本（第一次進遊戲）。log 有 `[Main Button Bar System] Loaded.`，`TypeLoadException` 0 筆
- 測試前改過的設定：D 節照第十輪做法，把 `alwaysDrawBar` 改成 False、範本 `MainButtonBarSystem_Template.xml` 暫時移開（`alwaysDrawBar` 開著時本模組沒設定也會畫列，見 D 節）。測完都從備份放回，雜湊相同

## 總結

| 節 | 結果 | 說明 |
|---|---|---|
| 載入 | ✅ | 本機編的 DLL 正常載入，0 個 `TypeLoadException` |
| A. TooManyButtons 順序 | ✅ | 同一次啟動的第一局、第二局，兩對 `order` 相同的按鈕順序一樣（以前第二局會對調） |
| B. 來源模組名稱 | ✅ | 簡中 → 英文 → 日文 → 簡中，每次都是當下語言的名稱 |
| C. 匯入報告 | ✅ | 「主题：萘伊」下面多一行「上面还有自定义主题（1 个按钮有自己的样式）」 |
| D. UI Rethemed - Main Bar | ✅ | 三種黃字都對；「关闭「Main bar」」按下去不用重開，右下角馬上回來，對方設定檔 `mainButtons` 變 False |
| E. 日文 | ✅ | 底部兩顆按鈕完整（字變小）；預覽狀態名稱、「アイコンの位置」說明完整。⚠️ 另外看到「ボタンの背景」的「マウスオーバー: テー…」被截斷（新發現） |
| F. 平分時太窄的按鈕 | ✅ | 窄按鈕只畫背景；「更多 (3)」和選單按鈕完整；清掉單顆寬度回到平均分配 |
| G. 改圖示位置時保留文字 | ✅ | 建築規劃仍是文字＋圖示（圖示到文字右邊），原本只有圖示的仍只有圖示；畫面說明有寫預覽照遊戲本身的畫法 |
| H. log | ✅ | 本模組 0 紅字。黃字只有開發模式的主題提示（見 H 節），都是設計上會出的 |
| I. 主題按鈕底圖指到沒載入的按鈕 | ✅ | 3 顆不存在的按鈕只一條黃字；按「重新加载主题贴图」兩次、切三次語言都沒有再多 |

## 實機測試的環境

| 用途 | 排序 | 模組數 |
|---|---|---|
| A～C、E～I、theme-addons F／H | `ModsConfig_20261106_1039_mbbs_r12fix_main.xml`：第十二輪的 `r12_base`（perf84＋TooManyButtons＋SoftRace 範例）加 ExampleTheme、I 節測試主題 | 82 |
| D1～D3 | `ModsConfig_20261105_1101_mbbs_r10_mbbs_first.xml`：Harmony、本體、5 個 DLC、本模組、UI Rethemed | 9 |
| D4 | `ModsConfig_20261105_1101_mbbs_r10_lfs_first.xml`：同上，UI Rethemed 在前 | 9 |

- check_order：`r12fix_main` 只剩已知的 `0multiplayerapi.dll` 同名 DLL。每份都用 `modsconfig_check.py` 確認 RimCrow 匯入後 `same order: True`。
- 測試用模組（測完都從 `Mods\` 移走）：
  - `MBBS_Example_SoftRace`：repo `examples/` 原樣複製（`r12_base` 有它）。
  - `ExampleTheme`：repo `docs/examples/ExampleTheme` 原樣複製，theme-addons H 節在 Mods 那份上改 Def、改檔名，測完從 repo 原檔還原。
  - `MBBS_TestTheme_I`（`local.mbbstest.themei`）：I 節用，只有一個 `BarAppearanceDef`，`buttonImages` 列 3 顆不存在的按鈕。原檔在本機測試資料夾，不進 repo。
- 存檔：讀第十、十二輪留下的 `MBBS_r12_imperial`、`MBBS_r12_nai`、`MBBS_r12_custom`、`MBBS_r10_G`，都沒有存回去。
- log：本機測試資料夾 `round-12-fixes\logs\S1`～`S8`（每次啟動一份）。

## A. TooManyButtons 順序 ✅

| 步驟 | 結果 |
|---|---|
| A1 讀 `MBBS_r12_imperial`（這次啟動的第一局） | Equal Milking → 求醫者、经商管理 → 悬赏令（`r12fix_a1_game1_bar.png`） |
| A2 從遊戲內「载入」讀 `MBBS_r12_nai`（第二局） | 兩對順序和 A1 一樣，沒有對調（`r12fix_a2_game2_bar.png`）。第十二輪同樣的步驟第二局會對調 |

## B. 來源模組名稱 ✅

| 步驟 | 結果 |
|---|---|
| B1 簡中，遊戲中設定視窗 | 「来源模组」欄：核心、生物技术（`r12fix_b1_zhcn_source.png`） |
| B2 主選單切英文，從 Options → Mod options 開範本設定 | Core、Biotech（`r12fix_b2_english_source.png`） |
| B2 再切日文 | Core、Biotech（`r12fix_b2_japanese_source.png`）。原版日文翻譯本來就把 ExpansionDef 名稱留英文（`Data\Core\Languages\Japanese` 的 `ExpansionDefs.xml`：`<Core.label>Core</Core.label>`、`<Biotech.label>Biotech</Biotech.label>`），所以這是日文的正確名稱 |
| B3 再切回簡中（多做的，用來分辨「停在上一個語言」） | 核心、生物技术（`r12fix_b3_back_to_zhcn.png`），沒有停在 Core |

## C. 匯入報告 ✅

把第十二輪的 `r12_custom_file.xml` 暫時放進 `Exports`，在 `MBBS_r12_nai` 的設定視窗按「导入…」：報告是「主题：萘伊」，下一行「上面还有自定义主题（1 个按钮有自己的样式）。」（`r12fix_c1_import_report.png`）。按取消，沒有真的匯入。測完匯出檔已移走。

## D. UI Rethemed - Main Bar ✅

UI Rethemed 的設定檔一開始沒有寫 `mainButtons`（＝預設開）。

| 步驟 | 結果 |
|---|---|
| （先做的）`alwaysDrawBar` True、範本在 | log：`mainButtons = True; bar drawn by this mod.`，右下角時間控制不見。不是 D1 要的狀態，所以照第十輪改 `alwaysDrawBar` False、範本移開再測 |
| D1 `mbbs_first`、Main bar 開、讀 `MBBS_r10_G` | log：`bar drawn by the game or a later mod (this mod left it: its settings are untouched)`，畫面是 UI Rethemed 的列。設定視窗黃字：「本模组还没有任何设置，所以底部栏交给 UI Rethemed - Main Bar 绘制。要改用本模组，在这里改任意一项设置即可；改了之后也要关闭 UI Rethemed - Main Bar 的「Main bar」，否则右下角的时间控件、日期和天气会消失。」，沒有按鈕（`r12fix_d1_notice.png`） |
| D2 同一局，外觀選帝國 | 黃字換成「UI Rethemed - Main Bar 的「Main bar」也开着。本模组绘制底部栏时，右下角的时间控件、日期和天气会消失，部分窗口的位置也会偏移。请关闭 UI Rethemed - Main Bar 的「Main bar」。」，右邊有「关闭「Main bar」」（`r12fix_d2_notice_button.png`）。底部列變帝國，右下角的時間控制確實不見（`r12fix_d2_bottomright_missing.webp`） |
| D3 按「关闭「Main bar」」 | 左上角訊息「已修改 UI Rethemed - Main Bar 的设置，并保存到它的模组设置。」，黃字消失，右下角時間控制、日期、溫度、天氣馬上回來（`r12fix_d3_after_button.webp`、`r12fix_d3_bottomright_back.webp`）。設定檔 `Mod_3826865706_UIRethemedMainBarMod.xml` 第 4 行 `<mainButtons>False</mainButtons>` |
| D4 設定檔放回備份、換 `lfs_first`、讀 `MBBS_r10_G` | 黃字「UI Rethemed - Main Bar 在模组列表中排在本模组前面，正在绘制底部栏，这里的设置不会生效。要改用本模组，请关闭 UI Rethemed - Main Bar 的「Main bar」，或在模组列表中把本模组排在它前面。」，旁邊也有按鈕（`r12fix_d4_lfs_first_notice.png`）。沒按 |
| D5 | UI Rethemed 設定檔從備份放回，雜湊相同 |

## E. 日文 ✅（另有一處新的截斷）

| 步驟 | 結果 |
|---|---|
| E1 遊戲中設定視窗底部 | 「デフォルトテンプレートとして保存」「デフォルトテンプレートを適用」完整，字變小（`r12fix_e1_japanese_bottom_buttons.png`） |
| E2 自訂主題 | 預覽下「通常／マウスオーバー／開いているとき／無効」完整（`r12fix_e2_japanese_states.png`）；「アイコンの位置」說明三行完整（`r12fix_e2_japanese_icons.png`） |
| ⚠️ 新發現 | 同一頁「ボタンの背景」那幾列，「マウスオーバー: テーマに従う」被截成「マウスオーバー: テー…」（同一張截圖下緣）。這輪沒修這裡 |

## F. 平分時太窄的按鈕 ✅

`MBBS_r12_nai`、摺疊、填滿畫面，建築規劃、工作T、勤工分配、醫療、方案都設 400：

| 步驟 | 結果 |
|---|---|
| F1 | 其他按鈕擠成細條，只畫背景（萘伊黑板的邊框），沒有圖示疊在一起；「更多 (3)」整個字看得到；選單按鈕的圖完整（`r12fix_f1_5x400_bar.png`、`r12fix_f1_5x400_right_zoom.png`） |
| F2 | 「整条栏」按「让 5 个有自己宽度的按钮都改为跟随整条栏」：回到平均分配（`r12fix_f2_cleared_bar.png`） |

設定都按取消，沒有存。

## G. 改圖示位置時保留文字 ✅

`MBBS_r12_nai`（有 VTE），自訂主題「所有按钮」只把圖示位置改成右中：

| 步驟 | 結果 |
|---|---|
| G1 | 建築規劃、方案仍是文字＋圖示，圖示到文字右邊；工作、醫療、研究等原本只有圖示的仍只有圖示，在按鈕右中（`r12fix_g1_icon_right_bar.png`） |
| G2 | 「图标」段說明：「改了图标大小或位置的按钮，图标改由本模组按这里的设置绘制；是否显示文字仍取决于游戏和 Vanilla Textures Expanded 这类模组。预览总是按游戏本身的方式绘制。」（`r12fix_g2_icon_note.png`） |

## H. log ✅

`log_scan.py` 掃 S1～S8：本模組 0 個 ERROR。WARN 全部是預期的：

- `Theme MBBS_Nai …, buttonImages: 23 picture(s) name main buttons that are not loaded …`（82 個模組時；9 個模組時是 63）：萘伊主題替完整清單的按鈕準備了圖，開發模式提示一條。是 I 節的設計（每款主題一條）。
- `Theme MBBSTest_ThemeI …: 3 picture(s) …`：I 節測試主題。
- `Theme ExampleTheme_Steel …`：theme-addons H7、H13 故意製造的。
- `Mod MBBS test theme (round-12-fixes I) dependency … needs to have <downloadUrl>`：測試模組的 About 沒寫下載網址，不是本模組。

## I. 主題的按鈕底圖指到沒載入的按鈕 ✅

| 步驟 | 結果 |
|---|---|
| I1 | 啟動時一條：`[Main Button Bar System] Theme MBBSTest_ThemeI (MBBS test theme (round-12-fixes I)), buttonImages: 3 picture(s) name main buttons that are not loaded (a typo, or a mod that is not active), so they are unused: MBBSTest_NoSuchButton1, MBBSTest_NoSuchButton2, MBBSTest_NoSuchButton3` |
| I2 | 主選單範本設定 → 外观 → 「重新加载主题贴图」按兩次：左上角「主题贴图已重新加载：重新读取了 126 个文件。」（`r12fix_i2_reload_message.webp`），那條黃字仍只有 1 條。中間切了三次語言，也沒有多 |

## 其他發現（不在這輪清單內）

1. **文字靠左＋VTE 的「圖示＋文字」按鈕：文字和圖示疊在一起**。`MBBS_r12_imperial`、`MBBS_r12_nai` 兩個存檔的整條欄都設「靠左」，建築規劃的字從按鈕左緣開始畫，VTE 的房子圖示疊在「筑」上（帝國、萘伊都一樣，`r12fix_a1_game1_bar.png`、`r12fix_g1_icon_right_bar.png` 左邊）。不是這輪改的東西，推定 round-8「文字靠左」以來就這樣，沒查證。
2. **遊戲版本 1.7.5000 rev1275**：本模組照常運作。
3. **切過語言後，殖民者和頭像變成洋紅色方塊**（貼圖遺失）。讀檔、換語言幾次後出現，重開遊戲就正常。和本模組無關（推定是 1.7.5000 換語言重載時其他模組的貼圖），列出來供參考。
4. 主選單的「重新加载主题贴图」滑鼠提示，簡中的換行位置怪（「…重新\n读取\nPNG…」），只是外觀。

## 測完的還原

- `Config` 的 `Prefs.xml`、`KeyPrefs.xml`、本模組設定、範本、UI Rethemed 設定都從備份放回，雜湊相同。
- `Mods\ExampleTheme`、`Mods\MBBS_Example_SoftRace`、`Mods\MBBS_TestTheme_I` 已移走；`Exports\r12_custom_file.xml` 已移走。
- ModsConfig 停在 `lfs_first`（9 個），使用者自己換回完整清單。
- 存檔都沒有覆寫。遊戲的自動存檔間隔是 0.125 天，測試局都只玩了幾秒到幾十秒（遊戲內 1 小時內），本機的 `Autosave-*` 時間戳仍是 11-06 01:21 之前的（測試前看到的），沒被蓋。
