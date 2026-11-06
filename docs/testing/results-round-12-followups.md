# 實機測試後續（PR #19）實機測試結果

- 分支：`claude/round-12-followups` @ `ca34c4f`（PR #19）
- 測試日期：2026-11-06（Claude 用 computer use 操作，本機）
- 遊戲：RimWorld 1.7.5000 rev1275，開發模式開，2560×1440 全螢幕，介面簡中（第 1 項切日文）
- 部署：`deploy.ps1 -Mod MainButtonBarSystem`，`DEPLOYED_FROM.txt` 是 `claude/round-12-followups` @ `ca34c4f`，DLL 雜湊和 repo 相同
- 這輪和 Lost Forest 1.7 本地移植（`local.lostforest17`）一起測，同一份排序；Lost Forest 的結果另外記在它自己的工作區
- 測試前改過的設定：無（`alwaysDrawBar` 維持 True）。`Mods\ExampleTheme` 是從 repo `docs/examples/ExampleTheme` 複製，再照第 3 項拿掉 `<button>`、`<buttonMouseover>`、`<buttonActive>`、`<buttonDisabled>` 和三個文字顏色

## 總結

| 項目 | 結果 | 說明 |
|---|---|---|
| 1. 日文「ボタンの背景」 | ✅ | 四列都完整；放不下的「マウスオーバー」「開いているとき」換到第二行，縮圖和按鈕在第一行 |
| 2. 文字對齊讓開 VTE 圖示 | ✅ | 靠左時字從 VTE 圖示右邊開始，不再疊在圖示上；置中、靠右也不壓到圖示；沒有 VTE 圖示的按鈕、`<` 捲動箭頭、「更多」位置不變 |
| 3. 只有按鈕圖的主題提醒 | ✅ | 開發模式主選單一條 `Theme ExampleTheme_Steel (…), button: the theme has button pictures but no button atlas, …` 黃字，沒有紅字 |
| 4. `theme-addons.md` | — | 文件修改，合併後就算完成，不用實機 |
| 和 Lost Forest 一起 | ✅ | Lost Forest 的 gizmo（Dragonia 護盾、Glacia 能力、提燈開關）在萘伊主題下顯示正常；兩邊的 Harmony 補丁目標沒有重疊 |
| log | ✅ | 5 份 log 本模組都是 0 紅字；黃字只有兩條開發模式主題提示（見 log 節） |

## 實機測試的環境

| 用途 | 排序 | 模組數 |
|---|---|---|
| 第 1～3 項、和 Lost Forest 一起（S1～S5） | `ModsConfig_20261106_1503_lf17_mbbs_r12followups_test.xml`：Lost Forest 測試排序（Harmony、本體、5 個 DLC、【ZP】Rice、優化／讀取優化模組和漢化、Lost Forest 1.7）加本模組、ExampleTheme、Vanilla Textures Expanded（含前置 Vanilla Expanded Framework）和兩個漢化 | 42 |
| Lost Forest 種族（S6） | `ModsConfig_20261106_1633_lf17_mbbs_r12followups_spawnbypass_test.xml`：上一份再加 Lost Forest 的測試用小模組 `local.lf17devspawnbypass`（開發模式下讓任務限定種族可以用開發工具生成，不在 repo、測完已從 `Mods\` 移走） | 43 |

- check_order：兩份都 0 問題。xml_check：Lost Forest、本模組、ExampleTheme、VTE、測試用小模組 0 問題。RimCrow 匯入後 `modsconfig_check.py` 都是 `same order: True`。
- 存檔：Lost Forest 測試用的 `LF17_r1_fos`、`LF17_r1_fos2`（`Saves` 根目錄）。沒有讀寫使用者的存檔。
- log：本機測試資料夾 `round-12-followups\logs\`，`S1_mainmenu`、`S2_lf_forced_quest`、`S4_ja_load`、`S5_zh_mainmenu`、`S6_lf_spawnbypass`。S3（15:32～15:59，第 2 項和 Lost Forest gizmo 那一段）的 log 沒留住，原因見「其他發現」；那段期間每次查都只有下面說的 4 筆 VEF 紅字。

## 1. 日文「ボタンの背景」 ✅

主選單「選項 → 語言」切日文（遊戲會重開），讀 `LF17_r1_fos2`，遊戲中「オプション → MODオプション → Main Button Bar System → 主按鈕列設定 → 外観 → カスタム → すべてのボタン」，捲到「ボタンの背景」。

| 列 | 結果 |
|---|---|
| 通常 | `通常: テーマに従う`，一行 |
| マウスオーバー | `マウスオーバー:` ／ `テーマに従う` 兩行，縮圖、「選択...」「デフォルト」在第一行 |
| 開いているとき | `開いているとき:` ／ `テーマに従う` 兩行，同上 |
| 無効 | `無効: テーマに従う`，一行 |

截圖 `r12f_1_ja_button_bg.png`。按「中止」關掉，沒存。

## 2. 文字對齊讓開 VTE 圖示 ✅

萘伊主題、摺疊＋捲動、按鈕預設寬度 120 px。VTE 只有按鈕夠寬時才畫「圖示＋文字」，所以在設定視窗把建築規劃、方案的單顆寬度設成 400（照上一輪做法），再切整條欄的「文字對齐」。預覽是即時的；測完按「取消」，設定沒有存。

| 對齊 | 結果 |
|---|---|
| 置中（原本的設定） | 「建筑规划」「方案」在 VTE 圖示右邊剩下的寬度裡置中（`r12f_2_center_bar.png`） |
| 靠左 | 字從 VTE 房子圖示右邊開始，「筑」不再疊在圖示上（`r12f_2_left_bar.png`、放大 `r12f_2_left_zoom.png`）。上一輪的問題是字從按鈕左緣開始畫 |
| 靠右 | 字貼右邊，不壓到圖示（`r12f_2_right_bar.png`、放大 `r12f_2_right_zoom.png`） |

靠左和靠右兩張整條欄截圖逐像素比對：差異只在兩顆寬按鈕的文字範圍（x 150～1322），其他只有圖示的按鈕、左邊的 `<` 捲動箭頭、右邊的「更多 (3)」完全一樣。

## 3. 只有按鈕圖的主題提醒 ✅

ExampleTheme 拿掉按鈕框四行和三個文字顏色（見開頭），開發模式。每次啟動主選單都有這一條，沒有紅字：

```
WARN [Verse] [Main Button Bar System] Theme ExampleTheme_Steel (Example Theme for Main Button Bar System), button: the theme has button pictures but no button atlas, so the game's opaque button background covers them and they do not show; add a button atlas with a transparent middle (make_theme.py: "hollow": true or "frame": "vanilla" in the palette)
```

這輪沒有在遊戲裡選 ExampleTheme 看畫面（清單只要求黃字）。

## 和 Lost Forest 一起 ✅

| 步驟 | 結果 |
|---|---|
| Glacia（馴服後）選取 | 「撤退命令」「肾上腺素爆发」「紧急处理」顯示正常，底部列是萘伊主題（`r12f_3_lf_glacia_gizmo_nai.webp`） |
| Doug 穿 Lost Forest 提燈 | 「熄灯」gizmo 顯示燃料 5.0／5.0；按下變成點燈、顯示「剩余 2.5 天」（`r12f_3_lf_lantern_gizmo_nai.webp`） |
| Dragonia（馴服後）選取 | 護盾 gizmo「Drarr ward 100 / 100」只有一條，另外是放生、撤退命令（`r12f_3_lf_dragonia_gizmo_nai.png`）。「Drarr ward」是 Lost Forest 原作寫死的英文，和本模組無關 |
| Harmony 補丁 | 靜態比對兩邊的 `[HarmonyPatch]` 目標：本模組只補 `MainButtonsRoot`、`Widgets.ButtonTextSubtle`、`Widgets.DrawAtlas`、`CameraDriver.CameraDriverOnGUI`、`PlayDataLoader.LoadAllPlayData`；Lost Forest 補的是遊戲邏輯、渲染和主選單的 DLC 圖示，沒有重疊。本模組 dev 模式印出的主按鈕方法補丁清單裡也沒有 Lost Forest |

## log

`log_scan.py` 掃 5 份：每份本模組 0 紅字、`Loaded.` 1 次、`Could not apply`／`was not found` 0。黃字每份都只有兩條，都是開發模式設計上會出的：

- `Theme MBBS_Nai …, buttonImages: 61 picture(s) name main buttons that are not loaded …`：萘伊主題替完整清單的 77 顆按鈕準備了圖，這份小排序沒有那些模組。
- 第 3 項的 ExampleTheme 提醒。

整份 log 的紅字只有 4 筆，每次啟動都一樣，是 Vanilla Expanded Framework 讀到舊設定檔類別名稱（`VFECore.VFEGlobalSettings`、`HeavyWeapons.HeavyWeaponsSettings`、`AnimalBehaviours.*`），它會自己改用新類別，和本模組無關。S2 另外有 Lost Forest 強制觸發任務造成的大量紅字，也和本模組無關（記在 Lost Forest 那邊）。

## 其他發現（不在這輪清單裡）

- **切語言會重開遊戲並覆蓋 `Player.log`**：1.7.5000 在主選單切語言時，遊戲以 `-WaitForExitingPid=…` 重開，新程序直接覆蓋 `Player.log`，沒有 `Player-prev.log`。S3 的 log 因此沒留住。之後要切語言前，先把 `Player.log` 複製出來（已補進標準測試流程）。
- 開發工具「動作」選單的快捷鍵是 `/`（`Dev_ToggleDebugActionsMenu`）。用滑鼠點頂部第 4 個圖示時，游標停在螢幕上緣會觸發鏡頭邊緣捲動，用 `/` 就不會（已補進標準測試流程）。

## 還原

- Config：測試前備份在本機測試資料夾 `round-12-followups\config_backup\`（含 SHA256SUMS）。測完比對：`Prefs.xml`、`KeyPrefs.xml`、本模組設定、範本、VTE 設定都和備份相同；Lost Forest 新產生的 `Mod_Local_LostForest17_LostForestMod.xml` 已刪掉（副本在 `config_after_test\`）。
- `Mods\ExampleTheme` 恢復成測試前的空資料夾；`Mods\Local_LF17DevSpawnBypass` 已移走。
- `Mods\MainButtonBarSystem` 留在 `ca34c4f`（PR #19）。
- ModsConfig 停在 `spawnbypass_test`（43 個，其中測試用小模組已移走），由使用者換回完整清單。
