# 測試總表（0.7.0：第六輪＋第四階段，含之前沒測到的）

依據：`round-6.md`、`stage-4.md`，加上 `results-stage-1-3.md`、`results-stage-3-fixes.md`、`results-round-5.md` 裡標「未測」的項目。結果寫回 `results-stage-4.md`。

狀態：✅ 通過、❌ 不通過、⬜ 未測、➖ 不適用。截圖存成 `screenshots/stage4_<編號>.png`。

## 測試環境

- **快速測試排序**：`RimCrow/backups/other/ModsConfig_20261104_1026_mbbs_selftest.xml`（25 個，check_order 0 問題）。
  - 基本：Harmony、本體、5 個 DLC、本模組。
  - 圖示：Vanilla Expanded Framework、Vanilla Textures Expanded、Bradson's Main Button Icons (Forked + Expanded)。
  - 會新增或改主按鈕的模組：Hospital、Dubs Mint Menus、Dubs Mint Minimap、Dub's Paint Shop、Animal Tab、Medical Tab - Continued（M 撞鍵）、Stuff List (Continued)、Simple Warrants、Mech Work Tab、故人归来、Rules of Engagement（使用者要裝）。
  - 相容：BetterSliders Continued（含中文翻譯）、Colored Categories（也補丁 `DrawAtlas`）。
  - **注意 log 會被洗滿**：上次 18 個模組那份，Rules of Engagement 在地圖畫面右下角畫「顯示標記」切換鈕時，每一幀都觸發「null texture passed to GUI.DrawTexture」，幾分鐘就到 9,999 筆上限，之後遊戲**不再寫任何 log**。完整清單（2,201 個）那次沒有這個問題（0 筆）。
  - 11-04 12:26 用 25 個那份進地圖，13 秒就到上限。這次 92% 來自 Dubs Mint Minimap（小地圖畫小人、標記、視窗內容），Rules of Engagement 只佔 8%。原因還沒查到。
  - 要看 log 的話改用 `ModsConfig_20261104_1231_mbbs_selftest_cleanlog.xml`（23 個，拿掉這兩個）。
    - 它的程式（`Patch_PlaySettings_GuardMarkers.Postfix` → `StanceIcons.Get`）用 `ContentFinder` 讀 `UI/Stances/ASA_MarkerToggle`，讀不到就用原版的 `TexCommand.ForbidOff`。這張圖在它的模組裡確實存在。為什麼小排序下會變成「空貼圖」還沒查到，推定和 Waylith 載入貼圖的時機有關。
    - 所以用這份排序測試時，**一進地圖就先看一次 log**；本模組的紅字檢查以完整清單那次為準。
- 完整清單（2,201 個）載入要約 27 分鐘，只用來做最後的確認。
- 開發模式要開（「Patches on main button methods」診斷）。
- 測完要匯入 `ModsConfig_20261103_2245_full.xml` 還原平常的清單（如果之後在 RimCrow 改過清單，用 RimCrow 自己存的那份）。
- 測試排序不要讀主存檔，開新局測。

## A. 載入

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| A1 | 模組照常載入 | 看 Player.log | 有 `[Main Button Bar System] Loaded.`，本模組 0 筆 ERROR／WARN | ⬜ |
| A2 | 模組圖示 | 主選單 → 模組 | 本模組有小圖示（齒輪＋格子） | ⬜ |
| A3 | LoadFolders.xml | 看設定畫面和抽屜齒輪 | 設定畫面正常，齒輪有圖 | ⬜ |
| A4 | 補丁清單 | 開發模式，第一次畫主按鈕列後看 log | 「Patches on main button methods」：`DrawAtlas` 只有一組本模組的補丁（3 參數），Colored Categories 的另外一組 | ⬜ |
| A5 | DoButtons 改成 postfix 觀察 | 同上 | 診斷是第一次畫主按鈕列時寫入 | ⬜ |

## B. 設定視窗

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| B1 | 分頁 | 打開設定 | 上方只有「按鈕」「外觀」兩個分頁 | ✅（`stage4_02`） |
| B2 | 簡單／進階切換 | 按鈕頁左上角 | 看得出選了哪一個；切到進階、關掉重開，還是進階；換存檔也一樣 | ⬜ |
| B3 | 只從標題列拖動視窗 | 在清單、右側面板、底部按鈕之間按住拖 | 視窗不動；只有標題列能拖 | ⬜（上一輪通過，這版沒改） |
| B4 | 連續勾選 | 「抽屜」「顯示」欄按住往下拖 | 經過的格子變成同一個狀態；從 ≡ 拖才是排序 | ⬜（上一輪通過） |
| B5 | 「預設值」按鈕 | 看各項 | 已是預設值時半透明；改了才能按，按了回到預設 | ⬜ |
| B6 | 英文「Drawer」欄標題 | 遊戲語言切英文 | 完整顯示，和其他欄標題對齊 | ⬜ |
| B7 | 其他語言 | 切 2～3 種（例如日文、德文、俄文） | 文字是該語言、沒有被切掉、`{0}` 有換成數字 | ❌ 遊戲中切換語言後，按鈕清單的名稱停在切換前的語言，重開才正常（見結果第 5 節、`stage4_01`）。其他語言還沒切 |

## C. 滑桿和數字輸入（BetterSliders 啟用中）

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| C1 | 不被蓋住 | 進階 → 整條列的滑桿，滑鼠移上去 | 右端沒有 BetterSliders 的數字框 | ⬜ |
| C2 | 拖滑桿 | 拖動 | 右邊輸入框的數字跟著變 | ⬜ |
| C3 | 直接輸入 | 在輸入框打數字 | 滑桿跟著動；超出範圍會被限制 | ⬜ |
| C4 | 預設值 | 按「預設值」 | 滑桿和輸入框一起回到預設 | ⬜ |
| C5 | 其他滑桿不受影響 | 原版選項視窗的滑桿 | BetterSliders 照常有數字框 | ⬜ |

## D. 抽屜和齒輪

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| D1 | 齒輪 | 摺疊模式，收 1 顆以上進抽屜，打開「更多」 | 右下角是齒輪（本模組自帶的白色齒輪），有提示，點了打開設定 | ⬜ |
| D2 | 抽屜只有一欄 | 只收 1 顆 | 齒輪不會被切掉 | ⬜ |
| D3 | 抽屜開關 | 抽屜開著時再點「更多」 | 關掉，不會關了又開 | ⬜ |
| D4 | 點抽屜裡的按鈕 | 點 | 開對應視窗，抽屜關閉 | ⬜ |

## E. 圖示與文字

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| E1 | 只顯示文字 | 進階 → 建築規劃 →「只顯示文字」 | 列上變成文字 | ⬜ |
| E2 | 圖示＋文字 | 切「圖示＋文字」 | 左圖示右文字；滑鼠移上去兩者一起往右上移 | ⬜ |
| E3 | 只顯示圖示 | 選一顆沒有圖示的按鈕 →「只顯示圖示」 | 沒有圖示時的行為合理（退回文字或空白，記下實際結果） | ⬜ |
| E4 | 照遊戲原本的 | 切回 | 回到原樣 | ⬜ |
| E5 | 整條列一次設定 | 整條列「圖示與文字（所有按鈕）」→「只顯示文字」 | 全部變文字 | ⬜ |
| E6 | 個別設定優先 | 再把一顆改成「只顯示圖示」 | 只有它不一樣 | ⬜ |
| E7 | 全部改回 | 按「讓 N 顆…都改成跟這裡」 | 全部回到跟整條列 | ⬜ |
| E8 | VTE／Bradson 圖示 | 看列和清單的圖示欄 | 圖示照常，清單的圖示欄顯示實際會用的圖示 | ⬜ |
| E9 | 文字對齊 | 整條列文字對齊「靠左」 | 文字靠左；滑鼠移上去文字往右上移 2px（上一輪沒測） | ⬜ |
| E10 | 抽屜套用 | 打開「更多」 | 抽屜裡的按鈕也套用圖示設定和對齊 | ⬜ |

## F. 圖示選擇和匯入

測試用圖已放好：存檔資料夾 `MainButtonBarSystem/Icons/test_gold_gear_64.png`（64×64，金色齒輪）。

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| F1 | 內建圖示 | 沒有圖示的按鈕 →「選擇…」→「內建」選一個 | 列上出現那個圖示 | ⬜ |
| F2 | 預設值 | 按「預設值」 | 恢復原本 | ⬜ |
| F3 | 匯入的 | 「選擇…」→「匯入的」 | 看得到 `test_gold_gear_64.png`；選了之後列上是金色齒輪 | ⬜ |
| F4 | 打開資料夾、重新整理 | 按這兩顆 | 打開正確的資料夾；新放的檔案會出現 | ⬜ |
| F5 | 圖檔不見 | 刪掉 PNG，重開遊戲 | 按鈕退回原本的圖示，設定畫面有「找不到」的說明 | ⬜（要多開一次遊戲） |

## G. 外觀和主題

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| G1 | 預覽箭頭 | 外觀分頁 | 預覽兩端有 `<` `>`，右箭頭在「更多」左邊 | ⬜ |
| G2 | 5 款主題都有預覽 | 外觀分頁 | 帝國、信條、基因實驗室、虛空、星航都有卡片和預覽 | ⬜ |
| G3 | 逐一切換 | 每款選一次 | 主按鈕列、抽屜、齒輪、箭頭都換掉 | ⬜ |
| G4 | 四種狀態 | 每款看一般、滑鼠移上、分頁開著、停用 | 看得出差別（停用的可以在世界地圖畫面看） | ⬜ |
| G5 | 文字可讀 | 每款看文字顏色 | 一般和滑鼠移上的字都看得清楚 | ⬜ |
| G6 | 回到原版 | 卡片標題列「預設值」 | 回到原版外觀，交還給遊戲繪製（如果沒有其他改動） | ⬜ |
| G7 | Colored Categories 不受影響 | 選一款主題，打開建築分頁 | 建築分類按鈕仍是 Colored Categories 的樣子，沒有被主題換掉 | ⬜ |
| G8 | 配色像不像 DLC | 對照 | 見 `results-stage-4.md` 第 3 節：帝國、信條像；基因實驗室偏藍；虛空、星航不像 | ✅ 已對照 |

## H. 快捷鍵

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| H1 | Tab 不誤報 | 進階 → 建築規劃 | 不顯示和除錯選單衝突 | ⬜ |
| H2 | M 有說明 | 進階 → 醫療（Medical Tab） | 列出「分類：名稱」（原版 Misc7），下面有說明 | ⬜ |
| H3 | 原版按鈕改鍵 | 建築規劃 [設定] 換一個鍵 | 「選項 → 鍵盤設定」是同一個鍵 | ⬜ |
| H4 | 沒有快捷鍵的按鈕 | 例如 Hospital 的按鈕設一個鍵 | 關掉設定後按鍵會打開它 | ⬜ |
| H5 | 隱藏或收進抽屜 | 把 H4 那顆隱藏或收進抽屜 | 快捷鍵照樣能用 | ⬜ |
| H6 | 打字不觸發 | 在任何文字框打字 | 自訂快捷鍵不觸發 | ⬜ |

## I. 捲動和摺疊

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| I1 | 出現箭頭 | 捲動模式，最小寬度調大到放不下 | 兩端出現 `<` `>` | ⬜ |
| I2 | 滾輪不縮放地圖 | 在列上滾滾輪 | 列左右捲動，地圖不縮放 | ⬜ |
| I3 | 箭頭 | 點箭頭 | 一次約捲半個畫面 | ⬜ |
| I4 | 被蓋住的按鈕 | 把一顆捲到被右箭頭蓋一半，滑鼠移到箭頭上 | 那顆不反白、不跳提示，點下去是箭頭 | ⬜ |
| I5 | 說明文字 | 最小寬度下面 | 有「放得下就不會出現箭頭」的說明 | ⬜ |
| I6 | 菜单固定 | 捲動時 | 菜单固定在最右邊 | ⬜ |

## J. 存檔、範本、匯出匯入

| 編號 | 項目 | 怎麼做 | 預期 | 狀態 |
|---|---|---|---|---|
| J1 | 存檔保留 | 改設定 → 儲存 → 存檔 → 回主選單 → 讀檔 | 設定都在（含主題、圖示） | ⬜ |
| J2 | 範本 | 「存為預設範本」→ 開新遊戲 | 新遊戲是範本的排法 | ⬜ |
| J3 | 匯出文字 | 「匯出…」→「複製成文字」 | 可讀的 XML | ⬜ |
| J4 | 從剪貼簿匯入 | 「全部重設」→「匯入…」→「從剪貼簿」 | 排法回來（按儲存才保留） | ⬜ |
| J5 | 存成檔案 | 「存成檔案」→「匯入…」 | 清單裡看得到那個檔案 | ⬜ |
| J6 | 模組 XML | 「複製成模組 XML」 | 可以直接放進 Defs 的 XML | ⬜ |

## K. 發佈和工作坊（不用開遊戲）

| 編號 | 項目 | 預期 | 狀態 |
|---|---|---|---|
| K1 | `make_release.py` | 只有 About、1.7、Defs、Languages、Textures、LoadFolders.xml、API.md | ✅ 146 個檔、1.1 MB |
| K2 | 貼圖規格 | 正方形、不超過 72 | ✅ |
| K3 | 翻譯檔 | 29 種語言鍵齊全、佔位符一致 | ✅ |
| K4 | 預覽圖、說明 | 見 `results-stage-4.md` 第 6 節的建議 | 建議修改 |

## L. TooManyButtons、Custom Bottom Bar（這次不測）

兩個都有訂閱、沒有啟用（`east.toomanybuttons`、`dogebm.custombuttons`）。之後本機接手實作和驗證時，再另外做一份含它們的測試排序。

## M. 圖示來源和換圖示模組（2026-11-04 加）

### 排序（`RimCrow\backups\other\`）

| 檔案 | 模組數 | 內容 |
|---|---|---|
| `ModsConfig_20261104_1216_mbbs_buttons_max.xml` | 81 | 完整清單裡宣告支援 1.7、前置齊全的 45 個主按鈕模組，加上它們的前置和 18 個漢化包。沒有換圖示模組 |
| `ModsConfig_20261104_1216_mbbs_retexture.xml` | 85 | 上一份再加 Vanilla Textures Expanded（含漢化）、Bradson's Main Button Icons (Forked + Expanded)、Tang's 貼圖重置。順序照完整清單，Bradson's 在 Vanilla Textures Expanded 前面 |
| `ModsConfig_20261104_1216_mbbs_retexture_swap.xml` | 85 | 和上一份一樣，只把 Bradson's 移到 Vanilla Textures Expanded 後面 |

- 三份的 check_order 都只剩 2 組同名 DLL（`0MultiplayerAPI.dll`、`Newtonsoft.Json.dll`，完整清單本來就有）。
- 順序是刻意排的，匯入後不要按 RimCrow 的自動排序。
- 三份都有 RimTalk，會用設定檔裡的 OpenRouter 金鑰：進地圖後只要沒有暫停、速度在 3 倍以下，就會送請求。想省錢就保持暫停，或在 RimTalk 設定裡關掉 AI。
- 三份都有 Rules of Engagement，進地圖後要先看 log 有沒有被空貼圖警告洗到上限。
- 下面的預期結果是靜態推算的（`RimClaude\tools\mainbutton_scan.py --config <排序>`），報告在 `RimClaude\tools\output\主按鈕預期_mbbs_*.md`，沒有進遊戲驗證。
- 看的地方：設定視窗選一顆按鈕，資訊區的「圖示來源」。

### 圖示怎麼決定（原版程式和本模組原始碼確認）

1. 本模組裡玩家選的自訂圖示，最優先。
2. 按鈕自己的 `iconPath`，以所有 XML 補丁套用完之後的值為準。補丁照載入順序套用，兩個模組改同一顆時，後載入的贏。
3. 貼圖檔：`ContentFinder<Texture2D>.Get` 從最後一個模組往前找，找不到才用遊戲本體資源，所以同路徑的貼圖也是後載入的贏。
4. 沒有圖示就顯示文字。

### 測試項目

| # | 項目 | 排序 | 預期 | 結果 |
|---|---|---|---|---|
| M1 | 原版按鈕沒有圖示 | buttons_max | Architect、Work、Research 等的圖示來源是「無」，列上顯示文字 | ⬜ |
| M2 | 模組自帶圖示 | buttons_max | Guests 是 Hospitality (Continued)，Rimpedia 是 Rimpedia 1.7，Mint 的裝備選單是 Dubs Mint Menus | ⬜ |
| M3 | 本體貼圖 | buttons_max | History、Factions、Menu、Ideos 的圖示來源是「遊戲本體資源」（如果本體的貼圖也算在 Core 的內容裡，會顯示 Core，兩種都算對） | ⬜ |
| M4 | 兩個模組放同一個路徑 | buttons_max | RimTalkDebug（`UI/RimTalkIcon`）的圖示來源是 RimTalk - Expand Memory（比 RimTalk 晚載入） | ⬜ |
| M5 | 換圖示模組補上原版按鈕的圖示 | retexture | Architect、Work、Schedule、Assign、Animals、Research、Wildlife、Quests、World 的圖示來源都是 Vanilla Textures Expanded；MedicalTab、AIRobots 是 Bradson's | ⬜ |
| M6 | 模組自帶圖示被補丁換掉 | retexture | Guests、Rimpedia、Mint 的裝備選單改成 Bradson's 的圖（Bradson's 的補丁是「有 iconPath 就換掉」） | ⬜ |
| M7 | 補丁把小按鈕變成一般大小 | retexture | Vanilla Textures Expanded 替 History、Factions、Menu 換圖示時，也把 `minimized` 刪掉，所以這三顆從小圖示變成一般寬度的按鈕。本模組的寬度計算和「重設」要以這個狀態為準 | ⬜ |
| M8 | 同路徑貼圖蓋過本體 | retexture | Ideos 的圖示來源是 Tang's 貼圖重置，圖是 Tang's 的 | ⬜ |
| M9 | 兩個換圖示模組改同一顆 | retexture | Mechs 是 Vanilla Textures Expanded 的 `IconMechanoids`（它在後面）；SM_MechWork 也是同一張（Mech Work Tab 偵測到 Vanilla Textures Expanded 才加） | ⬜ |
| M10 | 交換順序 | retexture_swap | 只有 Mechs 變成 Bradson's 的 `Mechs`，其他和 retexture 那份相同 | ⬜ |
| M11 | 自訂圖示最優先 | retexture | 替 Guests 選一張內建圖示，列上換成那張；按「預設值」後回到 Bradson's 的圖 | ⬜ |
| M12 | 圖示模式 | retexture | 「文字」模式下所有圖示都藏起來；「圖示＋文字」用的是上面決定出來的那張 | ⬜ |
| M13 | Player.log | 三份都看 | 沒有 `Could not load Texture2D`，也沒有本模組的 WARN／ERROR | ⬜ |

## 讓 Claude 自己操作測試（computer use）時

- 先在 RimCrow 匯入快速測試排序，由使用者啟動遊戲（RimCrow 開，或 `D:\Steam\steamapps\common\RimWorld\RimWorld.exe`）。
- Claude 要先申請操作 RimWorld 視窗的權限（遊戲屬於一般應用程式，可以點擊和打字）。
- 用截圖判斷畫面、用滑鼠和鍵盤操作。截圖會存進 `screenshots/stage4_<編號>.png`。
- 比較適合 Claude 自己做的：B、C、D、E、F1～F4、G、H1～H2、I、J3～J6 這類「點了看結果」的項目。
- 需要使用者在旁邊的：切換遊戲語言（B6、B7，會重新載入）、F5（重開遊戲）、H3（鍵盤設定視窗）、J2（開新遊戲）。
- 遊戲畫面上的文字是遊戲自己畫的，判讀要靠截圖，速度會比使用者手動慢。
