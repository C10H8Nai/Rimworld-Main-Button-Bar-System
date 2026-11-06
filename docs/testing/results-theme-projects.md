# 主題專案（新增／刪除主題）實機測試結果

- 分支：`claude/theme-projects`。冒煙測試用 `da28ffa`，完整測試用 `feddba7`（C.1 以後是 `6f32bb7`），都用 `deploy.ps1` 部署（repo 裡的 DLL）。
- 清單：`docs/testing/theme-projects.md`
- 環境：RimWorld 1.7.5000 rev1275（Waylith），開發模式開，2560×1440，介面簡體中文（語言測試切日文、愛沙尼亞文、丹麥文、挪威文、瑞典文）。
- 測試日期：2026-11-07 02:54～03:53（`claude/theme-projects`），04:06～04:33 補測（部署 `claude/release-1.0` @ `e71aadf`，包含本分支全部），Claude 用 computer use 操作。
- 測試前的設定和自動存檔備份在本機 `_資料與測試\MainButtonBarSystem\theme-projects\config_backup\`（有 SHA256SUMS），測完還原。

## 總結

| 節 | 結果 | 說明 |
|---|---|---|
| A 新增 | ✅ | 全部通過（A.5、A.6 在補測） |
| B 空白主題的圖 | ✅ | 四種狀態、裁切框、更多、抽屜都分得出；B.4 列背景有畫（補測） |
| C 未啟用的由本模組載入 | ✅ | C.7 測出換語言時一筆紅字，已修（`b3051f1`、`5ca7e61`）並重測通過；C.5 用未啟用的專案重測通過；C.3 在 1.7 走不到（遊戲會強制重開） |
| D 重新載入與自動偵測 | ✅ | D.4 第一次測出 Waylith `LoadImage` 會丟例外，已修（`6f32bb7`）並重測通過；D.8 補測 |
| E 刪除 | ✅ | 跨磁碟刪除（Mods 在 D:、存檔資料夾在 C:）成功；檔案被鎖時什麼都不改 |
| F 不打擾只整理按鈕的玩家 | ✅ | 冒煙測試時還沒有任何專案：只多「新增主題…」，log 沒有新訊息 |
| G 翻譯 | ✅ | 日文、簡中；愛沙尼亞文、丹麥文、挪威文、瑞典文順便看過 |

本模組 0 紅字。黃字只有清單寫明會出現的幾種、開發模式的萘伊主題診斷（53 張插圖對到沒載入的按鈕），和 F.6 留下的 `TP v99` 略過訊息。

## 環境

- 排序：`RimCrow\backups\other\ModsConfig_20261107_0248_mbbs_tp_main.xml`（50 個：第十三輪 `r13_main` 拿掉測試用丟例外模組）；C.2 用 `ModsConfig_20261107_0335_mbbs_tp_active.xml`（再加 `Test Steel`，51 個）。check_order 只有已知同名 DLL；匯入後 `same order: True`。
- 建立的專案：`Mods\Test Steel`、`Mods\Test Steel (2)`（E.1 刪除），C.4 的複本 `Test Steel Copy`。測完都搬到本機 `_資料與測試\MainButtonBarSystem\theme-projects\created_projects\`。
- 存檔：`MBBS_tp_select`（C.1、C.6）、`MBBS_tp_f7`（第十三輪 F.7）。
- log：本機 `theme-projects\logs\`（S1 冒煙、S2 主要、S3～S7 各語言、S8 C.6、S9 C.2、S10 C.5／F.7）。03:08～03:14 那次（A.3～B、D、E）的 log 沒留下，結果照當下讀 log 的紀錄。

## 細節

### A. 新增

| 項目 | 結果 | 截圖 |
|---|---|---|
| A.1 | 清單最後（自訂主題之後）有「新增主題…」 | — |
| A.2 | 名稱空白「建立」灰、提示要名稱；packageId `abc`、`NAI.MainButtonBarSystem` 各自灰並說明原因 | tp_a2_dialog.png |
| A.3 | `Mods\Test Steel\` 檔案齊全；About 前置項有 `downloadUrl`；卡片被選上、來源「主題專案（未啟用）」；不用重開。說明留空時 Def 沒有 `<description>`，log 沒有 `empty description` | tp_a3_created.png |
| A.4 | 同名再建：`Nai.mbbstheme.TestSteel2`、資料夾 `Test Steel (2)` | — |
| A.5 | 補測：在 `Mods\` 放一個叫 `Test Iron` 的**檔案**，再建立名稱 `Test Iron` 的主題：訊息「无法在 …\Mods\Test Iron 创建主题：Cannot create '…\Test Iron' because a file or directory with the same name already exists.」；沒有新資料夾、沒有新卡片，那個檔案原封不動，log 0 紅字 | tp2_a5_blocked.png |
| A.6 | 補測：自訂主題「背景插圖」的「選擇…」→「主題的圖」最後一排是 Test Steel 的 N／H／A／X 四張按鈕底圖和 ButtonPicture（滑鼠提示「Test Steel, normal 64×64」） | tp2_a6_picker.png |

冒煙測試（`da28ffa`）找到兩件事，已在 `feddba7` 修：說明留空時遊戲的 `Def.ConfigErrors` 報 `empty description` 並越界（S1 log 兩行黃字）；主選單沒有底部列、卡片用靜態 `preview` 圖，改圖看不到 → 未啟用專案的卡片改成一律用貼圖即時畫。

### B. 空白主題的圖

進 Dev 快速測試地圖：一般藍 N、開啟中橘 A、滑鼠下綠 H；插圖的裁切框和準心看得到；「更多」紫色；抽屜深紫、齒輪空心。停用狀態（灰 X）在卡片預覽看得到。`_Guide.png` 在 tasks 2.2 目視過。

截圖：tp_b1_bar_left.png、tp_b1_bar_right.png、tp_b3_drawer.png

B.4（補測）：按鈕幾乎把列蓋滿（只顯示圖示），列背景只在「更多」底下看得出來——那裡透出 `Bar.png` 上的字「256x36 Tile」，表示背景有畫、照 Tile 鋪；接縫的黃色記號被按鈕框蓋住，看不到。截圖 tp2_b4_bar.png。

### C. 未啟用的主題專案由本模組載入

| 項目 | 結果 | 截圖 |
|---|---|---|
| C.1 | 存 `MBBS_tp_select`、重開遊戲讀檔：外觀還是 Test Steel，卡片來源「主題專案（未啟用）」，沒有本模組紅字 | tp_c1_loaded.png |
| C.2 | 在 RimCrow 啟用 `Test Steel` 重開：清單只有一張 Test Steel，來源「Nai (Test Steel)」（模組名稱）；log 沒有 `Adding duplicate`、`short hash`；「刪除」灰、提示「這是已啟用的模組，請先在模組清單停用並重開遊戲」；「重新載入」只讀 PNG（訊息「12 張圖片已重新讀取。Defs 的修改要重開遊戲」） | tp_C2_active_card.png |
| C.3 | **1.7 走不到這個狀態**（補測）：在遊戲的模組清單啟用 Test Steel、按「Save and apply changes」，`ModsConfig.xml` 立刻寫入，但遊戲跳出「The mod list has changed. The game will now restart automatically.」，只有 OK、Esc 關不掉。按 OK 重開後就是 C.2 的狀態（一張卡、來源是模組名稱、刪除灰、log 沒有 `Adding duplicate`）。`ModsConfig` 勾選但還沒重開的判斷只有單元測試 | — |
| C.4 | 複製成 `Test Steel Copy` 重開：清單一張；本模組一行黃字 `Theme project Test Steel Copy: the theme NaiTestSteel_Theme is skipped: another theme already has that defName …`，沒有 `Adding duplicate`。遊戲本身的模組清單會因為重複的 packageId 出紅字（遊戲行為，和本模組無關） | — |
| C.5 | 第一次（03:46）測錯了對象：用的是**已啟用**的 Test Steel，檔案不見時照設計沿用遊戲記憶體裡的貼圖，所以沒有黃字（訊息只寫「讀了 11 個」）。05:04 用**未啟用**的 Test Steel 重測（`claude/release-1.0` @ `621c03b`）：改名後 1～2 秒內自動偵測，log 一行黃字 `Theme NaiTestSteel_Theme (theme project Test Steel), barBackground: texture 'NaiTestSteel/Bar' not found (the game's look is used for it)`，卡片「更多」底下的列背景（`Bar.png 256x36` 字樣）消失；改回原名後背景回來、訊息「重新读取了 13 个文件」；0 紅字。清單已補註明要用未啟用的專案（`bf904c4`） | tp3_c5_card_before_after.png、tp3_c5_card_restored.png |
| C.6 | 存檔選著 Test Steel、資料夾搬走後讀檔：外觀退回原版，本模組沒有紅字（紅字都是遊戲瑞典文翻譯的） | tp_c6_gone.png |
| C.7 | 一開始換語言都會重開程序（帶 `-WaitForExitingPid`），查到是測試排序裡的 **WOWGAG's GUI Performance Patch** 把 `LanguageDatabase.SelectLanguage` 換成重開（開發模式直接 `GenCommandLine.Restart()`）。原版是程序內 `ClearAllPlayData`＋`LoadAllPlayData`。改用拿掉它的排序（`ModsConfig_20261107_0441_mbbs_c7_nogpp.xml`，49 個）重測：程序內重新載入、log 有 `Play data reloaded`，但**一筆紅字 `NaiTestSteel_Theme already has short hash.`**。原因：1.7 的 `ClearAllPlayData` 在卸下模組後才找 Def 型別，本模組的 `BarAppearanceDef` 資料庫沒被清掉，內建主題重新載入時被同名取代，主題專案的主題卻留著舊 shortHash，遊戲的 `GiveAllShortHashes` 就報錯。修正：`ClearAllPlayData` 前先把主題專案的主題移出資料庫（`b3051f1`，review 後 `5ca7e61` 加上例外防護）。重測（`5ca7e61`）：程序內重新載入、0 紅字，Test Steel 仍在清單、仍選著、卡片正常 | tp3_c7_after_reload.webp |

### D. 重新載入與自動偵測

| 項目 | 結果 | 截圖 |
|---|---|---|
| D.1 | 用程式在 `Button.png` 畫紅塊：1～2 秒內卡片的一般狀態出現紅塊，訊息「讀了 N 個檔案」 | tp_d1_red.png |
| D.2 | 文字變紅、建築規劃的插圖換成齒輪，其他按鈕照舊 | tp_d2_xml.png |
| D.3 | 多一款 → 出現「Test steel 2」卡片；拿掉 → 卡片消失 | tp_d3_added.png |
| D.4 | 第一次（`feddba7`）：Waylith 的 `Texture2D.LoadImage` 讀到殘缺 PNG 會丟 `StbTextureReadException`（Unity 是回傳 false），每輪重試都出一筆紅字。`6f32bb7` 改成接住後重試；重測：寫到一半的幾秒 0 紅字、按鈕維持舊圖，寫回後自動讀到 | — |
| D.5 | 一行黃字 `not valid XML yet`，主題維持上一版；修好後恢復 | — |
| D.6 | 不認得的欄位、錯的列舉／顏色、同欄兩次、不存在的 `workerClass` 各一行黃字，沒有紅字，列照常畫 | — |
| D.7 | 檔案總管開到 `Mods\Test Steel`（用 Shell API 確認路徑後關掉） | — |
| D.8 | 補測：地圖上打開設定、外觀分頁選著 Test Steel 閒置 30 秒，FPS 每 10 秒 169／169／170（打開前 168） | — |

### E. 只能刪除主題專案

| 項目 | 結果 | 截圖 |
|---|---|---|
| E.1 | 刪 `Test Steel (2)`：搬到 `…\MainButtonBarSystem\DeletedThemes\Test Steel (2)_<時間>`，`Mods\` 裡沒有了，卡片消失。Mods（D:）和存檔資料夾（C:）不同磁碟，走的是複製後刪除 | tp_e1_deleted.png |
| E.2 | 內建主題的卡片沒有「重新載入／打開資料夾／刪除」這列 | — |
| E.3 | 見 C.2；C.3 沒測 | — |
| E.4 | 用 PowerShell 獨占開著 `_Guide.png` 時刪除：訊息「無法移動…沒有做任何更改」，資料夾和清單都沒變，`Mods\` 沒有 `.mbbs-deleting`；關掉檔案後再刪成功 | tp_e4_locked.png |

### F、G

- F.1：冒煙測試開設定視窗時 `Mods\` 還沒有任何專案：外觀分頁只多「新增主題…」，log 沒有新的本模組訊息。
- G.1：日文的「新規テーマ…」按鈕、對話框、卡片下三顆按鈕、卡片來源都有翻譯，沒有超出（tp_g1_ja_dialog.png）。簡中是這次的主要介面。愛沙尼亞文、丹麥文、挪威文、瑞典文的結果寫在第十三輪補測的結果檔。

## 發現的問題

1. ~~重新載入時缺圖沒有黃字~~（C.5）：是第一次測錯對象（已啟用的專案），用未啟用的專案重測有黃字，不是問題。review 記下的「同一張圖第二次不見不會再警告」（WarningOnce）仍是 Minor。
2. 遊戲本身：同一個 packageId 的兩份資料夾（C.4 的複本）會讓遊戲的模組清單出紅字。文件已寫「複製專案要改 packageId 和 defName」。

## 補測（04:06～04:33，release-1.0）另外看到的

- 發佈 PR #24 的 `theme.xml`：存成自訂主題後 `meta` 是 `<buttons />`、`modVersion` 1.0.0，讀回正常。
- ⚠️ 發佈 PR #24 的萘伊說明：新加的「在「滚动」或「折叠」显示模式下、按钮宽度至少 120 px 时效果最佳。」在說明最後，**主題卡片只顯示兩行，這句被截掉看不到**，也沒有滑鼠提示能看全文（rel_nai_card.png）。

## 沒測／走不到

- C.3：1.7 在模組清單存檔後會強制重開（見上）。
