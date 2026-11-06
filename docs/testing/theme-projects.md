# 主題專案（新增／刪除主題）實機測試清單

分支：`claude/theme-projects`。規格：`openspec/changes/theme-projects/specs/theme-projects/spec.md`（每個 Scenario 對到下面一節）；設計：`docs/superpowers/specs/2026-11-07-theme-projects-design.md`。

排序：一般功能的小排序（帶優化模組），開發模式開著。測試建立的主題專案，測完從 `Mods\` 搬到 `_資料與測試\MainButtonBarSystem\theme-projects\`（不進 repo）。每節結束看 log 有沒有本模組的紅字、黃字（`log_scan.py`），黃字要能對到下面寫明會出現的那幾種。

## A. 新增主題專案

1. 主選單 → 模組設定 → 本模組 → 外觀分頁（預設主題）。清單最後（內建主題卡片、自訂主題存檔之後）有「新增主題…」，滑鼠提示說明用途。
2. 按下：對話框有名稱、作者、說明、packageId。名稱空白 → 「建立」是灰的、紅字提示「要名稱」；把 packageId 改成 `abc`（不合法）或已安裝模組的 packageId（例如 `NAI.MainButtonBarSystem`）→ 「建立」灰、提示原因。
3. 名稱 `Test Steel`、作者 `Nai`：packageId 自動是 `Nai.mbbstheme.TestSteel`。按建立。
   - 預期：`Mods\Test Steel\` 有 `About/About.xml`、`About/Preview.png`、`Defs/NaiTestSteel_Theme.xml`、`Textures/NaiTestSteel/` 12 張 PNG、`_Guide.png`、`MBBS_ThemeProject.xml`；About 的 `modDependencies` 帶 `downloadUrl`（和／或 `steamWorkshopUrl`）。
   - 清單多一張「test steel」卡片並被選上，來源寫「主題專案（未啟用）」；底部列（開 `alwaysDrawBar` 或進遊戲看）變成空白主題；左上訊息顯示資料夾路徑。
   - 不用重開遊戲。
4. 再按一次「新增主題…」，同樣名稱和作者：packageId 自動變成 `Nai.mbbstheme.TestSteel2`，資料夾 `Test Steel (2)`。（建完可以直接刪，見 E。）
5. Mods 寫不進去（把 `Mods` 設成唯讀，或用一個在 Mods 裡已存在、被鎖的檔名）不好模擬，沒做到的話寫「沒測」。預期：訊息說明原因和路徑，沒有任何新資料夾，沒有紅字。
6. 自訂主題模式：選圖對話框（「選擇…」）的主題圖清單裡有 test steel 的四張按鈕底圖和 ButtonPicture。

## B. 空白主題的圖

1. 選著 test steel，進遊戲地圖（或主選單＋`alwaysDrawBar`）：一般按鈕藍（N）、滑鼠移上綠（H）、打開某分頁的按鈕橘（A）；三種顏色看得出來。停用的按鈕（沒地圖時）灰（X）。
2. 每顆按鈕的框裡看得到插圖的準心或裁切框（黃／青／粉紅）。
3. 「更多」按鈕紫色（+）；打開抽屜，背景是深紫九宮格（W）、齒輪是空白主題的齒輪；捲動模式的左右箭頭是空心三角。
4. 列背景是格線＋黃色接縫記號，鋪滿整條列。
5. 打開 `_Guide.png`：每個檔名都有標出對應的地方和尺寸。

## C. 未啟用的主題專案由本模組載入

1. 不用重開就能選：A.3 完成後，在遊戲裡存檔（`MBBS_tp_select`），重開遊戲，讀檔：外觀還是 test steel，卡片來源「主題專案（未啟用）」，log 沒有本模組紅字。
2. 已啟用的不重複載入：在模組清單勾選 `Test Steel`、重開遊戲。清單裡 test steel 只有一張卡，來源是模組名稱「Test Steel」；log 沒有 `Adding duplicate` 或 `already has short hash`。選中它時「刪除」是灰的，提示要先停用並重開。測完在模組清單取消勾選、重開。
3. 勾選了但還沒重開：在模組清單勾選 `Test Steel` 後不要重開，回到設定視窗。卡片來源仍是「主題專案（未啟用）」（遊戲還沒載入它），「刪除」是灰的、提示「已在模組清單勾選」。取消勾選。
4. defName 撞名：把 `Mods\Test Steel` 整個複製成 `Mods\Test Steel Copy`（不改任何檔），重開遊戲。清單只有一張 test steel；log 一行黃字 `Theme project Test Steel Copy: the theme NaiTestSteel_Theme is skipped: another theme already has that defName …`，沒有紅字。測完刪掉複本（直接刪資料夾即可，它是測試產物）。
5. 貼圖讀不到：把 `Textures/NaiTestSteel/Bar.png` 改名成 `Bar_x.png`，按「重新載入」。列背景變回遊戲原本（沒有背景），log 一行黃字（找不到貼圖），沒有紅字。改回原名、重新載入，背景回來。
6. 主題專案不見了：選著 test steel 存檔（`MBBS_tp_gone`），關遊戲，把 `Mods\Test Steel` 暫時搬到別處，開遊戲讀檔：外觀退回原版，沒有紅字。搬回來。
7. 切換語言（遊戲會重新載入所有 Def）：主選單選著 test steel 時，從「選項 → 語言」換成英文再換回來（換語言前先複製 `Player.log`）。test steel 仍在清單裡、仍選著、卡片和按鈕正常；log 有 `Play data reloaded`，沒有本模組紅字。

## D. 重新載入與自動偵測

1. 改圖後自動更新：設定視窗開在外觀分頁、選著 test steel，用小畫家打開 `Textures/NaiTestSteel/Button.png`，塗一塊紅色存檔：1～2 秒內卡片預覽和底部列的一般按鈕出現紅色，左上訊息「讀了 N 個檔案」，沒有紅字。（未啟用的主題專案，卡片一律用貼圖即時畫，不用 `preview` 圖，所以主選單也看得到改動。）
   - 建立時說明留空：Def 沒有 `<description>` 行，log 沒有 `empty description`。
2. 改 XML 後重新載入：在 `Defs/NaiTestSteel_Theme.xml` 把 `labelColor` 改成 `(1, 0.3, 0.3, 1)`，並在 `buttonImage` 後面加一段：
   ```xml
   <buttonImages>
     <li>
       <button>Architect</button>
       <texture>NaiTestSteel/Gear</texture>
       <fit>Fit</fit>
     </li>
   </buttonImages>
   ```
   按「重新載入」（或等自動偵測）。預期：按鈕文字變紅；建築規劃（Architect）的插圖換成齒輪，其他按鈕照舊。
3. XML 新增一款主題：在同一個 XML 複製整段 `BarAppearanceDef`，defName 改 `NaiTestSteel_Theme2`、label 改 `test steel 2`，存檔：清單多一張「test steel 2」。刪掉那段存檔：卡片消失；如果當時選著它，外觀回到原版。
4. 檔案正在寫入（模擬寫到一半）：PowerShell 先 `$p='<…\Textures\NaiTestSteel\Button.png>'; $b=[IO.File]::ReadAllBytes($p)`，再 `[IO.File]::WriteAllBytes($p, $b[0..99])`（只剩前 100 bytes，不是完整 PNG），等 3 秒，最後 `[IO.File]::WriteAllBytes($p, $b)`。預期：中間那幾秒按鈕維持舊圖、沒有訊息、沒有紅字；寫回完整檔後下一輪讀到（訊息「讀了 N 個檔案」）。
5. XML 打到一半：把 XML 存成不合法（例如刪掉一個 `>`）：log 一行黃字 `not valid XML yet`，主題維持上一版；修好存檔後恢復。
6. 不認得的欄位：在 Def 加 `<notAField>1</notAField>` 存檔：log 一行黃字 `<notAField> is not a field of BarAppearanceDef; it is skipped.`，沒有紅字，其他欄位照常。
   - 值寫錯：`<barBackgroundFit>Tiled</barBackgroundFit>`、`<labelColor>(1, 0.3</labelColor>`：各一行黃字 `… is not a valid …; it is skipped.`，那一項回到預設，沒有紅字。
   - 同一欄寫兩次（再加一行 `<button>…</button>`）：一行黃字 `written twice`，用第一個。
   - `<workerClass>NoSuchWorker</workerClass>`：一行黃字，列照常畫（沒有每幀的例外）。
   - 改完把 XML 還原。
7. 打開資料夾：按「打開資料夾」，檔案總管開到 `Mods\Test Steel`；滑鼠提示是完整路徑。
8. 每幀不讀檔：設定視窗開著閒置 30 秒，Frame profiler 或目測沒有卡頓（只做目測即可）。

## E. 只能刪除主題專案

1. 刪除未啟用的主題專案：選著 test steel（或 A.4 的第二份），按「刪除」→ 確認視窗寫名稱、資料夾、會搬到 `…\MainButtonBarSystem\DeletedThemes\`。確認：資料夾搬到 `DeletedThemes\Test Steel_<日期時間>`，`Mods\` 裡沒有了；卡片消失；外觀回到原版；左上訊息寫搬到哪裡。
2. 不能刪的主題：選內建主題（帝國、萘伊…）和其他主題模組的卡片：沒有「重新載入／打開資料夾／刪除」這列按鈕。
3. 已啟用的不能刪：見 C.2、C.3。
4. 搬移失敗：用 PowerShell 獨占開著專案裡的一個檔（`$f=[IO.File]::Open('<…\_Guide.png>','Open','Read','None')`，測完 `$f.Close()`），按刪除並確認。預期：訊息說明原因，資料夾和清單都維持原狀，沒有紅字（`Mods\` 裡也不能多出 `.mbbs-deleting` 資料夾）。關掉檔案後再刪一次，成功。

## F. 不打擾只整理按鈕的玩家

1. 用沒有任何主題專案的狀態（`Mods\` 沒有 `MBBS_ThemeProject.xml` 的資料夾）開設定視窗：按鈕分頁和以前一樣；外觀分頁只多「新增主題…」一顆按鈕；log 和以前一樣（沒有新的本模組訊息）。

## G. 翻譯

1. 切日文、簡中各看一次：「新增主題…」按鈕、對話框標題和提示、卡片下的三顆按鈕、確認視窗都有翻譯，沒有超出或被切。
