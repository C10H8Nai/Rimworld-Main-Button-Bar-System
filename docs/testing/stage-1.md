# 第一階段測試清單

分支：`claude/stage-1`。這一階段沒有正式功能，只確認地基和三個高風險項目（規格第 6、8 節）。

## 這一版做了什麼

- **接管繪製**：`MainButtonsRoot.DoButtons` 的 Prefix（優先級最後）。三種測試模式，在「選項 → 模組設定 → Main Button Bar System」切換，**即時生效**：
  1. 由遊戲繪製（預設，不接管）
  2. 由本模組繪製：原版排法（外觀應該和 1 完全一樣）
  3. 由本模組繪製：捲動模式（可調預設寬度、最小寬度、填滿畫面、菜单固定最右）
- **基準線**：啟動時（`StaticConstructorOnStartup`）記下每顆主按鈕的原始狀態。
- **診斷**（暫時，正式版會拿掉）：啟動時和第一次畫主按鈕列時，各在 `Player.log` 寫一段診斷，並把每顆按鈕的資料寫成表格檔。
- 如果本模組畫的時候出錯，會寫一行紅字，然後這次遊戲交還給原版繪製，不會讓主按鈕列消失。

## 步驟

1. `git fetch` 後切到 `claude/stage-1`，用 `verify_dll.py` 檢查 DLL。
2. 啟動遊戲，載入一個存檔。
3. **模式 1（預設）**：截圖主按鈕列。
4. 切到 **模式 2**：再截圖一次，和模式 1 比對。應該完全相同，包括 Bradson 的圖示、Vanilla Textures Expanded 的圖示、各模組新增的按鈕、半格按鈕的寬度。
   - 點幾顆按鈕：視窗正常開關；滑鼠移上去有提示。
   - 用快捷鍵開幾個分頁（例如建築）。
5. 切到 **模式 3**：
   - 按鈕夠多時應該超出畫面，兩端出現 `<` `>` 箭頭。
   - 在列上滾滑鼠滾輪：列應該左右捲動，**地圖不應該跟著縮放**（規格 4.10）。
   - 點箭頭：一次捲大約半個畫面。
   - 菜单固定在最右邊，不跟著捲動。
   - 調寬一點的最小寬度，或關掉「填滿」，看看排版有沒有跟著變。
   - 被捲到看不見一半的按鈕，點得到的部分要正常運作。
6. 切回模式 1 再存檔、退出。

## 要帶回雲端的資料

- `Player.log` 裡兩段以 `[Main Button Bar System]` 開頭的診斷（整段複製）：
  - `Stage 1 diagnostics`：按鈕數、快捷鍵、圖示來源、LoadImage 結果。
  - `Patches on main button methods`：哪些模組補丁了主按鈕相關的方法（判斷和 Performance Optimizer、TooManyButtons、Custom Bottom Bar 的互動）。
- 表格檔：`%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\MainButtonBarSystem\Diagnostics\buttons.tsv`。這個檔可以 commit 到 repo 的 `docs/research/`，方便雲端看。
- 模式 2 和模式 1 有沒有任何差異（截圖最好）。
- 模式 3：滾輪會不會讓地圖縮放、箭頭和捲動手感。
- 本模組有沒有紅字或黃字。

## 選做：測試匯入自訂圖片

在 `...\RimWorld by Ludeon Studios\MainButtonBarSystem\Icons\` 放一兩張 PNG（這個資料夾在第一次啟動時就會建立），重開遊戲，診斷裡的 `LoadImage` 那幾行會列出每張圖讀取的結果和尺寸。
