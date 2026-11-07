# 第八輪測試清單

分支：`claude/round-8`（從 main 89b117e 開出，第七輪已合併）。處理 `test/round-7-results` 分支（commit 21f51f6）`docs/testing/results-round-7.md` 回報的問題。

## 回報的問題怎麼處理

| 回報 | 處理 |
|---|---|
| 1.「只顯示圖示」在裝了 Vanilla Textures Expanded 時無效（按鈕夠寬就同時顯示文字） | 對照 VTE 原始碼（`Patch_MainButtonWorker.DoButton`）：它的 transpiler 不只把空字串換回名稱（`NoBlankText`），還會把圖示移到左邊（`AdjustedLogoPos`）、加大文字左邊距。只清空文字的話，圖示會留在左邊。所以改成和「圖示＋文字」同一套做法：「只顯示圖示」時，遊戲拿到的按鈕沒有圖示（VTE 的 `CanDrawIconAndLabel` 因此是 false，什麼都不改），文字在 `Widgets.ButtonTextSubtle` 的補丁裡清掉，圖示由本模組畫在正中央（大小 `iconSize`，滑鼠移上位移 `hoverLabelOffset`，和原版一樣）。按鈕沒有圖示時照舊顯示文字。「預設」模式不受影響，仍由遊戲和 VTE 決定 |
| 2. Void 滑鼠移上的效果太弱 | 滑鼠移上和開著時：內線換成亮紅、外圍有兩層模糊紅光、底部往上約 8px 的紅霧、3px 亮紅底線。一般狀態不變（只有細紅線、沒有光）。只改了 Void 的 `ButtonMouseover`、`ButtonActive` 兩張貼圖 |
| 3. 工作坊簡中用詞 | 「外观分页」→「外观标签页」、「找不到类别」→「找不到类」（繁中的「分頁」「類別」是台灣用語，不動） |
| 4. Preview.png | 從 `test/round-7-results` 拿進來（1280×720、362 KB） |

另外：外觀分頁的主題預覽多了一顆「停用」狀態的範例（第 4 顆，和遊戲一樣停用的按鈕不顯示文字），不用觸發停用也能先看主題的停用外觀。

## 步驟

1. **只顯示圖示（有 Vanilla Textures Expanded）**：排序加入 VTE（它的設定維持「圖示和文字」），最小按鈕寬度調到 150，整條列設成「只顯示圖示」。
   - 按鈕上沒有文字，圖示在正中央（不是靠左），滑鼠移上時圖示往右上移 2px。
   - 沒有圖示的按鈕仍顯示文字。
   - 再把整條列改回「預設」：照 VTE 自己的設定畫。VTE 沒設定過時是 `mainButtonMode = IconsAndText`，這個模式有圖示就只顯示圖示，看起來和「只顯示圖示」一樣；要看到「圖示在左＋文字」，先在 VTE 的設定選 `IconsThenText`。
   - 「圖示＋文字」：圖示在左、文字在右，只有一份圖示，沒有重疊。
2. **只顯示圖示（沒有 VTE）**：同樣設定再測一次，結果應該相同（圖示置中、沒有文字）。和改之前的原版圖示位置比對，看位置有沒有偏。
3. **Void 滑鼠移上**：套用虛空主題，正常距離看得出滑鼠移上的那顆在發紅光；一般狀態仍然只有細紅線。可以量一下下方幾列像素，和第七輪的數字比較。
4. **停用狀態**：
   - 先看外觀分頁的預覽：每個主題的第 4 顆是停用外觀。
   - 實機：遊戲裡打開「組成商隊」，按「選擇路線」進入世界地圖的路線規劃。這時 `validWithoutMap` 為 false 的按鈕和「世界」按鈕會變成停用（原版 `MainButtonWorker.Disabled` 的 `WorldRoutePlanner.FormingCaravan` 條件）。每個主題各看一次停用按鈕的樣子（沒有文字是原版行為）。
   - 也可以用：所有殖民者組成商隊離開後放棄據點，沒有地圖時一樣會停用。
5. **文字靠左**：整條列文字對齊選「靠左」，看一般按鈕、「更多」抽屜裡的按鈕、「圖示＋文字」三種情況。
6. **Custom Bottom Bar 本模組優先**：在相容設定把 Custom Bottom Bar 改成「本模組優先」，畫面提示「重開後生效」，重開遊戲後由本模組畫列，log 有移除補丁的訊息。再改回「對方優先」並重開，回到 Custom Bottom Bar 畫列。
7. **checklist M 節**：三份換圖示的排序（`_noroe` 版）各開到主選單，用開發模式輸出的 `Diagnostics/buttons.tsv` 的 `iconPath`／`iconSourceMod` 欄，對照 `test/stage-4-results` 的 `docs/testing/checklist-stage-4.md` 的預期結果。
8. 開發模式啟動一次，看有沒有本模組的紅字、黃字。

## 要帶回雲端的資料

- 各步驟通過／不通過，不通過的寫實際看到什麼（最好附截圖）。
- 「只顯示圖示」在有／沒有 VTE 時的截圖。
- Void 滑鼠移上的截圖或像素。
- 停用狀態的截圖（至少一個主題）。
- M 節的對照結果。
