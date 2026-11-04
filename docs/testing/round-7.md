# 第七輪測試清單

分支：`claude/round-7`（從 main 4a6e8b8 開出）。處理 `test/stage-4-results` 分支（commit b5dc3bb）`docs/testing/results-stage-4.md` 回報的問題。第四階段清單（`docs/testing/stage-4.md`）裡還沒測的項目照舊要測。

## 回報的問題怎麼處理

| 回報 | 處理 |
|---|---|
| B7 遊戲中切換語言後，設定視窗的按鈕清單、右側標題、「原本的名稱」停在舊語言 | 新增 `Patches/PlayDataReload.cs`：`PlayDataLoader.LoadAllPlayData` 的 postfix 在載入完成後（`LongEventHandler.ExecuteWhenFinished`）重抓 baseline，並清掉所有記著舊 Def 的快取：Tuning def、按鈕預設（XML 的部分；API 在執行時登記的保留）、改名紀錄（新 Def 的名字是新語言，有改名的按鈕下一幀重新套用）、`ContentFinder` 找到的圖示（PNG 匯入的保留）、抽屜齒輪圖示、相容橋接的「已套用」旗標。完成時 log 一行 `Play data reloaded ...`。第一次啟動不會觸發（`ButtonBaseline.Captured` 還是 false） |
| 開發模式 2 筆 WARN（`ButtonPainter`、`Window_ButtonDrawer` probably needs a StaticConstructorOnStartup attribute） | 兩個類別都加上 `[StaticConstructorOnStartup]` |
| 基因實驗室偏藍 | 底色改青藍、邊框青 #66B0B5、四角基因方塊深紅 #A84457，開著時的內線也是深紅；文字淡青 |
| 虛空不像 | 邊框、箭頭、齒輪改鼠尾草綠 #AEBC7E；紅色只剩一條細內線，平常不發光，滑鼠移上或開著時才有紅光和紅色底線；文字淡鼠尾草 |
| 星航不像 | 改成紫色為主（邊框 #9074BB，箭頭和齒輪淡紫），青色當輔色（鉚釘、滑鼠移上的底線、移上時的文字）；底色太空紫 |
| description.txt | Features 補上 5 款內建主題（預設是原版、不需要擴充）；匯入其他主按鈕列模組的設定改成「實驗性」；補一句和 Better Sliders 相容。`About.xml` 的說明同步 |
| Preview.png 換實機截圖 | 新增 `tools/make_preview.py`：給兩張截圖（原版／本模組）和裁切範圍，輸出 16:9、小於 1 MB 的 `About/Preview.png`。說明在 `docs/workshop/release-checklist.md` |

三款主題的描述改了，29 種語言都重新翻譯（標籤不變）。配色依據記在 `docs/research/dlc-visuals-2026-11-04.md` 的「本機實測與第三版」。

## 步驟

1. **切換語言（B7 重測）**：開一局，打開設定視窗看按鈕清單 → 關掉 → 選項裡換語言（例如繁中 → English）→ 回到遊戲再開設定。
   - 按鈕清單、右側標題、「原本的名稱」都是新語言。
   - Player.log 有一行 `[Main Button Bar System] Play data reloaded ...`，而且只有一行。
   - 有改過名的按鈕仍然顯示自訂名稱；清掉自訂名稱後顯示新語言的原名。
   - 抽屜的齒輪、主題的貼圖、選過的「遊戲內建圖示」都還在（不是空白或粉紅）；PNG 匯入的圖示也還在。
   - 有勾「本模組優先」的相容橋接時，換語言後仍然由本模組畫列。
   - 再換回原本的語言，以上都再確認一次。
2. **WARN**：開發模式啟動，log 不再有 `ButtonPainter`／`Window_ButtonDrawer` 的 `StaticConstructorOnStartup` 警告。
3. **主題**：外觀分頁依序套用基因實驗室、虛空、星航，各看一次：按鈕列、滑鼠移上、打開某個分頁（開著的狀態）、停用的按鈕、「更多」、抽屜、齒輪、左右箭頭。和 DLC 的 Preview.png 對照，記下還差在哪裡（最好附截圖）。
   - 虛空：平常沒有紅光，只有細紅線；滑鼠移上或開著時才有紅光和紅色底線。
   - 文字在三款主題上都看得清楚。
4. **工作坊文字**：讀一次 `docs/workshop/description.txt` 和 `About.xml` 的說明，模組選單裡的說明顯示正常。
5. **Preview.png**（可選）：用已經截好的素材跑 `tools/make_preview.py`，看結果是否可用；可以的話把產生的 `About/Preview.png` 和用的參數一起推到測試分支。
6. 第四階段清單裡還沒測的項目（圖示四種模式、整條列一次設定、PNG 匯入、文字對齊、其他模組設定的參考／匯入、checklist M 節換圖示模組的排序）。

## 要帶回雲端的資料

- 各步驟通過／不通過，不通過的寫實際看到什麼。
- 有沒有紅字、黃字（本模組的）。
- 三款主題和 DLC 預覽圖的對照結果。
