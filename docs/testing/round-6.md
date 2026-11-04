# 第六輪測試清單

分支：`claude/round-5`（PR #6 繼續更新，這一輪的 commit 接在第五輪後面）。處理 `test/round-5-results` 分支 `docs/testing/results-round-5.md` 回報的問題。

## 回報的問題怎麼處理

| 回報 | 處理 |
|---|---|
| 1. 齒輪圖示不像齒輪 | 模組自帶 `Textures/MainButtonBarSystem/UI/Gear.png`（64×64，白色齒輪加深色描邊），Tuning 的 `drawerSettingsIcon` 預設指向它。主題可以用 `drawerSettingsIcon` 換掉。不再用原版的除錯圖示 |
| 2. 简单／进阶不該是分頁 | 分頁只剩「按鈕」「外觀」。簡單／進階改成按鈕頁左上角的兩段式切換，選中的那段有底色、白字，沒選的是灰字。切換狀態存在模組設定（`showAdvanced`），所有存檔共用，下次打開維持上次的選擇 |
| 3. 滑桿被 BetterSliders 的數字框蓋住 | 所有滑桿改成本模組自己畫（不呼叫 `Widgets.HorizontalSlider`，所以 BetterSliders 不會介入），右邊固定一格本模組的數字輸入框，可以直接輸入。輸入框寬度、滑軌、把手的大小和顏色都在 Tuning |
| 4. 外觀預覽沒有箭頭 | 預覽照主按鈕列的樣子排：`<` 按鈕… `>` 「更多」。主題有 `arrowLeft`／`arrowRight` 就畫主題的圖示 |
| 補丁兩個 `DrawAtlas` 會跑兩遍 | 只補丁 3 參數的版本 |
| API.md 的貼圖建議 | 照本機確認的切法改寫：一定要正方形；畫面上四角 = 貼圖寬 ÷ 4，上限是按鈕高的一半（主按鈕列 18），所以貼圖超過 72×72 沒有意義；邊和中央是拉伸 |

生態調查（`docs/research/ecosystem-2026-11-04.md`）帶來的修正：

| 項目 | 處理 |
|---|---|
| `DoButtons` 的「別人已經接管」判斷永遠不會執行 | Harmony 3 會在前面的 prefix 回傳 false 後跳過本模組的 prefix，所以觀察改到 postfix（一定會執行）。別的模組畫列時，查詢 API 和診斷現在都會正確記錄 |
| UI Not Included、DragReorder | 新增相容橋接：偵測到時設定畫面有提示；本模組優先時，移除 UI Not Included 換掉 `DoButtons` 的 transpiler、DragReorder 在主按鈕列上的補丁 |
| 版本資訊 | `About.xml` 加 `modVersion`（0.6.0）；新增 `LoadFolders.xml`（根目錄＋`1.7`）；`MainButtonBarAPI.ApiVersion` |
| API.md | 新增：軟相依寫法、API 穩定性、本模組的補丁清單和優先級、使用限制 |

翻譯：「簡單」「進階」的 key 改名（`MBBS_Tab_*` → `MBBS_View_*`，翻譯沿用）；新增「按鈕」分頁名稱和切換的提示，29 種語言都有。

## 步驟

1. 打開設定：上方只有「按鈕」「外觀」兩個分頁。
2. 按鈕頁左上角是「簡單｜進階」切換，看得出目前是哪一個。切到進階後關掉視窗、重開（或換一個存檔），仍然是進階。
3. 有裝 BetterSliders 的情況下，到進階的整條列看滑桿：右端沒有被數字框蓋住，右邊是本模組的輸入框。
   - 拖滑桿，輸入框的數字跟著變。
   - 在輸入框直接輸入數字，滑桿跟著動；超出範圍會被限制在範圍內。
   - 按「預設值」，滑桿和輸入框都回到預設。
4. 抽屜右下角是齒輪，滑鼠移上去有提示，點了打開設定。
5. 外觀分頁的預覽兩端有 `<` `>`，右箭頭在「更多」的左邊。
6. `LoadFolders.xml` 新增後，模組照常載入（`Loaded.` 有出現、設定畫面正常、抽屜齒輪有圖）。
7. 開發模式下，「Patches on main button methods」那段診斷，現在是在第一次畫主按鈕列時寫入（改由 postfix 觸發）。
8. 上一輪沒測的：撞鍵（Tab 不誤報、Medical Tab 的 M 有說明）、英文「Drawer」欄標題、文字對齊的滑鼠位移、其他語言的設定畫面。

## 要帶回雲端的資料

- 各步驟通過／不通過，不通過的寫實際看到什麼。
- 有沒有紅字、黃字。
- 開發模式的「Patches on main button methods」：`DrawAtlas` 應該只剩一組本模組的補丁。
